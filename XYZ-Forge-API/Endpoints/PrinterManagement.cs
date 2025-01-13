using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using XYZForge.Models;
using XYZForge.Services;

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
                    
                    var printer = new Printer
                    {
                        PrinterName = req.PrinterName,
                        Resolution = req.Resolution,
                        HasWiFi = req.HasWiFi,
                        HasTouchScreen = req.HasTouchScreen,
                        MaxDimensions = req.MaxDimensions,
                        Price = req.Price,
                        Type = req.Type
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
                        printer.SupportedMaterials = req.SupportedMaterials;

                        if (printer.FilamentDiameter == null || printer.SupportedMaterials == null || !printer.SupportedMaterials.Any())
                        {
                            return Results.BadRequest("FilamentDiameter and SupportedMaterials are required for Filament printers");
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
            /////////////////TEST/////////////////////
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

            app.MapPost("/printer/update",async (UpdatePrinters req,MongoDBService mongoDbService)=>
            {
                var printer = await mongoDbService.GetPrinterByIdAsync(req.id);
                if(printer == null)
                {
                    return Results.NotFound("Printer not found");
                }
                return Results.Ok(printer);
            });
        }
    }
}
