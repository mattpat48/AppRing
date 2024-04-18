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

        public Gate(string gateId, string name)
        {
            this.gateId = gateId;
            this.name = name;
        }
    }
}
