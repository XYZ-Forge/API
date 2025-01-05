using XYZForge.Models;
using XYZForge.Services;

namespace XYZForge.Endpoints
{
    public static class PrinterManagement
    {
        public static void MapPrinterEndpoints(this WebApplication app)
        {
            // POST: Adaugă o imprimantă (dinamic - Resin sau Filament)
            app.MapPost("/add-printer", async (AddPrinterRequest req, MongoDBService mongoDbService) =>
            {
                if (string.IsNullOrEmpty(req.PrinterName) || string.IsNullOrEmpty(req.Type))
                {
                    return Results.BadRequest("PrinterName and Type are required.");
                }

                try
                {
                    // Creează obiectul imprimantă
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

                    // Completează câmpurile specifice pentru Resin sau Filament
                    if (req.Type == "Resin")
                    {
                        printer.ResinTankCapacity = req.ResinTankCapacity;
                        printer.LightSourceType = req.LightSourceType;

                        if (printer.ResinTankCapacity == null || string.IsNullOrEmpty(printer.LightSourceType))
                        {
                            return Results.BadRequest("ResinTankCapacity and LightSourceType are required for Resin printers.");
                        }
                    }
                    else if (req.Type == "Filament")
                    {
                        printer.FilamentDiameter = req.FilamentDiameter;
                        printer.SupportedMaterials = req.SupportedMaterials;

                        if (printer.FilamentDiameter == null || printer.SupportedMaterials == null || !printer.SupportedMaterials.Any())
                        {
                            return Results.BadRequest("FilamentDiameter and SupportedMaterials are required for Filament printers.");
                        }
                    }
                    else
                    {
                        return Results.BadRequest($"Unsupported printer type: {req.Type}");
                    }

                    // Adaugă imprimanta în baza de date
                    await mongoDbService.AddPrinterAsync(printer);
                    return Results.Ok(new { message = "Printer added successfully", printer });
                }
                catch (Exception ex)
                {
                    return Results.Problem($"An error occurred while adding the printer. Error: {ex.Message}");
                }
            });
        }
    }
}
