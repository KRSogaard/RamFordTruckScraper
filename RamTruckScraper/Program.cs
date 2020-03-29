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

                IScrapingEngine engine = new LocalScrapingEngine(4, failureHandler, factories, true);

                engine.AddTask(new TaskPayload("SearchPage",
                    new RamListSearchTask.RamSearchDetails()
                    {
                        MaxMiles = 50000,
                        Model = "Ram%201500",
                        YearStart = 2018,
                        YearEnd = 2020,
                        MaxPrice = 50000,
                        ZipCode = 90292,
                        MaxDistance = 200,
                        Page = 1
                    }.Serilize())).Wait();

                engine.AddTask(new TaskPayload("FordScan", new FordScanTask.FordSearchDetails()
                {
                    MaxMiles = 50000,
                    Model = "F-150",
                    YearStart = 2018,
                    YearEnd = 2020,
                    MaxPrice = 50000,
                    ZipCode = 90292,
                    MaxDistance = 250,
                    Page = 1
                }.Serilize()));


                engine.Start().Wait();
            } catch (Exception exp)
            {
                Console.WriteLine(exp.Message);
            }

            Console.WriteLine($"Have {cars.Count} cars");

            List<Car> ramCarsOfIntrest = cars
                .Where(c => c.Make.Contains("Ram", StringComparison.CurrentCultureIgnoreCase))
                .Where(c => c.Trim.Contains("Laramie", StringComparison.CurrentCultureIgnoreCase) || c.Trim.Contains("Limited", StringComparison.CurrentCultureIgnoreCase))
                .Where(c => c.Miles < 40000)
                .Where(c => c.Body.Contains("Crew"))
                .Where(c => c.Engine.Contains("5.7"))
                .OrderBy(c => c.Price)
                .ToList();

            List<Car> fordCarsOfIntrest = cars
                .Where(c => c.Make.Contains("Ford", StringComparison.CurrentCultureIgnoreCase))
                .Where(c => c.Trim.Contains("LARIAT", StringComparison.CurrentCultureIgnoreCase) || c.Trim.Contains("Limited", StringComparison.CurrentCultureIgnoreCase) || c.Trim.Contains("PLATINUM", StringComparison.CurrentCultureIgnoreCase))
                .Where(c => c.Miles < 40000)
                .Where(c => c.Body.Contains("Crew"))
               // .Where(c => c.Engine.Contains("3.5L"))
                .OrderBy(c => c.Price)
                .ToList();

            Console.Write("\n\n\n\n\n\n");
            Console.WriteLine($"RAM: {ramCarsOfIntrest.Count} cars of intrest found:");
            foreach (Car c in ramCarsOfIntrest)
            {
                Console.WriteLine($"{c.Make} {c.Model} {c.Trim} ({c.Miles} Miles, ${c.Price}, {c.Color}): {c.Url}");
            }
            Console.Write("\n\n\n\n\n\n");
            Console.WriteLine($"Ford: {fordCarsOfIntrest.Count} cars of intrest found:");
            foreach (Car c in fordCarsOfIntrest)
            {
                Console.WriteLine($"{c.Make} {c.Model} {c.Trim} ({c.Miles} Miles, ${c.Price}, {c.Color}): {c.Url}");
            }
            Console.Write("\n\n\n\n\n\n");
        }
    }
}
