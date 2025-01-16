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
                        SupportedMaterials = req.SupportedMaterials,
                        Status = req.Status
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

                if (string.IsNullOrWhiteSpace(req.id))
                {
                    return Results.BadRequest("Printer ID is required");
                }

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

                if (!string.IsNullOrWhiteSpace(req.status))
                {   
                    if(req.status != "IDLE" && req.status != "BUSY" && req.status != "OFFLINE") {
                        return Results.BadRequest("Invalid status");
                    }
                    
                    printer.Status = req.status;
                }

                await mongoDbService.UpdatePrinterAsync(req.id, printer);
                return Results.Ok($"Printer {req.id} updated successfully by admin");
            });

            app.MapPost("/printer/add-resin", async ([FromBody] UpdatePrinters req, [FromServices] MongoDBService mongoDbService) =>
            {
                if (req.IssuerJWT == null) {
                    return Results.BadRequest("Missing JWT token");
                }

                var principal = JwtHelper.ValidateToken(req.IssuerJWT, secretKey!, logger);

                if (principal == null) {
                    return Results.BadRequest("Invalid or expired token.");
                }

                var roleClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                if (roleClaim != "Admin") {
                    return Results.BadRequest("Only admins can add resin to printers.");
                }

                if (req.id == null) {
                    return Results.BadRequest("Printer ID is required");
                }

                var printer = await mongoDbService.GetPrinterByIdAsync(req.id);
                if (printer == null) {
                    return Results.NotFound("Printer not found");
                }

                if (req.resinTankCapacity.HasValue) {
                    printer.ResinTankCapacity = req.resinTankCapacity.Value;
                } 
                else 
                {
                    return Results.BadRequest("ResinTankCapacity is required");
                }

                await mongoDbService.UpdatePrinterAsync(req.id, printer);
                return Results.Ok($"Resin added to printer {req.id} successfully by admin");
            });  

            app.MapPost("/printer/assign-material", async ([FromBody] AssignMaterialRequest req, [FromServices] MongoDBService mongoDbService, ILogger<Program> logger) =>
            {
                if (string.IsNullOrWhiteSpace(req.IssuerJWT))
                {
                    return Results.BadRequest("Missing JWT token");
                }

                var principal = JwtHelper.ValidateToken(req.IssuerJWT, secretKey, logger);
                if (principal == null)
                {
                    return Results.BadRequest("Invalid or expired token.");
                }

                var roleClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                if (roleClaim != "Admin")
                {
                    return Results.BadRequest("Only admins can assign materials to printers.");
                }

                var printer = await mongoDbService.GetPrinterByIdAsync(req.PrinterId);
                if (printer == null)
                {
                    return Results.NotFound("Printer not found.");
                }

                var material = await mongoDbService.GetMaterialByIdAsync(req.MaterialId);
                if (material == null)
                {
                    return Results.NotFound("Material not found.");
                }

                if (printer.Type == "Filament" && material.Type != "Filament")
                {
                    return Results.BadRequest("Incompatible material type for the printer.");
                }

                if (printer.Type == "Resin" && material.Type != "Resin")
                {
                    return Results.BadRequest("Incompatible material type for the printer.");
                }

                printer.CurrentMaterialId = material.Id;
                await mongoDbService.UpdatePrinterAsync(printer.Id!, printer);

                return Results.Ok(new { message = "Material assigned successfully to printer." });
            });

            app.MapPost("/printer/change-filament", async ([FromBody] ChangeFilament req, [FromServices] MongoDBService mongoDbService, ILogger<Program> logger) =>
            {
                if (string.IsNullOrWhiteSpace(req.IssuerJWT))
                {
                    return Results.BadRequest("Missing JWT token.");
                }

                var principal = JwtHelper.ValidateToken(req.IssuerJWT, secretKey, logger);
                if (principal == null)
                {
                    return Results.BadRequest("Invalid or expired token.");
                }

                var roleClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                if (roleClaim != "Admin")
                {
                    return Results.BadRequest("Only admins can change filament.");
                }

                var printer = await mongoDbService.GetPrinterByIdAsync(req.PrinterId);
                if (printer == null || printer.Type != "Filament")
                {
                    return Results.NotFound("Printer not found or not a filament printer.");
                }

                if (string.IsNullOrEmpty(printer.CurrentMaterialId))
                {
                    return Results.NotFound("No material is currently loaded in the printer.");
                }

                var currentMaterial = await mongoDbService.GetMaterialByIdAsync(printer.CurrentMaterialId);
                if (currentMaterial == null || currentMaterial.Type != "Filament")
                {
                    return Results.NotFound("No valid filament found for the current material in the printer.");
                }

                if (currentMaterial.RemainingQuantity < 100 || currentMaterial.Color != req.RequiredColor)
                {
                    double requiredDiameter = printer.FilamentDiameter ?? 0;
                    if (requiredDiameter <= 0)
                    {
                        return Results.BadRequest("Printer filament diameter is not configured.");
                    }

                    var compatibleFilament = await mongoDbService.FindCompatibleFilamentAsync(req.RequiredColor, requiredDiameter, req.RequiredQuantity);
                    if (compatibleFilament == null)
                    {
                        return Results.NotFound("No compatible filament available in stock.");
                    }

                    if (compatibleFilament.RemainingQuantity < req.RequiredQuantity)
                    {
                        return Results.BadRequest("Insufficient quantity of compatible filament in stock.");
                    }

                    // Deduct quantity from compatible filament
                    compatibleFilament.RemainingQuantity -= req.RequiredQuantity;
                    await mongoDbService.UpdateMaterialAsync(compatibleFilament.Id!, compatibleFilament);

                    // Update the printer's current material
                    printer.CurrentMaterialId = compatibleFilament.Id;
                    await mongoDbService.UpdatePrinterAsync(printer.Id!, printer);

                    return Results.Ok(new
                    {
                        message = "Filament changed successfully.",
                        printerId = printer.Id,
                        newFilament = compatibleFilament.Name,
                        remainingQuantity = compatibleFilament.RemainingQuantity
                    });
                }

                return Results.Ok("No filament change needed.");
            });

        }
    }
}
