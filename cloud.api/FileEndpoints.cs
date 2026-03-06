using cloud.api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System.Security.Claims;
using cloud.api.Dtos;
using cloud.api.Models;

namespace cloud.api
{
    public static class FileEndpoints
    {
        public static void MapFileEndpoints(this IEndpointRouteBuilder app, GridFSBucket bucket)
        {
            //get file structure overview, only files that are public or owned by the user
            app.MapGet("/files", async (IGridFSBucket bucket, ClaimsPrincipal user, string? path) =>
            {
                bool isPublic;
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? user.FindFirst("sub")?.Value;
                path = path ??= "/";

                var filter = Builders<GridFSFileInfo>.Filter.And(
                    Builders<GridFSFileInfo>.Filter.Eq("metadata.VirtualPath", path),
                    Builders<GridFSFileInfo>.Filter.Or(
                        Builders<GridFSFileInfo>.Filter.Eq("metadata.IsPublic", true),
                        Builders<GridFSFileInfo>.Filter.Eq("metadata.ÔwnerId", userId)
                        )
                    );
                var cursor = await bucket.FindAsync(filter);
                var fileInfos = await cursor.ToListAsync();

                var result = fileInfos.Select(f => new FileEntry(
                    f.Id.ToString(),
                    f.Filename,
                    f.Length,
                    f.Metadata.GetValue("VirtualPath", "/").AsString,
                    f.Metadata.GetValue("IsPublic", false).AsBoolean,
                    f.Metadata.GetValue("OwnerId", "").AsString,
                    // Extract the BsonArray and map each item to your DTO
                    f.Metadata.GetValue("AllowedIdentities", new BsonArray()).AsBsonArray
                      .Select(x => new Dtos.ACLEntryDto(x["IdentityId"].AsString, x["Access"].AsString))
                      .ToList(),
                    f.Metadata.Contains("MimeType") ? f.Metadata["MimeType"].AsString : "application/octet-stream"
                )).ToList();

                return Results.Ok(result);
            }) 
            .AllowAnonymous();

            //get file by id, only if public or owned by user
            app.MapGet("/files/{id}", async (IGridFSBucket bucket, string id, ClaimsPrincipal user) =>
            {
                var objectId = new ObjectId(id);
                var fileInfo = await (await bucket.FindAsync(Builders<GridFSFileInfo>.Filter.Eq("_id", objectId))).FirstOrDefaultAsync();

                if (fileInfo == null) return Results.NotFound();

                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                bool isOwner = fileInfo.Metadata["OwnerId"].AsString == userId;
                bool isPublic = fileInfo.Metadata["IsPublic"].AsBoolean;

                if (!isPublic && !isOwner) return Results.Forbid();

                var stream = await bucket.OpenDownloadStreamAsync(objectId);
                return Results.File(stream, "application/octet-stream", fileInfo.Filename);
            }); //not checked, assume non functional

            //get file by id with service token, token contains fileID 

            //update file access
            app.MapPatch("/files/{id}", async (string? id,[FromBody] Dtos.ACLEntryDto aclEntryDto, IMongoDatabase db, IGridFSBucket bucket, ClaimsPrincipal user) => 
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var fileId = new ObjectId(id);

                // 1. We need the "fs.files" collection
                var filesCollection = db.GetCollection<BsonDocument>("fs.files");

                // 2. Filter: Must be the right ID AND the requester must be the owner
                var filter = Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Eq("_id", fileId),
                    Builders<BsonDocument>.Filter.Eq("metadata.OwnerId", userId)
                );

                // 3. Update: "$push" adds an item to an array without duplicates (or use $addToSet)
                var update = Builders<BsonDocument>.Update.Push("metadata.AllowedIdentities", new BsonDocument
                    {
                        { "IdentityId", aclEntryDto.IdentityId },
                        { "Access", aclEntryDto.Access }
                    });

                var result = await filesCollection.UpdateOneAsync(filter, update);

                if (result.MatchedCount == 0) return Results.NotFound("File not found or you are not the owner.");

                return Results.Ok(new { Message = "Permissions updated successfully" });

            });
        }
    }
}

/*
 * app.MapGet("/files", async ([FromServices]IGridFSBucket bucket, ClaimsPrincipal user, string? path) =>
{
    bool isPublic;
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? user.FindFirst("sub")?.Value;
    path = path ??= "/";

    var filter = Builders<GridFSFileInfo>.Filter.And(
        Builders<GridFSFileInfo>.Filter.Eq("metadata.VirtualPath", path),
        Builders<GridFSFileInfo>.Filter.Or(
            Builders<GridFSFileInfo>.Filter.Eq("metadata.IsPublic", true),
            Builders<GridFSFileInfo>.Filter.Eq("metadata.OwnerId", userId)
            )
        );
    var cursor = await bucket.FindAsync(filter);
    var fileInfos = await cursor.ToListAsync();

    var result = fileInfos.Select(f => new FileEntryDto(
        f.Id.ToString(),
        f.Filename,
        f.Length,
        f.Metadata["VirtualPath"].AsString,
        f.Metadata["IsPublic"].AsBoolean,
        f.Metadata["OwnerId"].AsString
    ));

    return Results.Ok(result);
})
.AllowAnonymous(); //also unatuhorized clients are allowed to make the get req

app.MapGet("/files/{id}", async (IGridFSBucket bucket, string id, ClaimsPrincipal user) => //potential security risk?? Hacker could find out what paths exist by trying out?
{
    var objectId = new ObjectId(id);
    var fileInfo = await (await bucket.FindAsync(Builders<GridFSFileInfo>.Filter.Eq("_id", objectId))).FirstOrDefaultAsync();

    if (fileInfo == null) return Results.NotFound();
    
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    bool isOwner = fileInfo.Metadata["OwnerId"].AsString == userId;
    bool isPublic = fileInfo.Metadata["IsPublic"].AsBoolean;

    if (!isPublic && !isOwner) return Results.Forbid();

    var stream = await bucket.OpenDownloadStreamAsync(objectId);
    return Results.File(stream, "application/octet-stream", fileInfo.Filename);
})
.AllowAnonymous();

app.MapPost("/files", async (IFormFile file, ClaimsPrincipal user, string virtualPath = "/", bool isPublic = false) => //how to set files to public?
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? user.FindFirst("sub")?.Value;

    if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

    using var stream = file.OpenReadStream();

    var options = new GridFSUploadOptions
    {
        Metadata = new BsonDocument
        {
            { "OwnerId", userId },
            { "IsPublic", isPublic },
            { "VirtualPath", virtualPath }
        }
    };

    var id = await bucket.UploadFromStreamAsync(file.FileName, stream, options);
    return Results.Ok(new { Id = id.ToString(), file.FileName });
})
.RequireAuthorization(); //makes sure user is filled

app.MapDelete("/files/{id}", async (IGridFSBucket bucket, string id, ClaimsPrincipal user) =>
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                 //?? user.FindFirst("sub")?.Value;

    var objectId = new ObjectId(id);
    var fileInfo = await (await bucket.FindAsync(Builders<GridFSFileInfo>.Filter.Eq("_id", objectId))).FirstOrDefaultAsync();

    if (fileInfo == null) return Results.NotFound();

    try
    {
        await bucket.DeleteAsync(objectId);
    }
    catch
    {
        throw new Exception("sum ting wong in the mongoDB");
    }

    if (fileInfo.Metadata["OwnerId"].AsString != userId) return Results.Forbid();

    return Results.NoContent();
})
.RequireAuthorization();

app.MapDelete("/files/{id}", async (IGridFSBucket bucket, string id, ClaimsPrincipal user) =>
{
    var objectId = new ObjectId(id);
    var fileInfo = await (await bucket.FindAsync(Builders<GridFSFileInfo>.Filter.Eq("_id", objectId))).FirstOrDefaultAsync();

    if (fileInfo == null) return Results.NotFound();

    // Nur der Besitzer darf löschen!
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (fileInfo.Metadata["OwnerId"].AsString != userId) return Results.Forbid();

    await bucket.DeleteAsync(objectId);
    return Results.NoContent();
})
.RequireAuthorization();

 */