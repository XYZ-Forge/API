using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using XYZForge.Helpers;
using XYZForge.Models;

namespace XYZForge.Endpoints
{
    public static class ApiEndpoints
    {
        public static void MapEndpoints(this WebApplication app)
        {
            var users = new List<User>();
            var logger = app.Services.GetRequiredService<ILogger<Program>>();

            var hashedPass = BCrypt.Net.BCrypt.HashPassword("Admin");
            users.Add(new User { Username = "Admin", Password = hashedPass, Role = "Admin" });
            logger.LogInformation("Default admin creds: Admin:Admin");

            app.MapGet("/get-user-data", (string Username) =>
            {
                var user = users.FirstOrDefault(u => u.Username == Username);
                return user is null
                    ? Results.NotFound("User not found")
                    : Results.Ok(new { user.Username, user.Password, user.Role });
            });

            app.MapPost("/register", (UserRegistration req) =>
            {
                if (users.Any(u => u.Username == req.Username))
                    return Results.BadRequest("User already exists");

                users.Add(new User
                {
                    Username = req.Username,
                    Password = BCrypt.Net.BCrypt.HashPassword(req.Password),
                    Role = string.IsNullOrEmpty(req.Role) ? "User" : req.Role
                });
                return Results.Ok("User registered successfully");
            });

            app.MapPost("/login", (UserLogin req) =>
            {
                var user = users.FirstOrDefault(u => u.Username == req.Username);
                if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.Password))
                    return Results.Unauthorized();

                var token = JwtHelper.GenerateJwtToken(req.Username, user.Role);
                return Results.Ok(new { Token = token });
            });

            app.MapPost("/update-user", (UserUpdate req) =>
            {
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var logger = app.Services.GetRequiredService<ILogger<Program>>();

                try
                {
                    // Define token validation parameters
                    var validatorParams = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = "XYZ-Forge",
                        ValidAudience = "XYZ-Forge-User",
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("g93KsFp02+3BtpxgLM92sGytv4N32FbkXaPbG8TnxUs="))
                    };

                    // Validate the token
                    var principal = handler.ValidateToken(req.IssuerJWT, validatorParams, out var _);

                    // Extract role claim
                    var usernameClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
                    if (usernameClaim is null)
                    {
                        logger.LogWarning("Role claim is missing in the token.");
                        return Results.BadRequest("Role claim is missing in the token.");
                    }

                    var user = users.FirstOrDefault(u => u.Username == usernameClaim.Value);
                    if (user.Role != "Admin")
                    {
                        logger.LogWarning($"Access denied. Role: {user.Role}");
                        return Results.Forbid();
                    }

                    user = users.FirstOrDefault(u => u.Username == req.Username);
                    if (user is null)
                    {
                        logger.LogWarning($"User not found: {req.Username}");
                        return Results.NotFound("User not found.");
                    }

                    // Update user details
                    if (!string.IsNullOrEmpty(req.TargetRole)) user.Role = req.TargetRole;
                    if (!string.IsNullOrEmpty(req.TargetUsername)) user.Username = req.TargetUsername;
                    if (!string.IsNullOrEmpty(req.TargetPassword))
                        user.Password = BCrypt.Net.BCrypt.HashPassword(req.TargetPassword);

                    logger.LogInformation($"User {user.Username} updated successfully.");
                    return Results.Ok($"User {user.Username} updated successfully.");
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
