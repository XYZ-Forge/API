using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using BCrypt.Net;
using Microsoft.OpenApi.Models;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);
var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options => {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = "XYZ-Forge",
                        ValidAudience = "XYZ-Forge-User",
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("g93KsFp02+3BtpxgLM92sGytv4N32FbkXaPbG8TnxUs="))
                    };

                    options.TokenValidationParameters.RoleClaimType = "Role";
                });

builder.Services.AddOpenApi();
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => {
    options.SwaggerDoc("v1", new OpenApiInfo {
        Title = "XYZ-Forge-API",
        Version = "V1",
        Description = "Our API",
        Contact = new OpenApiContact {
            Name = "XYZ-Forge",
            Email = "admin@xyz-forge.com"
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "XYZ-Forge-API");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

var users = new List<User>();

// Create default admin user
{
    var hashedPass = BCrypt.Net.BCrypt.HashPassword("Admin");
    users.Add(new User {
        Username = "Admin",
        Password = hashedPass,
        Role = "Admin"
    });
    logger.Log(LogLevel.Information, "Default admin creds: Admin:Admin");
}

// DEVELOPMENT! DO NOT PUT IN PROD

app.MapGet("/get-user-data", (string Username) => {
    if(string.IsNullOrEmpty(Username)) {
        return Results.BadRequest("No username specified");
    }

    var user = users.FirstOrDefault(u => u.Username == Username);

    if(user is null) {
        return Results.NotFound($"User with username \"{Username}\" not found");
    }

    return Results.Ok(new {
        username = user.Username,
        passwordHash = user.Password,
        role = user.Role
    });
});

app.MapPost("/register", (UserRegistration req) => {
    if(users.Any(u => u.Username == req.Username)) {
        return Results.BadRequest("User already exists");
    }

    var hashedPass = BCrypt.Net.BCrypt.HashPassword(req.Password);
    var role = string.IsNullOrEmpty(req.Role) ? "User" : req.Role;

    users.Add(new User { Username = req.Username, Password = hashedPass, Role = role });
    return Results.Ok("User registered successfully");
});

app.MapPost("/login", (UserLogin req) => {
    var user = users.FirstOrDefault(u => u.Username == req.Username);

    if(user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.Password)) {
        return Results.Unauthorized();
    }

    var token = GenerateJwtToken(req.Username, user.Role);
    return Results.Ok(new { Token = token });
});

app.MapPost("/update-user", (UserUpdate req) =>
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("g93KsFp02+3BtpxgLM92sGytv4N32FbkXaPbG8TnxUs="))
        };

        var principal = handler.ValidateToken(req.IssuerJWT, validatorParams, out var _);

        var roleClaim = principal.Claims.FirstOrDefault(c => c.Type == "Role");
        if (roleClaim is null)
        {
            logger.LogWarning("Role claim is missing in the token.");
            return Results.BadRequest("Role claim is missing in the token.");
        }

        if (roleClaim.Value != "Admin")
        {
            logger.LogWarning($"Access denied. Role: {roleClaim.Value}");
            return Results.Forbid();
        }
    }
    catch (Exception ex)
    {
        logger.LogError($"Token validation failed: {ex.Message}");
        return Results.Unauthorized();
    }

    var user = users.FirstOrDefault(u => u.Username == req.Username);
    if (user is null)
    {
        logger.LogWarning($"User not found: {req.Username}");
        return Results.NotFound("User not found.");
    }

    if (!string.IsNullOrEmpty(req.TargetRole) || 
        !string.IsNullOrEmpty(req.TargetUsername) || 
        !string.IsNullOrEmpty(req.TargetPassword))
    {
        if (!string.IsNullOrEmpty(req.TargetRole))
        {
            user.Role = req.TargetRole;
        }

        if (!string.IsNullOrEmpty(req.TargetUsername))
        {
            user.Username = req.TargetUsername;
        }

        if (!string.IsNullOrEmpty(req.TargetPassword))
        {
            user.Password = BCrypt.Net.BCrypt.HashPassword(req.TargetPassword);
        }

        logger.LogInformation($"User {user.Username} updated successfully.");
        return Results.Ok($"User {user.Username} updated successfully.");
    }

    logger.LogWarning("No valid update fields provided.");
    return Results.BadRequest("No valid update fields provided.");
});

app.Run();

string GenerateJwtToken(string username, string role) {
    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("g93KsFp02+3BtpxgLM92sGytv4N32FbkXaPbG8TnxUs="));
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

    var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
        issuer: "XYZ-Forge",
        audience: "XYZ-Forge-User",
        expires: DateTime.Now.AddHours(1),
        signingCredentials: credentials,
        claims: new[]
        {
            new Claim("name", username),
            new Claim("Role", role)
        }
    );

    return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
}

record User {
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
}

record UserRegistration(string Username, string Password, string? Role = "User");
record UserLogin(string Username, string Password);

record UserUpdate(string IssuerJWT, string Username, string TargetRole, string TargetUsername, string TargetPassword);