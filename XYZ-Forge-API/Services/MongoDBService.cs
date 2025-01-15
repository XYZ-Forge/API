using MongoDB.Driver;
using XYZForge.Models;

namespace XYZForge.Services
{
    public class MongoDBService
    {
        private readonly IMongoCollection<User> _usersCollection;
        private readonly IMongoCollection<Material> _materialsCollection;
        private readonly ILogger<MongoDBService> _logger;
        private readonly IMongoCollection<Printer> _printersCollection;

        public MongoDBService(IConfiguration configuration, ILogger<MongoDBService> logger)
        {
            _logger = logger;

            try
            {
                var mongoClient = new MongoClient(configuration["MONGODB_CONNECTION_STRING"]);
                var mongoDatabase = mongoClient.GetDatabase(configuration["MONGODB_DATABASE_NAME"]);
                _usersCollection = mongoDatabase.GetCollection<User>("Users");
                _materialsCollection = mongoDatabase.GetCollection<Material>("Materials");
                _printersCollection = mongoDatabase.GetCollection<Printer>("Printers");
                
                _logger.LogInformation("Connected to MongoDB successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to connect to MongoDB: {ex.Message}");
                throw;
            }
        }

        // User Management
        public async Task<List<User>> GetUsersAsync() =>
            await _usersCollection.Find(_ => true).ToListAsync();

        public async Task<User?> GetUserByUsernameAsync(string username) =>
            await _usersCollection.Find(user => user.Username == username).FirstOrDefaultAsync();

        public async Task CreateUserAsync(User newUser) =>
            await _usersCollection.InsertOneAsync(newUser);

        public async Task UpdateUserAsync(string username, User updatedUser) =>
            await _usersCollection.ReplaceOneAsync(user => user.Username == username, updatedUser);

        public async Task DeleteUserAsync(string username) =>
            await _usersCollection.DeleteOneAsync(user => user.Username == username);

        // Materials Management
        public async Task<List<Material>> GetMaterialsAsync() =>
            await _materialsCollection.Find(_ => true).ToListAsync() ?? new List<Material>();
        
        public async Task<List<Material>> FilterMaterialsAsync(string? name = null, string? type = null, string? color = null, double? price = null, double? remainingQuantity = null)
        {
            var filterBuilder = Builders<Material>.Filter;
            var filters = new List<FilterDefinition<Material>>();

            if (!string.IsNullOrWhiteSpace(name))
                filters.Add(filterBuilder.Eq(material => material.Name, name));
            if (!string.IsNullOrWhiteSpace(type))
                filters.Add(filterBuilder.Eq(material => material.Type, type));
            if (!string.IsNullOrWhiteSpace(color))
                filters.Add(filterBuilder.Eq(material => material.Color, color));
            if (price.HasValue)
                filters.Add(filterBuilder.Eq(material => material.Price, price.Value));
            if(remainingQuantity.HasValue) {
                if(remainingQuantity.Value == 0)
                    filters.Add(filterBuilder.Eq(material => material.RemainingQuantity, 0));
                else
                    filters.Add(filterBuilder.Gt(material => material.RemainingQuantity, 0));
            }

            var combinedFilter = filters.Count > 0
                ? filterBuilder.And(filters)
                : filterBuilder.Empty;

            return await _materialsCollection.Find(combinedFilter).ToListAsync();
        }

        public async Task CreateMaterialAsync(Material material) =>
            await _materialsCollection.InsertOneAsync(material);
        
        public async Task UpdateMaterialAsync(string materialName, Material material) =>
            await _materialsCollection.ReplaceOneAsync(material => material.Name == materialName, material);

        public async Task<DeleteResult> DeleteMaterialAsync(string materialName) =>
            await _materialsCollection.DeleteOneAsync(material => material.Name == materialName);
    
        //Printer Management

        public async Task<List<Printer>> GetPrintersAsync() =>
            await _printersCollection.Find(_ => true).ToListAsync();

        public async Task<Printer?> GetPrinterByIdAsync(string id) =>
            await _printersCollection.Find(printer => printer.Id == id).FirstOrDefaultAsync();

        public async Task AddPrinterAsync(Printer newPrinter) =>
            await _printersCollection.InsertOneAsync(newPrinter);

        public async Task UpdatePrinterAsync(string id, Printer updatedPrinter) =>
            await _printersCollection.ReplaceOneAsync(printer => printer.Id == id, updatedPrinter);

        public async Task<DeleteResult> DeletePrinterAsync(string id) =>
            await _printersCollection.DeleteOneAsync(printer => printer.Id == id);

        public async Task<List<Printer>> SearchPrintersAsync(string? id=null,string? name=null, string? resolution=null, bool? hasWiFi=null, bool? hasTouchScreen=null)
        {
            var filterBuilder = Builders<Printer>.Filter;
            var filters = new List<FilterDefinition<Printer>>();
            if(!string.IsNullOrWhiteSpace(id))
                filters.Add(filterBuilder.Eq(printer => printer.Id, id));
            if (!string.IsNullOrWhiteSpace(name))
                filters.Add(filterBuilder.Eq(printer => printer.PrinterName, name));
            if (!string.IsNullOrWhiteSpace(resolution))
                filters.Add(filterBuilder.Eq(printer => printer.Resolution, resolution));
            if (hasWiFi.HasValue)
                filters.Add(filterBuilder.Eq(printer => printer.HasWiFi, hasWiFi.Value));
            if (hasTouchScreen.HasValue)
                filters.Add(filterBuilder.Eq(printer => printer.HasTouchScreen, hasTouchScreen.Value));

            var combinedFilter = filters.Count > 0 ? filterBuilder.And(filters) : filterBuilder.Empty;
            return await _printersCollection.Find(combinedFilter).ToListAsync();
        }
        
    }
}
