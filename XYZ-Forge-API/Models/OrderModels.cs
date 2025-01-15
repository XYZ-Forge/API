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

        [BsonElement("ObjectName")]
        public string ObjectName { get; set; } = string.Empty;
        [BsonElement("Weight")]
        public double Weight { get; set; }

        [BsonElement("Dimensions")]
        public string Dimensions { get; set; }=string.Empty;

        [BsonElement("Color")]
        public string Color { get; set; } = string.Empty;

        [BsonElement("Address")]
        public string Address { get; set; } = string.Empty;

        [BsonElement("MaterialType")]
        public string MaterialType { get; set; } = string.Empty;

        [BsonElement("TotalCost")]
        public double TotalCost { get; set; }
    }

    public record CalculateCostRequest
    {
        public string MaterialType { get; set; } = string.Empty;
        public double Weight { get; set; }
    }

    public record GetOrders(string id);
    public record AddOrders(string ObjectName,double Weight,string Dimensions,string Color,string Address,string MaterialType,double TotalCost);
    public record SearchOrders(string? id=null,string? ObjectName=null,double? Weight=null,string? Dimensions=null,string? Color=null,string? Address=null,string? MaterialType=null,double? TotalCost=null);
    public record DeleteOrders(string id);

}