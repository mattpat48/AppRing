using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ring.Utils
{
    public class Device
    {
        public string deviceId { get; set; }
        public string deviceModel { get; set; }

        public Device(string deviceId, string deviceModel)
        {
            this.deviceId = deviceId;
            this.deviceModel = deviceModel;
        }
    }
}
