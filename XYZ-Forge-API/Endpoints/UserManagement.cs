using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using XYZForge.Helpers;
using XYZForge.Models;
using XYZForge.Services;

namespace XYZForge.Endpoints
{
    public static class UserApiEndpoints
    {
        public static void MapUserEndpoints(this WebApplication app)
        {
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");

            if (secretKey == null)
            {
                logger.LogError("Failed to load JWT secret key");
                app.Lifetime.StopApplication();
            }

            app.MapGet("/get-user-data", async (string Username, MongoDBService mongoDbService) =>
            {
                var user = await mongoDbService.GetUserByUsernameAsync(Username);
                return user is null
                    ? Results.NotFound("User not found")
                    : Results.Ok(new { user.Username, user.Password, user.Role });
            });

            app.MapPost("/register", async (UserRegistration req, MongoDBService mongoDbService) =>
            {
                var existingUser = await mongoDbService.GetUserByUsernameAsync(req.Username);
                if (existingUser != null)
                    return Results.BadRequest("User already exists");

                var newUser = new User
                {
                    Username = req.Username,
                    Password = BCrypt.Net.BCrypt.HashPassword(req.Password),
                    Role = string.IsNullOrEmpty(req.Role) ? "User" : req.Role
                };

                await mongoDbService.CreateUserAsync(newUser);
                return Results.Ok("User registered successfully");
            });

            app.MapPost("/login", async (UserLogin req, MongoDBService mongoDbService) =>
            {
                var user = await mongoDbService.GetUserByUsernameAsync(req.Username);
                if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.Password))
                    return Results.Unauthorized();

                var token = JwtHelper.GenerateJwtToken(user.Username, user.Role);
                return Results.Ok(new { Token = token });
            });

            app.MapPost("/delete-user", async (UserDelete req, MongoDBService mongoDbService, ILogger<Program> logger) =>
            {
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();

                try
                {
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

                    var principal = handler.ValidateToken(req.IssuerJWT, validatorParams, out var _);

                    var usernameClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
                    var roleClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

                    if (usernameClaim == null || roleClaim == null)
                    {
                        logger.LogWarning("Invalid Token: Missing Claims");
                        return Results.BadRequest("Invalid Token");
                    }

                    var userToDelete = await mongoDbService.GetUserByUsernameAsync(req.Username);
                    if (userToDelete == null)
                    {
                        logger.LogWarning($"User {req.Username} not found");
                        return Results.NotFound("User not found");
                    }

                    if (usernameClaim != req.Username && roleClaim != "Admin")
                    {
                        logger.LogWarning($"Unauthorized delete attempt on {req.Username} by {usernameClaim}");
                        return Results.Forbid();
                    }

                    await mongoDbService.DeleteUserAsync(req.Username);
                    logger.LogInformation($"User {req.Username} deleted successfully by {usernameClaim}");

                    return Results.Ok($"User {req.Username} deleted successfully");
                }
                catch (Exception ex)
                {
                    logger.LogError($"Token validation failed: {ex.Message}");
                    return Results.Unauthorized();
                }
            });


            app.MapPost("/update-user", async (UserUpdate req, MongoDBService mongoDbService, ILogger<Program> logger) =>
            {
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();

                try
                {
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

                    var principal = handler.ValidateToken(req.IssuerJWT, validatorParams, out var _);

                    var usernameClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
                    var roleClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

                    if (usernameClaim == null || roleClaim == null)
                    {
                        logger.LogWarning("Invalid Token: Missing Claims");
                        return Results.BadRequest("Invalid Token");
                    }

                    var targetUser = await mongoDbService.GetUserByUsernameAsync(req.Username);
                    if (targetUser == null)
                    {
                        logger.LogWarning($"User {req.Username} not found");
                        return Results.NotFound("User not found");
                    }

                    if (roleClaim != "Admin" && usernameClaim != req.Username)
                    {
                        logger.LogWarning($"Unauthorized update attempt on {req.Username} by {usernameClaim}");
                        return Results.Forbid();
                    }

                    if (!string.IsNullOrEmpty(req.TargetRole))
                    {
                        if (roleClaim != "Admin")
                        {
                            logger.LogWarning($"Unauthorized role change attempt on {req.Username} by {usernameClaim}");
                            return Results.Forbid();
                        }

                        targetUser.Role = req.TargetRole;
                    }

                    if (!string.IsNullOrEmpty(req.TargetUsername)) targetUser.Username = req.TargetUsername;
                    if (!string.IsNullOrEmpty(req.TargetPassword))
                        targetUser.Password = BCrypt.Net.BCrypt.HashPassword(req.TargetPassword);

                    await mongoDbService.UpdateUserAsync(req.Username, targetUser);
                    logger.LogInformation($"User {req.Username} updated successfully by {usernameClaim}");

                    return Results.Ok($"User {targetUser.Username} updated successfully");
                }
                catch (Exception ex)
                {
                    logger.LogError($"Token validation failed: {ex.Message}");
                    return Results.Unauthorized();
                }
            });

        }
    }
}
const double CostPlasticPerGram = 0.5; 
    const double CostResinPerGram = 1.2;  

    static void Main(string[] args)
    {
        while (true)
        {
            Console.WriteLine("1. Vizualizeaza costurile");
            Console.WriteLine("2. Calculeaza costul unui obiect");
            Console.WriteLine("3. Comanda un obiect");
            Console.WriteLine("4. Iesire");
            Console.Write("Alegeti o optiune: ");

            string option = Console.ReadLine();

            switch (option)
            {
                case "1":
                    ShowCosts();
                    break;
                case "2":
                    CalculateObjectCost();
                    break;
                case "3":
                    PlaceOrder();
                    break;
                case "4":
                    Console.WriteLine("La revedere!");
                    return;
                default:
                    Console.WriteLine("Optiune invalida.");
                    break;
            }
        }
    }

    static void ShowCosts()
    {
        Console.WriteLine("\nCosturi pentru printare 3D:");
        Console.WriteLine($"- Plastic: {CostPlasticPerGram} RON/g");
        Console.WriteLine($"- Resin: {CostResinPerGram} RON/g");
    }

    static void CalculateObjectCost()
    {
        Console.Write("\nIntroduceti tipul de printare (plastic/resin): ");
        string type = Console.ReadLine()?.ToLower();

        Console.Write("Introduceti greutatea obiectului (g): ");
        if (!double.TryParse(Console.ReadLine(), out double weight) || weight <= 0)
        {
            Console.WriteLine("Greutate invalida. Incercati din nou.");
            return;
        }

        double cost = 0;
        switch (type)
        {
            case "plastic":
                cost = weight * CostPlasticPerGram;
                break;
            case "resin":
                cost = weight * CostResinPerGram;
                break;
            default:
                Console.WriteLine("Tip de printare invalid.");
                return;
        }

        Console.WriteLine($"Costul estimat pentru un obiect de {weight}g cu tipul {type}: {cost:F2} RON");
    }

    static void PlaceOrder()
    {
        Console.Write("\nIntroduceti numele obiectului: ");
        string name = Console.ReadLine();

        Console.Write("Introduceti greutatea obiectului (g): ");
        if (!double.TryParse(Console.ReadLine(), out double weight) || weight <= 0)
        {
            Console.WriteLine("Greutate invalidă.");
            return;
        }

        Console.Write("Introduceti culoarea obiectului: ");
        string color = Console.ReadLine();

        Console.Write("Introduceti adresa de livrare: ");
        string address = Console.ReadLine();

        Console.Write("Introduceti tipul de printare (plastic/resin): ");
        string type = Console.ReadLine()?.ToLower();

        double cost = 0;
        switch (type)
        {
            case "plastic":
                cost = weight * CostPlasticPerGram;
                break;
            case "resin":
                cost = weight * CostResinPerGram;
                break;
            default:
                Console.WriteLine("Tip de printare invalid. Comanda a fost anulată.");
                return;
        }

        Console.WriteLine("\nDetalii comanda:");
        Console.WriteLine($"- Nume obiect: {name}");
        Console.WriteLine($"- Greutate: {weight}g");
        Console.WriteLine($"- Culoare: {color}");
        Console.WriteLine($"- Adresa de livrare: {address}");
        Console.WriteLine($"- Tip printare: {type}");
        Console.WriteLine($"- Cost total: {cost:F2} RON");
        Console.WriteLine("Comanda a fost plasată cu succes!");
    }
    



