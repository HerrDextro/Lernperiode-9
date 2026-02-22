using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace cloud.api
{
    // Define a C# class that maps to a MongoDB document
    public class User
    {
        [BsonId] // Marks this property as the document's primary key (_id)
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("HashedPassword")] // Maps to "name" field in MongoDB
        public string HashedPassword { get; set; }

        [BsonElement("Username")]
        public string Username { get; set; }
    }
}