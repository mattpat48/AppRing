using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ring.Utils
{
    public class Gate
    {
        public string gateId { get; set; }
        public string name { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string address { get; set; }

        public Gate(string gateId, string name, double latitude, double longitude)
        {
            this.gateId = gateId;
            this.name = name;
            this.latitude = latitude;
            this.longitude = longitude;
            this.address = "";

        }
    }
}
