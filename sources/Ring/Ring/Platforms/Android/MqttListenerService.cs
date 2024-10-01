using Android.App;
using Android.Content;
using Android.OS;
using MQTTnet;
using MQTTnet.Client;
using System.Text;
using Ring.Utils;
using Newtonsoft.Json;
using System.Text.Json;

namespace Ring.Platforms.Android
{
    [Service]
    public class MqttListenerService : Service
    {
        private MqttFactory? mqttServer;
        private IMqttClient? mqttClient;
        private MqttClientOptions? options;

        private string? _gatesPath;
        private string? storedPhoneNumber;
        private string? storedDeviceId;

        public override async void OnCreate()
        {
            base.OnCreate();

            mqttServer = new MqttFactory();
            mqttClient = mqttServer.CreateMqttClient();

            _gatesPath = Path.Combine(FileSystem.AppDataDirectory, "gates.json");

            // Configurazione del client MQTT
            string? mqttServerAddress = System.Environment.GetEnvironmentVariable("MQTT_SERVER");
            string? mqttServerPort = System.Environment.GetEnvironmentVariable("MQTT_PORT");
            if (string.IsNullOrEmpty(mqttServerAddress) || string.IsNullOrEmpty(mqttServerPort))
            {
                return;
            }

            storedPhoneNumber = await SecureStorage.GetAsync("PhoneNumber");
            storedDeviceId = await SecureStorage.GetAsync("DeviceId");

            options = new MqttClientOptionsBuilder()
                // ATTENZIONE: inserire l'indirizzo IP del server MQTT
                .WithClientId(storedPhoneNumber + " / " + storedDeviceId + " listener")
                .WithTcpServer(mqttServerAddress, int.Parse(mqttServerPort))
                .WithCredentials("user", "ringuser")
                .Build();

            mqttClient.ApplicationMessageReceivedAsync += MessageReceived;

            StartForegroundService();
            ConnectMqttBroker();
        }

        private Task MessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            List<Gate>? gates = new List<Gate>();
            string? phoneNumber;
            string? gateId;
            string? deviceId;
            string? message = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

            string gatesJson = File.ReadAllText(_gatesPath);
            gates = JsonConvert.DeserializeObject<List<Gate>>(gatesJson);

            if (gates == null)
            {
                return Task.CompletedTask;
            }

            using JsonDocument doc = JsonDocument.Parse(message);
            JsonElement root = doc.RootElement;

            if (root.TryGetProperty("phoneNumber", out JsonElement phoneNumberElement) &&
                root.TryGetProperty("gate", out JsonElement gateElement) &&
                root.TryGetProperty("id", out JsonElement deviceIdElement))
            {
                phoneNumber = phoneNumberElement.GetString();
                gateId = gateElement.GetString();
                deviceId = deviceIdElement.GetString();
            }
            else
            {
                return Task.CompletedTask;
            }

            foreach (Gate gate in gates)
            {
                if (gateId == gate.gateId)
                {
                    if (phoneNumber != storedPhoneNumber)
                    {
                        NotificationTools.SendNotification($"{gate.name} has been opened", $"{phoneNumber} has opened the gate");
                    }
                    else if (phoneNumber == storedPhoneNumber && deviceId != storedDeviceId)
                    {
                        NotificationTools.SendNotification($"{gate.name} has been opened", $"You have opened the gate from another device");
                    }
                }
            }    

            return Task.CompletedTask;
        }

        private void StartForegroundService()
        {
            #pragma warning disable CA1416
            var notification = new Notification.Builder(this, "mqtt_channel")
                .SetContentText("You will receive notification when gates are opened")
                .SetSmallIcon(Resource.Drawable.gate_icon)
                .Build();

            StartForeground(1, notification);
        }

        private async void ConnectMqttBroker()
        {
            try
            {
                if (mqttClient == null || options == null)
                {
                    return;
                }
                await mqttClient.ConnectAsync(options);
                await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("ringRequest/request").Build());
            }
            catch (Exception)
            {
            }
        }

        public override IBinder? OnBind(Intent? intent) => null;

        public override void OnDestroy()
        {
            mqttClient.DisconnectAsync();
            base.OnDestroy();
        }
    }
}
