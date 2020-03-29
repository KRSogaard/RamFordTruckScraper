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
    public class RamListSearchTaskFactory : IScrapingTaskFactory
    {
        public IScrapingTask GetTaskInstance()
        {
            return new RamListSearchTask();
        }

        public string GetTaskName()
        {
            return "SearchPage";
        }
    }

    public class RamListSearchTask : WebFetcher, IScrapingTask
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        public async Task Execute(IScrapingEngine scrapingEngine, string payload)
        {
            RamSearchDetails details = RamSearchDetails.Deserilize(payload);

            WebFetcherResult result = await Download(GenerateUrl(details));

            var a = JArray.Parse(result.HTML);
            var jResult = a[0].Value<JObject>()["data"].Value<JObject>();
            int totalVirs = jResult["totalVehicleCount"].Value<int>();
            var virs = jResult["inventoryVehicles"].Value<JArray>();

            foreach (JObject v in virs.Values<JObject>())
            {
                /*
                dealerCode: "26553"
                locationCode: "000"
                dealerDma: "0807"
                language: null
                modelYearCode: null
                stockNumber: "8114"
                vin: "1C6RRFEG7KN848590"
                description: "Rebel"
                longDescription: null
                yearDescription: "2019"
                divisionDescription: "RAM"
                modelDescription: "1500"
                inventoryType: "C"
                mileage: 18969
                destination: null
                listPrice: 35900
                engineCode: null
                transmissionCode: null
                extColorDescription: "White"
                extColorCode: null
                extImage: "http://fca.images.dmotorworks.com//26553/8114_1.jpg"
                imageSource: "DMI"
                intColorDescription: "Black / Red"
                intFabric: "Black / Red Cloth"
                doors: 4
                numberOfImages: 23
                dmiHotDeal: 0
                driveDescription: "Four-Wheel Drive with Locking Differential"
                bodyDescription: "4 Door Crew Cab Truck"
                engineDescription: "3.6L V6 24V MPFI DOHC"
                transmissionDescription: "8-Speed Automatic"
                wheelBase: "140.5000"
                percentage: 0
                zipDistance: "29.29"
                fiveStar: null
                hasWebTools: "N"
                domainName: "https://www.walnutcreekcjdr.com"
                dealerSitePointer: "4"
                emailAddress: "csavarani@steadauto.com"
                vehicleImages: null
                dealerName: "Walnut Creek Chrysler Jeep Dodge Ram"
                purchaseIncentives: null
                trimDesc: null
                CCode: null
                LLPCode: null
                MSRP: 0
                 */
                string odealerCode = v["dealerCode"].Value<string>();
                string oyearCode = v["yearDescription"].Value<string>();
                string ostockNumber = v["stockNumber"].Value<string>();
                string ozipDistance = v["zipDistance"].Value<string>();
                string omodelDescription = v["modelDescription"].Value<string>();

                // https://www.ramtrucks.com/hostc/cpov/vehicleDetails.do?dealerCode=26553&yearCode=2019&inventoryStockNumber=8114&zipDistance=29.29&fiveStar=&modelDescription=1500
                scrapingEngine.AddTask(new TaskPayload("CarDetails", $"{{" +
                        $"\"dealerCode\": \"{odealerCode}\", " +
                        $"\"yearCode\": \"{oyearCode}\", " +
                        $"\"inventoryStockNumber\": \"{ostockNumber}\", " +
                        $"\"zipDistance\": \"{ozipDistance}\", " +
                        $"\"modelDescription\": \"{omodelDescription}\"" +
                    $"}}")).Wait();
            }

            if (details.Page == 1)
            {
                int pages = (int)Math.Ceiling(totalVirs / 25.0);
                for (int i = 2; i <= pages; i++)
                {
                    await scrapingEngine.AddTask(new TaskPayload("SearchPage", details.Clone(i).Serilize()));
                }
            }
        }

        private string GenerateUrl(RamSearchDetails details)
        {
            return $"https://www.ramtrucks.com/hostc/cpov/vehicleResults.ajax" +
                $"?_dc=1585362825550" +
                $"&maxMileage={details.MaxMiles}" +
                $"&modelDescription={details.Model}" +
                $"&yearStart={details.YearStart}" +
                $"&yearEnd={details.YearEnd}" +
                $"&maxListPrice={details.MaxPrice}" +
                $"&inventoryType=C" +
                $"&zipCode={details.ZipCode}" +
                $"&zipDistance={details.MaxDistance}" +
                $"&sortOrder=DESC" +
                $"&sortByCode=Model" +
                $"&resultsGroupNumber={details.Page}" +
                $"&numResultsPerGroup=25" +
                $"&_rid=757868226427.2859";
        }

        public class RamSearchDetails
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

            public static RamSearchDetails Deserilize(string payload)
            {
                return JsonConvert.DeserializeObject<RamSearchDetails>(payload);
            }

            public RamSearchDetails Clone(int page)
            {
                return new RamSearchDetails()
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

// https://www.ramtrucks.com/hostc/cpov/vehicleResults.ajax?_dc=1585362825550&maxMileage=50000&modelDescription=Ram%201500&yearStart=2015&yearEnd=2020&maxListPrice=60000&inventoryType=C&zipCode=94063&zipDistance=200&sortOrder=DESC&sortByCode=Model&resultsGroupNumber=3&numResultsPerGroup=25&_rid=757868226427.2859
// https://www.ramtrucks.com/hostc/cpov/vehicleResults.ajax?_dc=1585363391845&maxMileage=50000&modelDescription=Ram%201500&yearStart=2015&yearEnd=2020&maxListPrice=60000&inventoryType=C&zipCode=94063&zipDistance=200&sortOrder=DESC&sortByCode=Model&resultsGroupNumber=2&numResultsPerGroup=25&_rid=1361570986909.7234