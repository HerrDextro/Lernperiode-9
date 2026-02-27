using cloud.api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System.Text;
using cloud.api.Services;


var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(8080);
});

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["Key"]);
var mongoClient = new MongoClient(builder.Configuration["MongoSettings:ConnectionString"]);
var database = mongoClient.GetDatabase(builder.Configuration["MongoSettings:DatabaseName"]);
var collection = database.GetCollection<User>("users"); //Neo Auth logic
var bucket = new GridFSBucket(database);

builder.Services.AddScoped<JWTService>();
builder.Services.AddSingleton<IMongoClient>(mongoClient);
builder.Services.AddSingleton<IMongoDatabase>(database);
builder.Services.AddSingleton<IMongoCollection<User>>(collection);
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
builder.Services.AddSingleton<IGridFSBucket>(bucket);
var app = builder.Build();


app.UseAuthentication();
app.UseAuthorization();

app.MapTestEndpoints(bucket);
//app.MapAuthEndpoints();

app.Run();

//things to add: encryption, indexing for faster search, more security? 

