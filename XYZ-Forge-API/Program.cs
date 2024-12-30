using XYZForge.Extensions;
using XYZForge.Endpoints;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureServices();
builder.Services.AddLogging();

var app = builder.Build();
app.ConfigurePipeline();

app.MapEndpoints();

app.Run();
