using XYZForge.Extensions;
using XYZForge.Endpoints;
using XYZForge.Services;
using XYZForge.Models;
using DotNetEnv;

Env.Load();

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
                Role = "Admin"
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