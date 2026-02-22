using BCrypt.Net;
using cloud.api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using BCrypt;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// This tells the app to listen on port 8080 on ALL network interfaces
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(8080);
});

var app = builder.Build();

app.MapGet("/", () => "The API is alive!");

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["Key"]);
var mongoClient = new MongoClient(builder.Configuration["MongoSettings:ConnectionString"]);
var database = mongoClient.GetDatabase(builder.Configuration["MongoSettings:DatabaseName"]);
var collection = database.GetCollection<User>("users"); //Neo Auth logic
var bucket = new GridFSBucket(database);

//auth logic
builder.Services.AddScoped<JWTService>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(secretKey),
        };
    });

builder.Services.AddAuthorization();

//auth endpoints
app.MapPost("/register", async (string username, string password) =>
{
    var newUser = new User
    {
        Username = username,
        HashedPassword = BCrypt.Net.BCrypt.HashPassword(password)
    };

    collection.InsertOne(newUser);

});

app.MapPost("/login", async (LoginDto req, JWTService jwtService) =>
{
    var user = await collection.Find(u => u.Username == req.Username).FirstOrDefaultAsync();
    if (user == null)
        return Results.Unauthorized();

    if (!PasswordHasher.VerifyPassword(req.Password, user.HashedPassword))
        return Results.Unauthorized(); //unauthorized can mean smt is wrong with pwd logic

    var token = jwtService.GenerateToken(user.Id, req.Username);
    return Results.Ok(new { Token = token });
});

app.MapPost("/refresh", async () => 
{ 
    throw new NotImplementedException();
});
//DONT FORGET TO ACTUALLY USE AUTH

//can use [Authorize] attribute on endpoints that require auth, e.g.: post anything
//create helloworld file if it doesn't exist
app.MapGet("/seed", async () => {
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
//lets def some endpoints: get /files (returns structure JSON) get GET /files?path=/whatever/whatever (returns file by path) GET /files/{id} returns binary filestream post /files (upload file) delete /files/{name} (delete file by name) delete /files/{path} (delete file by path)

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

app.UseAuthentication();
app.UseAuthorization();

app.Run();

//things to add: encryption, indexing for faster search, more security? 

