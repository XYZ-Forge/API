using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace XYZForge.Models {
    
    [BsonDiscriminator(RootClass = true)]
    [BsonKnownTypes(typeof(Resin), typeof(Filament))]
    public abstract record Material {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        [BsonElement("Material")]
        public string Name { get; set; } = string.Empty;
        [BsonElement("Type")]
        public string Type { get; set; } = string.Empty;
        [BsonElement("Color")]
        public string Color { get; set; } = string.Empty;
        [BsonElement("Price")]
        public double Price { get; set; } = 0;
    }

    public record Resin : Material {
        [BsonElement("Viscosity")]
        public double Viscosity { get; set; }
    }

    public record Filament : Material {
        [BsonElement("MaterialType")]
        public string MaterialType { get; set; } = string.Empty;
        [BsonElement("Diameter")]
        public double Diameter { get; set; }
    }

    public record GetMaterials(string IssuerJWT);
    public abstract record AddMaterial(string IssuerJWT, string Name, string Type, string Color, double Price);

    public record AddResin(string IssuerJWT, string Name, string Color, double Price, double Viscosity) 
        : AddMaterial(IssuerJWT, Name, "Resin", Color, Price);

    public record AddFilament(string IssuerJWT, string Name, string Color, double Price, string MaterialType, double Diameter) 
        : AddMaterial(IssuerJWT, Name, "Filament", Color, Price);
    
    public record SearchMaterial(string IssuerJWT, string? Name = "", string? Type = "", string? Color = "");

}