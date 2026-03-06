using cloud.api.Dtos;
using cloud.api.Models;
using cloud.api.Services;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System.Security.Claims;

namespace cloud.api
{
    public static class TestEndpoints
    {
        public static void MapTestEndpoints(this IEndpointRouteBuilder app, GridFSBucket bucket)
        {
            app.MapGet("/", () => "The API is alive!");

            app.MapGet("/seed", async () =>
            {
                var filter = Builders<GridFSFileInfo>.Filter.Empty;
                var existing = await bucket.Find(filter).AnyAsync();
                if (!existing)
                {
                    await bucket.UploadFromBytesAsync("helloworld.txt",
                        System.Text.Encoding.UTF8.GetBytes("Hello from your Private Cloud!"));
                    return "Seeded!";
                }
                return "Already seeded.";
            });

            app.MapGet("/test/files", async (IGridFSBucket bucket, ClaimsPrincipal user, string? path) =>
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

                if ( fileInfos.Count > 0 ) { return Results.NotFound( "No files found."); }

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
            });

            app.MapGet("/test/users", (IMongoCollection<User> collection) =>
            {
                var users = collection.Find(_ => true).ToList();
                if ( users.Count == 0 ) { return Results.NotFound( "No users found."); }
                return Results.Ok(users);
            });
        }
    }
}