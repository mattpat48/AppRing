using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MQTTnet.Client;

namespace Ring.Shared
{
    public class MyMqttClient : IMyMqttClient
    {
        public IMqttClient sharedMqttClient { get; set; }
        public MqttClientOptions sharedOptions { get; set; }
    }
}
