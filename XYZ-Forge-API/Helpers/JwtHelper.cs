using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace XYZForge.Helpers
{
    public static class JwtHelper
    {
        public static string GenerateJwtToken(string username, string role, int tokenVersion)
        {
            string? secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role),
                new Claim("TokenVersion", tokenVersion.ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: "XYZ-Forge",
                audience: "XYZ-Forge-User",
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials,
                claims: claims
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        public static ClaimsPrincipal? ValidateToken(string token, string secretKey, ILogger logger)
        {
            if (string.IsNullOrEmpty(token))
            {
                logger.LogWarning("Token is null or empty.");
                return null;
            }

            try
            {
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var validatorParams = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = "XYZ-Forge",
                    ValidAudience = "XYZ-Forge-User",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!))
                };

                return handler.ValidateToken(token, validatorParams, out _);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "JWT validation failed");
                return null;
            }
        }

    }
}
