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

            string? secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
            if(secretKey == null) {
                logger.LogError("Failed to load JWT secret key");
                app.Lifetime.StopApplication();
            }

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

            app.MapPost("/logout", () => {
                return Results.Ok("Logout successful"); // The logout processed is managed by express session
            });

            app.MapPost("/delete-user", (UserDelete req) => {
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var logger = app.Services.GetRequiredService<ILogger<Program>>();

                try {
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

                    if(usernameClaim == null || roleClaim == null) {
                        logger.LogWarning("Invalid Token: Missing Claims");
                        return Results.BadRequest("Invalid Token");
                    }

                    var userToDelete = users.FirstOrDefault(u => u.Username == req.Username);
                    if(userToDelete == null) {
                        return Results.NotFound("User not found");
                    }

                    if(usernameClaim != req.Username && roleClaim != "Admin") {
                        return Results.Forbid();
                    }

                    users.Remove(userToDelete);
                    logger.LogInformation($"User {req.Username} deleted by {usernameClaim}");

                    return Results.Ok($"User {req.Username} deleted sucessfully");
                    
                } catch(Exception ex) {
                    logger.LogError($"Token validation failed: {ex.Message}");
                    return Results.Unauthorized();
                }
            });

            app.MapPost("/update-user", (UserUpdate req) =>
            {
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var logger = app.Services.GetRequiredService<ILogger<Program>>();

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

                    var usernameClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
                    if (usernameClaim is null) {
                        logger.LogWarning("Role claim is missing in the token.");
                        return Results.BadRequest("Role claim is missing in the token.");
                    }

                    var user = users.FirstOrDefault(u => u.Username == usernameClaim.Value);    
                    if(user is null) {
                        return Results.NotFound("User not found");
                    }

                    var targetUser = users.FirstOrDefault(u => u.Username == req.Username);
                    if (targetUser is null) {
                        logger.LogWarning($"User not found: {req.Username}");
                        return Results.NotFound("User not found.");
                    }

                    if (!string.IsNullOrEmpty(req.TargetRole)) {
                        if (user.Role != "Admin") {
                            logger.LogWarning($"Access denied. Role: {user.Role}");
                            return Results.Forbid();
                        }
                        targetUser.Role = req.TargetRole;
                    }

                    if (!string.IsNullOrEmpty(req.TargetUsername)) targetUser.Username = req.TargetUsername;
                    if (!string.IsNullOrEmpty(req.TargetPassword))
                        targetUser.Password = BCrypt.Net.BCrypt.HashPassword(req.TargetPassword);

                    logger.LogInformation($"User {targetUser.Username} updated successfully.");
                    return Results.Ok($"User {targetUser.Username} updated successfully.");
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
