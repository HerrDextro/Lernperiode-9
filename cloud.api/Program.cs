using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using static System.Runtime.InteropServices.JavaScript.JSType;

var builder = WebApplication.CreateBuilder(args);

// This tells the app to listen on port 8080 on ALL network interfaces
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(8080);
});

var app = builder.Build();

// Add a simple hello world to test the connection
app.MapGet("/", () => "The API is alive!");
// ... rest of your code


// 1. Setup MongoDB & GridFS
var mongoClient = new MongoClient(builder.Configuration["MongoSettings:ConnectionString"]);
var database = mongoClient.GetDatabase(builder.Configuration["MongoSettings:DatabaseName"]);
var bucket = new GridFSBucket(database);

// 2. The "MVP Seed" - Creates the hello world file if the DB is empty
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

// 3. Get all files
app.MapGet("/files", async () => {
    var files = await bucket.Find(Builders<GridFSFileInfo>.Filter.Empty).ToListAsync();
    return files.Select(f => new { Id = f.Id.ToString(), f.Filename });
});

// 4. Download file by ID
app.MapGet("/files/{id}", async (string id) => {
    var stream = await bucket.OpenDownloadStreamAsync(new ObjectId(id));
    return Results.Stream(stream, "application/octet-stream", stream.FileInfo.Filename);
});

app.Run();



