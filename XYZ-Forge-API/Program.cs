using XYZForge.Extensions;
using XYZForge.Endpoints;
using XYZForge.Services;
using XYZForge.Models;
using DotNetEnv;
using System.Text;

Env.Load();

// Check if all required environment variables are set
var envVars = new string[] { "JWT_SECRET_KEY", "MONGODB_CONNECTION_STRING", "MONGODB_DATABASE_NAME" };
var errorMessages = new StringBuilder();
foreach (var envVar in envVars)
{
    if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(envVar)))
    {
        errorMessages.AppendLine($"Environment variable {envVar} is not set");
    }
}

if (errorMessages.Length > 0)
{
    Console.WriteLine(errorMessages.ToString());
    Console.WriteLine("Please set the required environment variables and restart the application");
    return;
}

var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services.ConfigureServices();
builder.Services.AddLogging();
builder.Services.AddHostedService<AdminUserInit>();

var app = builder.Build();

// Configure pipeline and map endpoints
app.ConfigurePipeline();
app.MapUserEndpoints();
app.MapMaterialEndpoints();
app.MapPrinterEndpoints();
app.UseRouting();

app.Run();

public class AdminUserInit : IHostedService
{
    private readonly MongoDBService _mongoDBService;
    private readonly ILogger<AdminUserInit> _logger;

    public AdminUserInit(MongoDBService mongoDBService, ILogger<AdminUserInit> logger)
    {
        _mongoDBService = mongoDBService;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancelToken)
    {
        var existingUser = await _mongoDBService.GetUserByUsernameAsync("Admin");
        if (existingUser == null)
        {
            var adminUser = new User
            {
                Username = "Admin",
                Password = BCrypt.Net.BCrypt.HashPassword("Admin"),
                Role = "Admin",
                TokenVersion = 0
            };

            await _mongoDBService.CreateUserAsync(adminUser);
            _logger.LogInformation("Default Admin user created; Admin:Admin");
        }
        else
        {
            _logger.LogInformation("Default Admin user already exists");
        }
    }

    public Task StopAsync(CancellationToken cancelToken) => Task.CompletedTask;
}