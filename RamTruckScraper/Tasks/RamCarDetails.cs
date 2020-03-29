using CsQuery;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Wewelo.Common;
using Wewelo.Scraper;
using Wewelo.Scraper.Engines;
using Wewelo.Scraper.Web;

namespace RamTruckScraper.Tasks
{
    public class CarDetailsTaskFactory : IScrapingTaskFactory
    {
        private List<Car> cars;

        public CarDetailsTaskFactory(List<Car> cars)
        {
            this.cars = cars;
        }

        public IScrapingTask GetTaskInstance()
        {
            return new CarDetailsTask(cars);
        }

        public string GetTaskName()
        {
            return "CarDetails";
        }
    }

    public class CarDetailsTask : WebFetcher, IScrapingTask
    {
        private static Logger log = LogManager.GetCurrentClassLogger();
        private List<Car> cars;

        public CarDetailsTask(List<Car> cars)
        {
            this.cars = cars;
        }

        public async Task Execute(IScrapingEngine scrapingEngine, string payload)
        {
            Car car = new Car()
            {
                Make = "Ram",
                Model = "1500",
            };

            RamCarDetails details = RamCarDetails.Deserilize(payload);
            string url = GenerateUrl(details);
            WebFetcherResult result = await Download(url);
            CQ dom = result.HTML;

            car.Url = url;
            car.Trim = FindTrim(dom);
            car.Price = FindPrice(dom);
            if (car.Price < 1)
            {
                /// Don't care about no price
                return;
            }

            dom.Select("tr.a-sp3_table_tr").Each(tr =>
            {
                string name = null;
                string text = null;
                try
                {
                    var cq = tr.Cq();
                    name = cq.Find(".a-sp3_table_th").First().Text();
                    text = cq.Find(".a-sp3_table_tprice").First().Text();
                    if (name.Contains("Mileage"))
                    {
                        car.Miles = int.Parse(text.Split('M')[0].Replace(",", "").Trim());
                    }
                    else if (name.Contains("Body Style"))
                    {
                        if (text.Contains("Crew Cab"))
                        {
                            car.Body = "Crew Cab";
                        }
                        else if(text.Contains("2 Door"))
                        {
                            car.Body = "2 Door";
                        } else
                        {
                            throw new Exception("Unknown body in " + text);
                        }

                    }
                    else if (name.Contains("Exterior"))
                    {
                        car.Color = text;
                    }
                    else if (name.Contains("Transmission"))
                    {
                        car.Transmission = text;
                    }
                    else if (name.Contains("Engine"))
                    {
                        car.Engine = text;
                    }
                    else if (name.Contains("Doors"))
                    {
                        car.Doors = int.Parse(text);
                    }
                } catch (Exception exp)
                {
                    log.Error(exp, $"Failed to parse {name}: {text}");
                }
            });

            // Donwload features
            car.Features = new List<string>();
            string featureUrl = GenerateFeatureUrl(details);
            result = await Download(featureUrl);
            var features = JArray.Parse(result.HTML).First["data"]["features"].AsJEnumerable();
            foreach(var feature in features)
            {
                var value = feature["featureData"].Value<String>();
                car.Features.Add(value);
            }

            // FourByFour?
            // InteriorColor
            // ShortBed
 
            cars.Add(car);
        }

        private string FindTrim(CQ dom)
        {
            var title = dom.Select(".a-sp3_updiv_right_boldtext").First().Text();
            if (title.Contains("Limited", StringComparison.CurrentCultureIgnoreCase))
            {
                return "Limited";
            }
            else if (title.Contains("Laramie", StringComparison.CurrentCultureIgnoreCase))
            {
                return "Laramie";
            }
            else if (title.Contains("Rebel", StringComparison.CurrentCultureIgnoreCase))
            {
                return "Rebel";
            }
            else if (title.Contains("Tradesman", StringComparison.CurrentCultureIgnoreCase))
            {
                return "Tradesman";
            }
            else if (title.Contains("Big Horn", StringComparison.CurrentCultureIgnoreCase))
            {
                return "Big Horn";
            }
            else if (title.Contains("ST", StringComparison.CurrentCultureIgnoreCase))
            {
                return "ST";
            }
            else if (title.Contains("SLT", StringComparison.CurrentCultureIgnoreCase))
            {
                return "SLT";
            }

            throw new Exception("Unknown trim in " + title);
        }

        private int FindPrice(CQ dom)
        {
            string priceField = null;
            try
            {
                priceField = dom.Select(".a-sp3_right_textline1").First().Text();
                return int.Parse(priceField.Split("$")[1].Replace(",", "").Split(".")[0]);
            }
            catch (Exception exp)
            {
                log.Error(exp, $"Faild to find price in \"{priceField}\"");
                return -1;
            }
        }

        private string GenerateUrl(RamCarDetails details)
        {
            return $"https://www.ramtrucks.com/hostc/cpov/vehicleDetails.do?" +
                $"dealerCode={details.DealerCode}&" +
                $"yearCode={details.YearCode}&" +
                $"inventoryStockNumber={details.InventoryStockNumber}&" +
                $"zipDistance={details.ZipDistance}&" +
                $"fiveStar=&" +
                $"modelDescription={details.ModelDescription}";
        }

        private string GenerateFeatureUrl(RamCarDetails details)
        {
            return $"https://www.ramtrucks.com/hostc/cpov/vehicleFeatures.ajax?" +
                $"_dc=1585374316019&" +
                $"dealerCode={details.DealerCode}&" +
                $"inventoryStockNumber={details.InventoryStockNumber}&" +
                $"stockNumber={details.InventoryStockNumber}&" +
                $"_rid=750390036582.1665";
        }

        public class RamCarDetails
        {
            public string DealerCode { get; set; }
            public string YearCode { get; set; }
            public string InventoryStockNumber { get; set; }
            public string ZipDistance { get; set; }
            public string ModelDescription { get; set; }

            public string Serilize()
            {
                return JsonConvert.SerializeObject(this);
            }

            public static RamCarDetails Deserilize(string payload)
            {
                return JsonConvert.DeserializeObject<RamCarDetails>(payload);
            }
        }
    }
}
