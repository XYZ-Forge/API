using XYZForge.Models;
using XYZForge.Services;
using MongoDB.Driver;


namespace XYZForge.Endpoints {
    public static class OrderApiEndpoints
    {
        public static void MapOrderEndpoints(this WebApplication app) {
            var logger =  app.Services.GetRequiredService<ILogger<Program>>();
            var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");

            if (secretKey == null) {
                logger.LogError("Failed to load JWT secret key");
                app.Lifetime.StopApplication();
            }
        }
    }
    public class OrderService
    {
        private readonly IMongoCollection<Order> _orderCollection;

        public OrderService(IMongoDatabase database)
        {
            _orderCollection = database.GetCollection<Order>("Orders");
        }

        public async Task<Order> PlaceOrderAsync(Order order) //Saves a new order to the database
        {
            await _orderCollection.InsertOneAsync(order);
            return order;
        }

        public async Task<IEnumerable<Order>> GetOrdersAsync() //Retrieves all orders from the database
        {
            return await _orderCollection.Find(_ => true).ToListAsync();
        }
    }
}
