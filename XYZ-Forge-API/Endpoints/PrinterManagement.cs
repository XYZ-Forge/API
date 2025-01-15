using Microsoft.AspNetCore.Mvc;
using XYZForge.Models;
using XYZForge.Services;
using XYZForge.Helpers;
using MongoDB.Driver;
using System.Security.Claims;

namespace XYZForge.Endpoints
{
    public static class PrinterManagement
    {
        public static void MapPrinterEndpoints(this WebApplication app)
        {
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");

            if (string.IsNullOrWhiteSpace(secretKey))
            {
                logger.LogError("JWT_SECRET_KEY is missing from the environment variables");
                throw new InvalidOperationException("JWT secret key is not configured.");
            }

            app.MapPost("/printers", async ([FromBody] GetPrinters req, [FromServices] MongoDBService mongoDbService) =>
            {
                if(req.IssuerJWT == null) {
                    return Results.BadRequest("Missing JWT token");
                }

                var principal = JwtHelper.ValidateToken(req.IssuerJWT, secretKey, logger);
                if (principal == null)
                    return Results.BadRequest("Invalid or expired token.");

                var roleClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                if (roleClaim != "Admin")
                    return Results.BadRequest("Only admins can access this route.");

                var printers = await mongoDbService.GetPrintersAsync();

                if (!printers.Any())
                {
                    return Results.NotFound("No printers in the database");
                }

                if (req.type == "Resin")
                {
                    var resinPrinters = printers.Where(p => p.Type == "Resin");
                    return Results.Ok(new { resinPrinters });
                }

                if (req.type == "Filament")
                {
                    var filamentPrinters = printers.Where(p => p.Type == "Filament");
                    return Results.Ok(new { filamentPrinters });
                }

                var allResinPrinters = printers.Where(p => p.Type == "Resin");
                var allFilamentPrinters = printers.Where(p => p.Type == "Filament");
                return Results.Ok(new { allResinPrinters, allFilamentPrinters });
            });

            app.MapPost("/add-printer", async ([FromBody] AddPrinterRequest req, [FromServices] MongoDBService mongoDbService) =>
            {
                var principal = JwtHelper.ValidateToken(req.IssuerJWT, secretKey, logger);
                if (principal == null)
                    return Results.BadRequest("Invalid or expired token.");

                var roleClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                if (roleClaim != "Admin")
                    return Results.BadRequest("Only admins can add printers.");

                if (string.IsNullOrWhiteSpace(req.PrinterName) || string.IsNullOrWhiteSpace(req.Type))
                {
                    return Results.BadRequest("PrinterName and Type are required");
                }

                try
                {
                    if (req.SupportedMaterials == null || !req.SupportedMaterials.Any())
                    {
                        return Results.BadRequest("Supported Materials are missing");
                    }

                    var printer = new Printer
                    {
                        PrinterName = req.PrinterName,
                        Resolution = req.Resolution,
                        HasWiFi = req.HasWiFi,
                        HasTouchScreen = req.HasTouchScreen,
                        MaxDimensions = req.MaxDimensions,
                        Price = req.Price,
                        Type = req.Type,
                        SupportedMaterials = req.SupportedMaterials
                    };

                    if (req.Type == "Resin")
                    {
                        printer.ResinTankCapacity = req.ResinTankCapacity;
                        printer.LightSourceType = req.LightSourceType;

                        if (printer.ResinTankCapacity == null || string.IsNullOrWhiteSpace(printer.LightSourceType))
                        {
                            return Results.BadRequest("ResinTankCapacity and LightSourceType are required for Resin printers");
                        }
                    }
                    else if (req.Type == "Filament")
                    {
                        printer.FilamentDiameter = req.FilamentDiameter;

                        if (printer.FilamentDiameter == null)
                        {
                            return Results.BadRequest("FilamentDiameter is required for Filament printers");
                        }
                    }
                    else
                    {
                        return Results.BadRequest($"Unsupported printer type: {req.Type}");
                    }

                    await mongoDbService.AddPrinterAsync(printer);
                    return Results.Ok(new { message = "Printer added successfully", printer });
                }
                catch (Exception ex)
                {
                    return Results.Problem($"An error occurred while adding the printer. Error: {ex.Message}");
                }
            });

            app.MapPost("/printer/search", async ([FromBody] SearchPrinters req, [FromServices] MongoDBService mongoDbService) =>
            {
                if(req.IssuerJWT == null) {
                    return Results.BadRequest("Missing JWT token");
                }

                var principal = JwtHelper.ValidateToken(req.IssuerJWT, secretKey, logger);
                if (principal == null)
                    return Results.BadRequest("Invalid or expired token.");

                var roleClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                if (roleClaim != "Admin")
                    return Results.BadRequest("Only admins can search for printers.");

                var res = await mongoDbService.SearchPrintersAsync(req.id, req.name, req.resolution, req.hasWiFi, req.hasTouchScreen);
                return res.Any() ? Results.Ok(res) : Results.NotFound("No printers found");
            });

            app.MapDelete("/printer/delete", async ([FromBody] DeletePrinter req, [FromServices] MongoDBService mongoDbService) =>
            {
                if(req.IssuerJWT == null) {
                    return Results.BadRequest("Missing JWT token");
                }

                var principal = JwtHelper.ValidateToken(req.IssuerJWT, secretKey, logger);
                if (principal == null)
                    return Results.BadRequest("Invalid or expired token.");

                var roleClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                if (roleClaim != "Admin")
                    return Results.BadRequest("Only admins can delete printers.");

                var result = await mongoDbService.DeletePrinterAsync(req.id);
                return result.DeletedCount > 0 ? Results.Ok($"Printer {req.id} deleted successfully by admin") : Results.NotFound("Printer not found");
            });

            app.MapPost("/printer/update", async ([FromBody] UpdatePrinters req, [FromServices] MongoDBService mongoDbService) =>
            {
                if(req.IssuerJWT == null) {
                    return Results.BadRequest("Missing JWT token");
                }

                var principal = JwtHelper.ValidateToken(req.IssuerJWT, secretKey, logger);
                if (principal == null)
                    return Results.BadRequest("Invalid or expired token.");

                var roleClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                if (roleClaim != "Admin")
                    return Results.BadRequest("Only admins can update printers.");

                var printer = await mongoDbService.GetPrinterByIdAsync(req.id!);
                if (printer == null)
                {
                    return Results.NotFound("Printer not found");
                }

                if(req.id == null) {
                    return Results.BadRequest("Printer ID is required");
                }

                if (!string.IsNullOrWhiteSpace(req.printerName))
                {
                    printer.PrinterName = req.printerName;
                }

                if (!string.IsNullOrWhiteSpace(req.resolution))
                {
                    printer.Resolution = req.resolution;
                }

                if (req.hasWiFi.HasValue)
                {
                    printer.HasWiFi = req.hasWiFi.Value;
                }

                if (req.hasTouchScreen.HasValue)
                {
                    printer.HasTouchScreen = req.hasTouchScreen.Value;
                }

                if (!string.IsNullOrWhiteSpace(req.maxDimensions))
                {
                    printer.MaxDimensions = req.maxDimensions;
                }

                if (req.price.HasValue)
                {
                    printer.Price = req.price.Value;
                }

                if (!string.IsNullOrWhiteSpace(req.type))
                {
                    printer.Type = req.type;
                }

                if (req.resinTankCapacity.HasValue)
                {
                    printer.ResinTankCapacity = req.resinTankCapacity;
                }

                if (!string.IsNullOrWhiteSpace(req.lightSourceType))
                {
                    printer.LightSourceType = req.lightSourceType;
                }

                if (req.filamentDiameter.HasValue)
                {
                    printer.FilamentDiameter = req.filamentDiameter;
                }

                if (req.supportedMaterials != null && req.supportedMaterials.Count > 0)
                {
                    printer.SupportedMaterials = req.supportedMaterials;
                }

                await mongoDbService.UpdatePrinterAsync(req.id, printer);
                return Results.Ok($"Printer {req.id} updated successfully by admin");
            });
        }
    }
}
