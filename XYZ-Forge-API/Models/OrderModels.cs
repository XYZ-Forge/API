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
        
        public string ObjectName { get; init; } = string.Empty;
        public double Weight { get; init; }
        public string Color { get; init; } = string.Empty;
        public string Address { get; init; } = string.Empty;
        public string MaterialType { get; init; } = string.Empty;
        public double TotalCost { get; init; }
    }

    public record CalculateCostRequest
    {
        public string MaterialType { get; init; } = string.Empty;
        public double Weight { get; init; }
    }

}