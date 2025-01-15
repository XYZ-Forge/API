using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
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

            if (string.IsNullOrWhiteSpace(secretKey))
            {
                logger.LogError("Failed to load JWT secret key");
                throw new InvalidOperationException("JWT secret key is not configured.");
            }

            app.MapGet("/get-user-data", async (string Username, [FromServices] MongoDBService mongoDbService) =>
            {
                var user = await mongoDbService.GetUserByUsernameAsync(Username);
                return user is null
                    ? Results.NotFound("User not found")
                    : Results.Ok(new { user.Username, user.Password, user.Role, user.TokenVersion });
            });

            app.MapPost("/register", async ([FromBody] UserRegistration req, [FromServices] MongoDBService mongoDbService) =>
            {
                var existingUser = await mongoDbService.GetUserByUsernameAsync(req.Username);
                if (existingUser != null)
                    return Results.BadRequest("User already exists");

                if (req.Role != "Admin" && req.Role != "User")
                    return Results.BadRequest("Invalid role");

                if (req.Role == "Admin")
                {
                    var principal = JwtHelper.ValidateToken(req.IssuerJWT, secretKey, logger);
                    if (principal == null)
                        return Results.BadRequest("Invalid or expired token.");

                    var roleClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                    if (roleClaim != "Admin")
                        return Results.BadRequest("Only admins can register other admins.");
                }

                var newUser = new User
                {
                    Username = req.Username,
                    Password = BCrypt.Net.BCrypt.HashPassword(req.Password),
                    Role = req.Role ?? "User"
                };

                await mongoDbService.CreateUserAsync(newUser);
                return Results.Ok("User registered successfully");
            });

            app.MapPost("/login", async ([FromBody] UserLogin req, [FromServices] MongoDBService mongoDbService) =>
            {
                var user = await mongoDbService.GetUserByUsernameAsync(req.Username);
                if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.Password))
                    return Results.BadRequest("Invalid username or password.");

                user.TokenVersion++;
                await mongoDbService.UpdateUserAsync(user.Username, user);

                var token = JwtHelper.GenerateJwtToken(user.Username, user.Role, user.TokenVersion);

                bool needToChangePassword = BCrypt.Net.BCrypt.Verify("Admin", user.Password) && user.Username == "Admin";

                return Results.Ok(new { Token = token, NeedToChangePassword = needToChangePassword });
            });

            app.MapPost("/logout", async ([FromBody] UserLogout req, [FromServices] MongoDBService mongoDbService) =>
            {
                var principal = JwtHelper.ValidateToken(req.IssuerJWT, secretKey, logger);
                if (principal == null)
                    return Results.BadRequest("Invalid or expired token.");

                var usernameClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
                var tokenVersionClaim = principal.Claims.FirstOrDefault(c => c.Type == "TokenVersion")?.Value;

                if (string.IsNullOrWhiteSpace(usernameClaim) || string.IsNullOrWhiteSpace(tokenVersionClaim))
                    return Results.BadRequest("Invalid token claims.");

                var user = await mongoDbService.GetUserByUsernameAsync(usernameClaim);
                if (user == null || user.TokenVersion.ToString() != tokenVersionClaim)
                    return Results.BadRequest("Invalid or expired token.");

                user.TokenVersion++;
                await mongoDbService.UpdateUserAsync(user.Username, user);

                logger.LogInformation($"User {user.Username} logged out successfully");
                return Results.Ok("Logged out successfully");
            });

            app.MapDelete("/delete-user", async ([FromBody] UserDelete req, [FromServices] MongoDBService mongoDbService) =>
            {
                var principal = JwtHelper.ValidateToken(req.IssuerJWT, secretKey, logger);
                if (principal == null)
                    return Results.BadRequest("Invalid or expired token.");

                var usernameClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
                var roleClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

                if (string.IsNullOrWhiteSpace(usernameClaim) || string.IsNullOrWhiteSpace(roleClaim))
                    return Results.BadRequest("Invalid token claims.");

                var userToDelete = await mongoDbService.GetUserByUsernameAsync(req.Username);
                if (userToDelete == null)
                    return Results.NotFound("User not found");

                if (usernameClaim != req.Username && roleClaim != "Admin")
                    return Results.BadRequest("Unauthorized delete attempt.");

                await mongoDbService.DeleteUserAsync(req.Username);
                logger.LogInformation($"User {req.Username} deleted successfully by {usernameClaim}");
                return Results.Ok($"User {req.Username} deleted successfully");
            });

            app.MapPost("/update-user", async ([FromBody] UserUpdate req, [FromServices] MongoDBService mongoDbService) =>
            {
                var principal = JwtHelper.ValidateToken(req.IssuerJWT, secretKey, logger);
                if (principal == null)
                    return Results.BadRequest("Invalid or expired token.");

                var usernameClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
                var roleClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

                if (string.IsNullOrWhiteSpace(usernameClaim) || string.IsNullOrWhiteSpace(roleClaim))
                    return Results.BadRequest("Invalid token claims.");

                var targetUser = await mongoDbService.GetUserByUsernameAsync(req.Username);
                if (targetUser == null)
                    return Results.NotFound("User not found");

                if (roleClaim != "Admin" && usernameClaim != req.Username)
                    return Results.BadRequest("Unauthorized update attempt.");

                if (!string.IsNullOrWhiteSpace(req.TargetRole) && roleClaim == "Admin")
                    targetUser.Role = req.TargetRole;

                if (!string.IsNullOrWhiteSpace(req.TargetUsername))
                    targetUser.Username = req.TargetUsername;

                if (!string.IsNullOrWhiteSpace(req.TargetPassword))
                    targetUser.Password = BCrypt.Net.BCrypt.HashPassword(req.TargetPassword);

                await mongoDbService.UpdateUserAsync(req.Username, targetUser);
                return Results.Ok($"User {targetUser.Username} updated successfully");
            });
        }
    }
}
