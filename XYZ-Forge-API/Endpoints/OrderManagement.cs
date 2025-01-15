using Microsoft.AspNetCore.Mvc;
using XYZForge.Models;
using XYZForge.Services;
using XYZForge.Helpers;
using MongoDB.Driver;
using System.Security.Claims;


namespace XYZForge.Endpoints {
    public static class OrderApiEndpoints
    {
        public static void MapOrderEndpoints(this WebApplication app) {
            var logger =  app.Services.GetRequiredService<ILogger<Program>>();
            var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");

            if (string.IsNullOrEmpty(secretKey)) {
                logger.LogError("Failed to load JWT secret key");
                app.Lifetime.StopApplication();
            }
            
            app.MapPost("/orders", async ([FromBody] GetOrders req, [FromServices] MongoDBService mongoDbService) => 
            {
                if(req.IssuerJWT == null) {
                    return Results.BadRequest("Missing JWT token");
                }
                if (secretKey == null) {
                    return Results.BadRequest("JWT secret key is not configured.");
                }
                var principal = JwtHelper.ValidateToken(req.IssuerJWT, secretKey, logger);
                if (principal == null)
                    return Results.BadRequest("Invalid or expired token.");
                var roleClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                if (roleClaim != "Admin")
                    return Results.BadRequest("Only admins can access this route.");
                
                var orders = await mongoDbService.GetOrdersAsync();

                if (!orders.Any())
                {
                    return Results.NotFound("No orders in the database");
                }

                return Results.Ok(new { orders });
            });

            app.MapPost("/add-order", async ([FromBody] AddOrders req, [FromServices] MongoDBService mongoDbService) =>
            {
        
                try
                {
                    var newOrder = new Order
                    {
                        ObjectName = req.ObjectName,
                        Weight = req.Weight,
                        Dimensions = req.Dimensions,
                        Color = req.Color,
                        Address = req.Address,
                        MaterialType = req.MaterialType,
                        TotalCost = req.TotalCost
                    };
                    await mongoDbService.CreateOrderAsync(newOrder);
                    return Results.Ok(new { message = "Order added successfully", newOrder });
                }
                catch (Exception ex)
                {
                    return Results.Problem($"Failed to add order. Error: {ex.Message}");
                }
            });

            app.MapPost("/orders/search", async ([FromBody] SearchOrders req, [FromServices] MongoDBService mongoDbService) =>
            {
                if(req.IssuerJWT == null) {
                    return Results.BadRequest("Missing JWT token");
                }

                if (secretKey == null) {
                    return Results.BadRequest("JWT secret key is not configured.");
                }

                var principal = JwtHelper.ValidateToken(req.IssuerJWT, secretKey, logger);
                if (principal == null){
                    return Results.BadRequest("Invalid or expired token.");
                }

                var roleClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                if (roleClaim != "Admin"){
                    return Results.BadRequest("Only admins can search for printers.");
                }

                var res = await mongoDbService.SearchOrdersAsync(req.id, req.ObjectName, req.Weight, req.Dimensions, req.Color, req.Address, req.MaterialType, req.TotalCost);
                return res.Any() ? Results.Ok(res) : Results.NotFound("No orders found");
            });

            app.MapPost("/orders/delete", async ([FromBody] DeleteOrders req, [FromServices] MongoDBService mongoDbService) =>
            {
                if(req.IssuerJWT == null) {
                    return Results.BadRequest("Missing JWT token");
                }

                if (secretKey == null) {
                    return Results.BadRequest("JWT secret key is not configured.");
                }

                var principal = JwtHelper.ValidateToken(req.IssuerJWT, secretKey, logger);

                if (principal == null){
                    return Results.BadRequest("Invalid or expired token.");
                }

                var roleClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

                if (roleClaim != "Admin"){
                    return Results.BadRequest("Only admins can delete orders.");
                }

                if (string.IsNullOrWhiteSpace(req.id))
                {
                    return Results.BadRequest("Order ID is required");
                }

                var order = await mongoDbService.GetOrderByIdAsync(req.id);
                if (order == null)
                {
                    return Results.NotFound("Order not found");
                }

                await mongoDbService.DeleteOrderAsync(req.id);
                return Results.Ok(new { message = "Order deleted successfully" });
            });

            app.MapPost("/orders/update", async ([FromBody] UpdateOrders req, [FromServices] MongoDBService mongoDbService) =>
            {
                if(req.IssuerJWT == null) {
                    return Results.BadRequest("Missing JWT token");
                }

                if (secretKey == null) {
                    return Results.BadRequest("JWT secret key is not configured.");
                }

                var principal = JwtHelper.ValidateToken(req.IssuerJWT, secretKey, logger);

                if (principal == null){
                    return Results.BadRequest("Invalid or expired token.");
                }

                if (string.IsNullOrWhiteSpace(req.Address))
                {
                    return Results.BadRequest("Address is required");
                }

                var order = await mongoDbService.GetOrderByIdAsync(req.Address);

                if (order == null)
                {
                    return Results.NotFound("Order not found");
                }

                try
                {
                    var updateOrder = new Order
                    {
                        Address = req.Address,
                    };
                    
                    await mongoDbService.UpdateOrderAsync(req.Address, updateOrder);
                    return Results.Ok(new { message = "Order updated successfully", updateOrder });
                }
                catch (Exception ex)
                {
                    return Results.Problem($"Failed to update the order. Error: {ex.Message}");
                }
            });
        }
    }
}
