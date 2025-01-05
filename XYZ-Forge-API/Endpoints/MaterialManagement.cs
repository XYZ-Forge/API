using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using XYZForge.Models;
using XYZForge.Services;

namespace XYZForge.Endpoints
{
    public static class MaterialApiEndpoints
    {
        public static void MapMaterialEndpoints(this WebApplication app)
        {
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");

            if (string.IsNullOrEmpty(secretKey))
            {
                logger.LogError("Failed to load JWT secret key");
                throw new InvalidOperationException("JWT secret key is not configured.");
            }

            app.MapGet("/materials", async ([FromServices] MongoDBService mongoDbService, string? type = null) =>
            {
                var materials = await mongoDbService.GetMaterialsAsync();

                if (!materials.Any())
                {
                    return Results.NotFound("No materials found in the database.");
                }

                if (type == "Resin")
                {
                    var resinMaterials = materials.OfType<Resin>().ToList();
                    return Results.Ok(new { materials = resinMaterials });
                }
                else if (type == "Filament")
                {
                    var filamentMaterials = materials.OfType<Filament>().ToList();
                    return Results.Ok(new { materials = filamentMaterials });
                }
                else
                {
                    var resinMaterials = materials.OfType<Resin>().ToList();
                    var filamentMaterials = materials.OfType<Filament>().ToList();

                    return Results.Ok(new { resin = resinMaterials, filament = filamentMaterials });
                }
            });

            app.MapPost("/add-material", async ([FromBody] JsonElement req, [FromServices] MongoDBService mongoDbService, ILogger<Program> logger) =>
            {
                if (!req.TryGetProperty("IssuerJWT", out var issuerJwtElement) || string.IsNullOrEmpty(issuerJwtElement.GetString()))
                {
                    return Results.BadRequest("Invalid or missing 'IssuerJWT' in the request payload.");
                }

                var issuerJwt = issuerJwtElement.GetString();
                if(issuerJwt == null) {
                    logger.LogError("Failed to get the JWT token from the request");
                    return Results.BadRequest("Failed to get JWT from request");
                }

                var principal = ValidateToken(issuerJwt, secretKey, logger);
                if (principal == null)
                {
                    return Results.BadRequest("Invalid or expired token.");
                }

                var roleClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                if (roleClaim != "Admin")
                {
                    return Results.BadRequest("Only Admins can add materials.");
                }

                if (!req.TryGetProperty("Type", out var typeElement) || string.IsNullOrEmpty(typeElement.GetString()))
                {
                    return Results.BadRequest("Invalid or missing 'Type' property in the request payload.");
                }

                var type = typeElement.GetString();
                try
                {
                    Material mat = type switch
                    {
                        "Resin" => JsonSerializer.Deserialize<AddResin>(req.GetRawText()) switch
                        {
                            AddResin resin => new Resin
                            {
                                Name = resin.Name,
                                Type = resin.Type,
                                Color = resin.Color,
                                Viscosity = resin.Viscosity,
                                Price = resin.Price,
                                RemainingQuantity = resin.RemainingQuantity
                            },
                            _ => throw new JsonException("Invalid Resin data")
                        },
                        "Filament" => JsonSerializer.Deserialize<AddFilament>(req.GetRawText()) switch
                        {
                            AddFilament filament => new Filament
                            {
                                Name = filament.Name,
                                Type = filament.Type,
                                Color = filament.Color,
                                MaterialType = filament.MaterialType,
                                Diameter = filament.Diameter,
                                Price = filament.Price,
                                RemainingQuantity = filament.RemainingQuantity
                            },
                            _ => throw new JsonException("Invalid Filament data")
                        },
                        _ => throw new JsonException($"Unsupported material type: {type}")
                    };

                    // TODO: If material already exists, the stock for that material should be increased
                    await mongoDbService.CreateMaterialAsync(mat);
                    return Results.Ok(new { message = "Material added successfully", mat });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error adding material");
                    return Results.Problem($"An error occurred while adding material. Please try again.");
                }
            });

            app.MapPost("/material/search", async ([FromBody] SearchMaterial req, [FromServices] MongoDBService mongoDbService) => {
                var principal = ValidateToken(req.IssuerJWT, secretKey, logger);
                if(principal == null) {
                    return Results.BadRequest("Invalid or expired token.");
                }

                var roleClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                if(roleClaim != "Admin") {
                    return Results.BadRequest("Only admins can search for materials");
                }

                try {
                    var res = await mongoDbService.FilterMaterialsAsync(req.Name, req.Type, req.Color);
                    
                    return res.Any() 
                        ? Results.Ok(new { res })
                        : Results.NotFound(new { message = "No items with those filters found in the database" });
                } catch(Exception ex) {
                    logger.LogError(ex, "Error searching for material");
                    return Results.BadRequest("Error searching for material");
                }
            });

            app.MapDelete("/material/name/{name}", async ([FromBody] DeleteMaterials req, [FromServices] MongoDBService mongoDbService, string name) =>
            {
                var principal = ValidateToken(req.IssuerJWT, secretKey, logger);
                if (principal == null)
                {
                    return Results.BadRequest("Invalid or expired token.");
                }

                var roleClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                if (roleClaim != "Admin")
                {
                    return Results.BadRequest("Only Admins can delete materials.");
                }

                try
                {
                    var res = await mongoDbService.DeleteMaterialAsync(name);
                    return res.DeletedCount == 0
                        ? Results.NotFound(new { message = $"Material with name '{name}' not found" })
                        : Results.Ok(new { message = $"Material with name '{name}' deleted successfully" });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error deleting material");
                    return Results.Problem($"An error occurred while deleting the material '{name}'.");
                }
            });
        }

        private static ClaimsPrincipal? ValidateToken(string token, string secretKey, ILogger logger)
        {
            if (string.IsNullOrEmpty(token))
            {
                logger.LogWarning("Token is null or empty.");
                return null;
            }

            try
            {
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var validatorParams = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = "XYZ-Forge",
                    ValidAudience = "XYZ-Forge-User",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
                };

                return handler.ValidateToken(token, validatorParams, out _);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "JWT validation failed");
                return null;
            }
        }
    }
}
