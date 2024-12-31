using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;
using XYZForge.Services;

namespace XYZForge.Extensions
{
    public static class ServiceExtensions
    {
        public static void ConfigureServices(this IServiceCollection services)
        {
            services.AddSingleton<MongoDBService>();
            
            string? secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
            
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = "XYZ-Forge",
                        ValidAudience = "XYZ-Forge-User",
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                        NameClaimType = ClaimTypes.Name,
                    };
                });


            services.AddAuthorization();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "XYZ-Forge-API",
                    Version = "V1",
                    Description = "Our API",
                    Contact = new OpenApiContact
                    {
                        Name = "XYZ-Forge",
                        Email = "admin@xyz-forge.com"
                    }
                });
            });
        }
    }
}
