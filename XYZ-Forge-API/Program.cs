using XYZForge.Extensions;
using XYZForge.Endpoints;
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureServices();
builder.Services.AddLogging();

var app = builder.Build();
app.ConfigurePipeline();

app.MapEndpoints();

app.Run();
