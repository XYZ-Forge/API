using MongoDB.Driver;
using XYZForge.Models;

namespace XYZForge.Services
{
    public class MongoDBService
    {
        private readonly IMongoCollection<User> _usersCollection;
        private readonly IMongoCollection<Material> _materialsCollection;
        private readonly ILogger<MongoDBService> _logger;

        public MongoDBService(IConfiguration configuration, ILogger<MongoDBService> logger)
        {
            _logger = logger;

            try
            {
                var mongoClient = new MongoClient(configuration["MONGODB_CONNECTION_STRING"]);
                var mongoDatabase = mongoClient.GetDatabase(configuration["MONGODB_DATABASE_NAME"]);
                _usersCollection = mongoDatabase.GetCollection<User>("Users");
                _materialsCollection = mongoDatabase.GetCollection<Material>("Materials");
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
        
        public async Task<List<Material>> FilterMaterialsAsync(string? name = null, string? type = null, string? color = null)
        {
            var filterBuilder = Builders<Material>.Filter;
            var filters = new List<FilterDefinition<Material>>();

            if (!string.IsNullOrEmpty(name))
                filters.Add(filterBuilder.Eq(material => material.Name, name));
            if (!string.IsNullOrEmpty(type))
                filters.Add(filterBuilder.Eq(material => material.Type, type));
            if (!string.IsNullOrEmpty(color))
                filters.Add(filterBuilder.Eq(material => material.Color, color));

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
    }
}
