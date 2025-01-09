using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace XYZForge.Models
{
    public class Printer
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; init; }

        [BsonElement("PrinterName")]
        public string PrinterName { get; set; } = string.Empty;

        [BsonElement("Resolution")]
        public string Resolution { get; set; } = string.Empty;

        [BsonElement("HasWiFi")]
        public bool HasWiFi { get; set; }

        [BsonElement("HasTouchScreen")]
        public bool HasTouchScreen { get; set; }

        [BsonElement("MaxDimensions")]
        public string MaxDimensions { get; set; } = string.Empty;

        [BsonElement("Price")]
        public double Price { get; set; } = 0.0;

        [BsonElement("Type")]
        public string Type { get; set; } = string.Empty;

        [BsonElement("ResinTankCapacity")]
        public double? ResinTankCapacity { get; set; } 

        [BsonElement("LightSourceType")]
        public string? LightSourceType { get; set; } 

        [BsonElement("FilamentDiameter")]
        public double? FilamentDiameter { get; set; } 

        [BsonElement("SupportedMaterials")]
        public List<string>? SupportedMaterials { get; set; } 
    }
    public class AddPrinterRequest
    {
        public string PrinterName { get; set; } = string.Empty;
        public string Resolution { get; set; } = string.Empty;
        public bool HasWiFi { get; set; }
        public bool HasTouchScreen { get; set; }
        public string MaxDimensions { get; set; } = string.Empty;
        public double Price { get; set; }
        public string Type { get; set; } = string.Empty; // Ex. "Resin" sau "Filament"

        // Specific pentru imprimante de tip Resin
        public double? ResinTankCapacity { get; set; }
        public string? LightSourceType { get; set; }

        // Specific pentru imprimante de tip Filament
        public double? FilamentDiameter { get; set; }
        public List<string>? SupportedMaterials { get; set; }
    }
}
