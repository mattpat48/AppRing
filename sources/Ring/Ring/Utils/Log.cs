using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ring.Utils
{
    public class Log
    {
        public string phoneNumber { get; set; }
        public string deviceId { get; set; }
        public string gateId { get; set; }
        public DateTime date { get; set; }

        public Log(string phoneNumber, string deviceId, string gateId, DateTime date)
        {
            this.phoneNumber = phoneNumber;
            this.deviceId = deviceId;
            this.gateId = gateId;
            this.date = date;
        }
    }
}
