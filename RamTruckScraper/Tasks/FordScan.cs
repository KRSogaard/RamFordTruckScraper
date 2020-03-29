using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wewelo.Scraper;
using Wewelo.Scraper.Engines;
using Wewelo.Scraper.Web;

namespace RamTruckScraper.Tasks
{
    public class FordScanTaskFactory : IScrapingTaskFactory
    {
        private List<Car> cars;
        public FordScanTaskFactory(List<Car> cars)
        {
            this.cars = cars;
        }

        public IScrapingTask GetTaskInstance()
        {
            return new FordScanTask(cars);
        }

        public string GetTaskName()
        {
            return "FordScan";
        }
    }

    public class FordScanTask : WebFetcher, IScrapingTask
    {
        private static Logger log = LogManager.GetCurrentClassLogger();
        private List<Car> cars;

        public FordScanTask(List<Car> cars)
        {
            this.cars = cars;
        }

        public async Task Execute(IScrapingEngine scrapingEngine, string payload)
        {
            FordSearchDetails details = FordSearchDetails.Deserilize(payload);

            // Needed for cookies
            string cookiesUrl = $"https://cpo.ford.com/Search#PriceHigh={details.MaxPrice}&MileageHigh={details.MaxMiles}&Model={details.Model}&Distance={details.MaxDistance}&ZipCode={details.ZipCode}";
            await Download(cookiesUrl);

            string url = GenerateUrl(details.Page, details);
            var fetch = await Download(url);

            JObject request = JObject.Parse(fetch.HTML);
            foreach (var c in request["Vehicles"].AsJEnumerable())
            {
                Car car = new Car()
                {
                    Make = "Ford",
                    Model = "F-150"
                };
                car.Trim = c["DLRModelPkg"].Value<string>();
                car.Miles = c["mileage"].Value<int>();
                car.Price = c["Price"].Value<int>();

                string vehicleLineDesc = c["VehicleLineDesc"].Value<string>();
                car.FourByFour = vehicleLineDesc.Contains("4x4");
                if (vehicleLineDesc.Contains("supercrew", StringComparison.CurrentCultureIgnoreCase))
                {
                    car.Body = "Super Crew";
                    car.Doors = 4;
                }
                else if (vehicleLineDesc.Contains("supercab", StringComparison.CurrentCultureIgnoreCase))
                {
                    car.Body = "Super Cab";
                    car.Doors = 4;
                } else if (vehicleLineDesc.Contains("Regular Cab", StringComparison.CurrentCultureIgnoreCase))
                {
                    car.Body = "Regular Cab";
                    car.Doors = 2;
                } else {
                    
                    throw new Exception("Unknow body type");
                }

                car.Color = c["color"].Value<string>();
                car.InteriorColor = c["DLRInterior"].Value<string>();
                car.Transmission = c["DLRTrans"].Value<string>();
                car.Engine = c["engine"].Value<string>();

                string bodyStyle = c["DLRBodyStyle"].Value<string>();
                car.ShortBed = bodyStyle.Contains("SHORT BED", StringComparison.CurrentCultureIgnoreCase);

                List<string> features = new List<string>();
                for (int i = 0; i < 100; i++)
                {
                    AddFeatureGroup(c, "fctGroup", i, features);
                    AddFeatureGroup(c, "interiorGroup", i, features);
                    AddFeatureGroup(c, "exteriorGroup", i, features);
                    AddFeatureGroup(c, "securityGroup", i, features);
                    AddFeatureGroup(c, "includedGroup", i, features);
                    AddFeatureGroup(c, "optionalGroup", i, features);
                    AddFeatureGroup(c, "securityGroup", i, features);
                }
                car.Features = features;

                string vin = c["VIN"].Value<string>();
                string pa = c["DealerPA"].Value<string>();
                car.Url = $"https://cpo.ford.com/Detail?" +
                    $"VIN={vin}&PaCode={pa}&ZIPCode={details.ZipCode}";

                if (String.IsNullOrWhiteSpace(car.Trim))
                {
                    if (features.Any(f => f.Contains("XLT", StringComparison.CurrentCultureIgnoreCase)))
                    {
                        car.Trim = "XLT";
                    } else if (features.Any(f => f.Contains("PLATINUM SERIES", StringComparison.CurrentCultureIgnoreCase)))
                    {
                        car.Trim = "PLATINUM";
                    }
                    else if (features.Any(f => f.Contains("XL SERIES", StringComparison.CurrentCultureIgnoreCase)))
                    {
                        car.Trim = "XL";
                    }
                    else if (features.Any(f => f.Contains("KING RANCH", StringComparison.CurrentCultureIgnoreCase)))
                    {
                        car.Trim = "KING RANCH";
                    }
                    else if (features.Any(f => f.Contains("LARIAT", StringComparison.CurrentCultureIgnoreCase)))
                    {
                        car.Trim = "LARIAT";
                    }
                    else {
                        Console.WriteLine("No trim");
                    }

                    //  
                }

                cars.Add(car);
            }


            if (details.Page == 1)
            {
                int pages = request["TotalPages"].Value<int>();
                for (int i = 2; i < pages; i++)
                {
                    await scrapingEngine.AddTask(new TaskPayload("FordScan", details.Clone(i).Serilize()));
                }
            }
        }

        private void AddFeatureGroup(JToken c, string groupName, int i, List<string> features)
        {
            string ftGroupName = groupName + (i == 0 ? "" : "" + i);
            if (c[ftGroupName] != null)
            {
                string value = c[ftGroupName].Value<string>();
                if (!String.IsNullOrWhiteSpace(value))
                {
                    features.Add(value);
                }
            }
        }

        public string GenerateUrl(int page, FordSearchDetails details)
        {
            return $"https://cpo.ford.com/search/ResultsJSON?" +
                $"PriceLow=0&" +
                $"PriceHigh={details.MaxPrice}&" +
                $"MileageLow=0&" +
                $"MileageHigh={details.MaxMiles}&" +
                $"ModelYearLow={details.YearStart}&" +
                $"ModelYearHigh={details.YearEnd}&" +
                $"Make=Ford&" +
                $"Model={details.Model}&" +
                $"Distance={details.MaxDistance}&" +
                $"ResultsPerPage=25&" +
                $"Page={page - 1}&" +
                $"SortBy=distance&" +
                $"SortByDescending=false&" +
                $"DisplayBrand=Ford&" +
                $"Country=US&" +
                $"ZipCode={details.ZipCode}&" +
                $"MileageRangeLow=0&" +
                $"MileageRangeHigh=200000&" +
                $"ModelYearRangeLow=2014&" +
                $"ModelYearRangeHigh=2020&" +
                $"PriceRangeLow=0&" +
                $"PriceRangeHigh=100000";
        }
    
        public class FordSearchDetails
        {
            public int MaxMiles { get; set; }
            public string Model { get; set; }
            public int YearStart { get; set; }
            public int YearEnd { get; set; }
            public int MaxPrice { get; set; }
            public int ZipCode { get; set; }
            public int MaxDistance { get; set; }
            public int Page { get; set; }

            public string Serilize()
            {
                return JsonConvert.SerializeObject(this);
            } 

            public static FordSearchDetails Deserilize(string payload)
            {
                return JsonConvert.DeserializeObject<FordSearchDetails>(payload);
            }

            public FordSearchDetails Clone(int page)
            {
                return new FordSearchDetails()
                {
                    MaxMiles = this.MaxMiles,
                    Model = this.Model,
                    YearStart = this.YearStart,
                    YearEnd = this.YearEnd,
                    MaxPrice = this.MaxPrice,
                    ZipCode = this.ZipCode,
                    MaxDistance = this.MaxDistance,
                    Page = page
                };
            }
        }
    }
}
