using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MQTTnet.Client;

namespace Ring.Shared
{
    public interface IMyMqttClient
    {
        IMqttClient sharedMqttClient { get; set; }
        MqttClientOptions sharedOptions { get; set; }
    }
}
