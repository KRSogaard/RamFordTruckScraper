using System;
using System.Collections.Generic;
using System.Text;

namespace RamTruckScraper
{
    public class Car
    {
        public string Make { get; set; }
        public string Model { get; set; }
        public string Trim { get; set; }
        public int Year { get; set; }
        public int Price { get; set; }
        public int Miles { get; set; }
        public string Body { get; set; }
        public bool ShortBed { get; set; }
        public bool FourByFour { get; set; }
        public string Color { get; set; }
        public string InteriorColor { get; set; }
        public string Transmission { get; set; }
        public string Engine { get; set; }
        public int Doors { get; set; }
        public List<string> Features { get; set; }

        public string Url { get; set; }
    }
}
