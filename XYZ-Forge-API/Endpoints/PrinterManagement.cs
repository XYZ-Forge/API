using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using XYZForge.Models;
using XYZForge.Services;
using MongoDB.Driver;

namespace XYZForge.Endpoints
{
    public static class PrinterManagement
    {
        public static void MapPrinterEndpoints(this WebApplication app)
        {

            app.MapPost("/printers", async (GetPrinters req,MongoDBService mongoDbService)=>
            {
                var printers=await mongoDbService.GetPrintersAsync();

                    if(!printers.Any())
                    {
                        return Results.NotFound("No printers in the database");
                    }

                if(req.type == "Resin")
                {
                    var resinPrinters = printers.Where(p => p.Type == "Resin");
                    return Results.Ok(new { resinPrinters= resinPrinters});
                }
                
                if(req.type == "Filament")
                {
                    var filamentPrinters = printers.Where(p => p.Type == "Filament");
                    return Results.Ok(new { filamentPrinters= filamentPrinters});
                }
                var allResinPrinters = printers.Where(p => p.Type == "Resin"); 
                var allFilamentPrinters = printers.Where(p => p.Type == "Filament");
                return Results.Ok(new {allResinPrinters, allFilamentPrinters});
            });

            

            
            app.MapPost("/add-printer", async (AddPrinterRequest req, MongoDBService mongoDbService) =>
            {
                if (string.IsNullOrEmpty(req.PrinterName) || string.IsNullOrEmpty(req.Type))
                {
                    return Results.BadRequest("PrinterName and Type are required");
                }

                try
                {
                    
                    if(req.SupportedMaterials == null || !req.SupportedMaterials.Any()) {
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

                        if (printer.ResinTankCapacity == null || string.IsNullOrEmpty(printer.LightSourceType))
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
        
            app.MapPost("/printer/search",async ([FromBody] SearchPrinters req,[FromServices] MongoDBService mongoDbService)=>
            {
                var res = await mongoDbService.SearchPrintersAsync(req.id,req.name,req.resolution,req.hasWiFi,req.hasTouchScreen);
                
                return res.Any() ? Results.Ok(res) : Results.NotFound("No printers found");
            });

            app.MapDelete("/printer/delete", async ([FromBody] DeletePrinter req, [FromServices] MongoDBService mongoDbService) =>
            {
                var result = await mongoDbService.DeletePrinterAsync(req.id);
                return result.DeletedCount > 0 ? Results.Ok($"Printer {req.id} deleted successfully by admin") : Results.NotFound("Printer not found");
            });

            app.MapPost("/printer/update",async ([FromBody]UpdatePrinters req,[FromServices]MongoDBService mongoDbService)=>
            {
                var printer = await mongoDbService.GetPrinterByIdAsync(req.id!);
                if(printer == null)
                {
                    return Results.NotFound("Printer not found");
                }

                if (!string.IsNullOrEmpty(req.printerName))
                {
                    printer.PrinterName = req.printerName;
                }

                if (!string.IsNullOrEmpty(req.resolution))
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

                if (!string.IsNullOrEmpty(req.maxDimensions))
                {
                    printer.MaxDimensions = req.maxDimensions;
                }

                if (req.price.HasValue)
                {
                    printer.Price = req.price.Value;
                }

                if (!string.IsNullOrEmpty(req.type))
                {
                    printer.Type = req.type;
                }

                if (req.resinTankCapacity.HasValue)
                {
                    printer.ResinTankCapacity = req.resinTankCapacity;
                }

                if (!string.IsNullOrEmpty(req.lightSourceType))
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
