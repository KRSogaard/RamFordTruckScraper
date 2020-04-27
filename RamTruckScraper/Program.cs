using NLog;
using RamTruckScraper.Tasks;
using System;
using System.Collections.Generic;
using Wewelo.Scraper;
using Wewelo.Scraper.Engines;
using System.Linq;

namespace RamTruckScraper
{
    class Program
    {
        private static Logger log = LogManager.GetCurrentClassLogger();
        private static int MIN_YEAR = 2018;
        private static int MAX_YEAR = 2020;
        private static int ZIP_CODE = 90064;
        private static int MAX_DISTANCE = 150;
        private static int MAX_PRICE = 99999;
        private static int MAX_MILES = 35000;

        static void Main(string[] args)
        {
            List<Car> cars = new List<Car>();

            try
            {
                log.Info("Starting");

                Action<TaskPayload, Exception> failureHandler = (tp, exp) =>
                {
                    log.Error(exp, $"Failed to execute task: {tp}");
                };

                List<IScrapingTaskFactory> factories = new List<IScrapingTaskFactory>();
                factories.Add(new RamListSearchTaskFactory());
                factories.Add(new CarDetailsTaskFactory(cars));
                factories.Add(new FordScanTaskFactory(cars));

                IScrapingEngine engine = new LocalScrapingEngine(1, failureHandler, factories, true);

                engine.AddTask(new TaskPayload("SearchPage",
                    new RamListSearchTask.RamSearchDetails()
                    {
                        MaxMiles = MAX_MILES,
                        Model = "Ram%201500",
                        YearStart = MIN_YEAR,
                        YearEnd = MAX_YEAR,
                        MaxPrice = MAX_PRICE,
                        ZipCode = ZIP_CODE,
                        MaxDistance = MAX_DISTANCE,
                        Page = 1
                    }.Serilize())).Wait();

                engine.AddTask(new TaskPayload("FordScan", new FordScanTask.FordSearchDetails()
                {
                    MaxMiles = MAX_MILES,
                    Model = "F-150",
                    YearStart = MIN_YEAR,
                    YearEnd = MAX_YEAR,
                    MaxPrice = MAX_PRICE,
                    ZipCode = ZIP_CODE,
                    MaxDistance = MAX_DISTANCE,
                    Page = 1
                }.Serilize()));


                engine.Start().Wait();
            } catch (Exception exp)
            {
                Console.WriteLine(exp.Message);
            }

            Console.WriteLine($"Have {cars.Count} cars");

            var unwatedRamModels = new String[] { "Sport", "SLT", "ST", "Rebel", "Tradesman", "Big Horn" };
            List<Car> ramCarsOfIntrest = cars
                .Where(c => c.Make.Contains("Ram", StringComparison.CurrentCultureIgnoreCase))
                .Where(c => !unwatedRamModels.Any(t => t.Equals(c.Trim, StringComparison.CurrentCultureIgnoreCase)))
                .Where(c => c.Miles < MAX_MILES)
                .Where(c => c.Price <= MAX_PRICE)
                .Where(c => c.Body.Contains("Crew"))
                //.Where(c => c.Engine.Contains("5.7"))
                .OrderBy(c => c.Price)
                .ToList();


            var unwatedFordModels = new String[] { "XL", "XLT", "XL/T", "XL/T/RB/BC", "XL/T/BC", "KING RANCH" };
            var wantedPackages = new String[] { "701A",  "502A", "900A", "801A", "802A" };
            List<Car> fordCarsOfIntrest = cars
                .Where(c => c.Make.Contains("Ford", StringComparison.CurrentCultureIgnoreCase))
                //.Where(c => !unwatedFordModels.Any(t => t.Equals(c.Trim, StringComparison.CurrentCultureIgnoreCase)))
                .Where(c => c.Miles < MAX_MILES)
                .Where(c => c.Price <= MAX_PRICE)
                .Where(c => c.Body.Contains("Crew"))
                .Where(c => c.Features.Any(t => wantedPackages.Any(p => t.Contains(p, StringComparison.CurrentCultureIgnoreCase))))
                //.Where(c => c.Engine.Contains("Eco"))
                .OrderBy(c => c.Price)
                .ToList();

            Console.Write("\n\n\n\n\n\n");
            Console.WriteLine($"RAM: {ramCarsOfIntrest.Count} cars of interest found:");
            foreach (Car c in ramCarsOfIntrest)
            {
                Console.WriteLine($"{c.Year} {c.Make} {c.Model} {c.Trim} ({c.Miles} Miles, ${c.Price}, {c.Color}): {c.Url}");
            }
            Console.Write("\n\n\n\n\n\n");
            Console.WriteLine($"Ford: {fordCarsOfIntrest.Count} cars of interest found:");
            foreach (Car c in fordCarsOfIntrest)
            {
                Console.WriteLine($"{c.Year} {c.Model} {c.Trim} ({c.Miles} Miles, ${c.Price}, {c.Color}): {c.Url}");
            }
            Console.Write("\n\n\n\n\n\n");
        }
    }
}
