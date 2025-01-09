using XYZForge.Models;
using XYZForge.Services;

namespace XYZForge.Endpoints {
    public static class OrderApiEndpoints
    {
        public static void MapOrderEndpoints(this WebApplication app) {
            var logger =  app.Services.GetRequiredService<ILogger<Program>>();
            var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");

            if (secretKey == null) {
                logger.LogError("Failed to load JWT secret key");
                app.Lifetime.StopApplication();
            }

            
        }
    }
}

// TODO: Refactor this to suit the API pattern
// const double CostPlasticPerGram = 0.5; 
//     const double CostResinPerGram = 1.2;  

//     static void Main(string[] args)
//     {
//         while (true)
//         {
//             Console.WriteLine("1. Vizualizeaza costurile");
//             Console.WriteLine("2. Calculeaza costul unui obiect");
//             Console.WriteLine("3. Comanda un obiect");
//             Console.WriteLine("4. Iesire");
//             Console.Write("Alegeti o optiune: ");

//             string option = Console.ReadLine();

//             switch (option)
//             {
//                 case "1":
//                     ShowCosts();
//                     break;
//                 case "2":
//                     CalculateObjectCost();
//                     break;
//                 case "3":
//                     PlaceOrder();
//                     break;
//                 case "4":
//                     Console.WriteLine("La revedere!");
//                     return;
//                 default:
//                     Console.WriteLine("Optiune invalida.");
//                     break;
//             }
//         }
//     }

//     static void ShowCosts()
//     {
//         Console.WriteLine("\nCosturi pentru printare 3D:");
//         Console.WriteLine($"- Plastic: {CostPlasticPerGram} RON/g");
//         Console.WriteLine($"- Resin: {CostResinPerGram} RON/g");
//     }

//     static void CalculateObjectCost()
//     {
//         Console.Write("\nIntroduceti tipul de printare (plastic/resin): ");
//         string type = Console.ReadLine()?.ToLower();

//         Console.Write("Introduceti greutatea obiectului (g): ");
//         if (!double.TryParse(Console.ReadLine(), out double weight) || weight <= 0)
//         {
//             Console.WriteLine("Greutate invalida. Incercati din nou.");
//             return;
//         }

//         double cost = 0;
//         switch (type)
//         {
//             case "plastic":
//                 cost = weight * CostPlasticPerGram;
//                 break;
//             case "resin":
//                 cost = weight * CostResinPerGram;
//                 break;
//             default:
//                 Console.WriteLine("Tip de printare invalid.");
//                 return;
//         }

//         Console.WriteLine($"Costul estimat pentru un obiect de {weight}g cu tipul {type}: {cost:F2} RON");
//     }

//     static void PlaceOrder()
//     {
//         Console.Write("\nIntroduceti numele obiectului: ");
//         string name = Console.ReadLine();

//         Console.Write("Introduceti greutatea obiectului (g): ");
//         if (!double.TryParse(Console.ReadLine(), out double weight) || weight <= 0)
//         {
//             Console.WriteLine("Greutate invalidă.");
//             return;
//         }

//         Console.Write("Introduceti culoarea obiectului: ");
//         string color = Console.ReadLine();

//         Console.Write("Introduceti adresa de livrare: ");
//         string address = Console.ReadLine();

//         Console.Write("Introduceti tipul de printare (plastic/resin): ");
//         string type = Console.ReadLine()?.ToLower();

//         double cost = 0;
//         switch (type)
//         {
//             case "plastic":
//                 cost = weight * CostPlasticPerGram;
//                 break;
//             case "resin":
//                 cost = weight * CostResinPerGram;
//                 break;
//             default:
//                 Console.WriteLine("Tip de printare invalid. Comanda a fost anulată.");
//                 return;
//         }

//         Console.WriteLine("\nDetalii comanda:");
//         Console.WriteLine($"- Nume obiect: {name}");
//         Console.WriteLine($"- Greutate: {weight}g");
//         Console.WriteLine($"- Culoare: {color}");
//         Console.WriteLine($"- Adresa de livrare: {address}");
//         Console.WriteLine($"- Tip printare: {type}");
//         Console.WriteLine($"- Cost total: {cost:F2} RON");
//         Console.WriteLine("Comanda a fost plasată cu succes!");
//     }
