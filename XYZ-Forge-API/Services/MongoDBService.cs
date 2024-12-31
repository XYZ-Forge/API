using MongoDB.Driver;
using XYZForge.Models;

namespace XYZForge.Services
{
    public class MongoDBService
    {
        private readonly IMongoCollection<User> _usersCollection;
        private readonly ILogger<MongoDBService> _logger;

        public MongoDBService(IConfiguration configuration, ILogger<MongoDBService> logger)
        {
            _logger = logger;

            try
            {
                var mongoClient = new MongoClient(configuration["MONGODB_CONNECTION_STRING"]);
                var mongoDatabase = mongoClient.GetDatabase(configuration["MONGODB_DATABASE_NAME"]);
                _usersCollection = mongoDatabase.GetCollection<User>("Users");
                _logger.LogInformation("Connected to MongoDB successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to connect to MongoDB: {ex.Message}");
                throw;
            }
        }

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
    }
}
