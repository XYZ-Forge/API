using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace XYZForge.Models
{

    public record Order
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; init; }
        // TODO: Model the Order object

        /*
            - Nume
            - 
        */
    }

}