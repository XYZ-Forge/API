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
           
           app.MapPost("/orders/calculate-cost", async ([FromBody] CalculateCostRequest req, [FromServices] MongoDBService mongoDbService, ILogger<Program> logger) =>
{
    if (req.IssuerJWT == null)
    {
        return Results.BadRequest("Missing JWT token.");
    }

    var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
    if (string.IsNullOrEmpty(secretKey))
    {
        logger.LogError("JWT secret key is not configured.");
        return Results.BadRequest("JWT secret key is not configured.");
    }

    var principal = JwtHelper.ValidateToken(req.IssuerJWT, secretKey, logger);
    if (principal == null)
    {
        logger.LogWarning("Invalid or expired token.");
        return Results.BadRequest("Invalid or expired token.");
    }

    if (string.IsNullOrWhiteSpace(req.MaterialType) || string.IsNullOrWhiteSpace(req.Color) || req.Weight <= 0)
    {
        return Results.BadRequest("Invalid material type, color, or weight.");
    }

    var order = await mongoDbService.GetOrderByIdAsync(req.Id);
    if (order == null)
    {
        return Results.NotFound("Order not found.");
    }

    var materials = await mongoDbService.GetMaterialsAsync();

  
    var selectedMaterial = materials.FirstOrDefault(m =>
        string.Equals(m.Type.Trim(), req.MaterialType.Trim(), StringComparison.OrdinalIgnoreCase) &&
        string.Equals(m.Color.Trim(), req.Color.Trim(), StringComparison.OrdinalIgnoreCase));

    if (selectedMaterial == null)
    {
        logger.LogWarning($"Material not found for MaterialType: {req.MaterialType}, Color: {req.Color}");
        return Results.NotFound(new { message = "Material not found for the specified type and color." });
    }

    if (selectedMaterial.Price <= 0)
    {
        logger.LogWarning($"Invalid price for MaterialType: {req.MaterialType}, Color: {req.Color}");
        return Results.BadRequest("Material price must be greater than zero.");
    }


    double baseCost = selectedMaterial.Price * req.Weight;

    if (selectedMaterial is Filament filament)
    {
        baseCost *= filament.Diameter > 0 ? (filament.Diameter / 1.75) : 1;
    }
    else if (selectedMaterial is Resin resin)
    {
        baseCost *= resin.Viscosity > 0 ? (1 + (resin.Viscosity / 1000)) : 1;
    }

    var dimensionFactors = req.Dimensions.Split('x')
        .Select(dim => double.TryParse(dim.Trim(), out var d) ? d : 1)
        .Aggregate(1.0, (acc, val) => acc * val);

    if (dimensionFactors <= 0)
    {
        logger.LogWarning($"Invalid dimensions: {req.Dimensions}");
        dimensionFactors = 1; 
    }

    baseCost += dimensionFactors * 0.05;

    
    order.TotalCost = baseCost;
    await mongoDbService.UpdateOrderAsync(order.Id!, order);

    return Results.Ok(new
    {
        MaterialType = req.MaterialType,
        Color = req.Color,
        Weight = req.Weight,
        Dimensions = req.Dimensions,
        CalculatedCost = baseCost
    });
});
            app.MapPost("/orders/update", async ([FromBody] UpdateOrders req, [FromServices] MongoDBService mongoDbService) =>
            {
                if(req.IssuerJWT == null) {
                    return Results.BadRequest("Missing JWT token");
                }

                var principal = JwtHelper.ValidateToken(req.IssuerJWT, secretKey!, logger);

                if (principal == null){
                    return Results.BadRequest("Invalid or expired token.");
                }

                var roleClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                if(roleClaim != "Admin"){
                    return Results.BadRequest("Only admins can update orders.");
                }

                if (string.IsNullOrWhiteSpace(req.id))
                {
                    return Results.BadRequest("Order ID is required");
                }

                if (string.IsNullOrWhiteSpace(req.Address))
                {
                    return Results.BadRequest("Address is required");
                }

                var order = await mongoDbService.GetOrderByIdAsync(req.id);

                if (order == null)
                {
                    return Results.NotFound("Order not found");
                }

                try
                {
                    var updateOrder = new Order
                    {
                        Id = order.Id,
                        ObjectName = order.ObjectName,
                        Weight = order.Weight,
                        Dimensions = order.Dimensions,
                        Color = order.Color,
                        Address = req.Address,
                        MaterialType = order.MaterialType,
                        TotalCost = order.TotalCost
                    };
                    
                    await mongoDbService.UpdateOrderAsync(req.id, updateOrder);
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
