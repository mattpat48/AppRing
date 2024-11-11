using Android.App;
using Android.Locations;
using MQTTnet.Client;
using MQTTnet.Server;
using MQTTnet;
using Newtonsoft.Json;
using System.Globalization;
using Ring.Services;
using Ring.Utils;

namespace Ring.Shared
{
    public static class MqttManager
    {
        public static void StartService()
        {
            MainActivity.ActivityCurrent?.StartMqttService();
        }

        public static void StopService()
        {
            MainActivity.ActivityCurrent?.StopMqttService();
        }

        public static bool Connect(IMqttClient mqttClient, MqttClientOptions options)
        {

            if (mqttClient == null || !mqttClient.IsConnected)
            {
                try
                {
                    if (mqttClient == null)
                    {
                        string? mqttServerAddress = Environment.GetEnvironmentVariable("MQTT_SERVER");
                        string? mqttServerPort = Environment.GetEnvironmentVariable("MQTT_PORT");
                        if (string.IsNullOrEmpty(mqttServerAddress) || string.IsNullOrEmpty(mqttServerPort))
                        {
                            return false;
                        } 

                        string? number = SecureStorage.GetAsync("PhoneNumber").Result;
                    }

                    MqttClientConnectResult connectionResult = mqttClient.ConnectAsync(options).Result;
                    MqttClientConnectResultCode connectionResultCode = connectionResult.ResultCode;

                    if (connectionResultCode == MqttClientConnectResultCode.Success)
                    {
                        MqttClientSubscribeResult subscribeResult =
                                mqttClient.SubscribeAsync(
                                new MqttTopicFilterBuilder()
                                .WithTopic("ringRequest/request")
                                .Build()
                            ).Result;
                        MqttClientSubscribeResultCode subscribeResultCode = subscribeResult.Items.First().ResultCode;
                        if (subscribeResultCode != MqttClientSubscribeResultCode.GrantedQoS0)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }

                }
                catch (Exception ex)
                {
                    return false;
                }
            }
            return mqttClient.IsConnected;
        }

        public static async Task<string> Open(IMqttClient mqttClient, MqttClientOptions options, string selectedId, bool car = false, Android.Locations.Location? carLocation = null)
        {
            bool connected = MqttManager.Connect(mqttClient, options);
            if (!connected)
            {
                return "Not connected to MQTT server";
            }

            try
            {
                double latitude;
                double longitude;
                if (!car)
                {
                    var locationGranted = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                    if (locationGranted != PermissionStatus.Granted)
                    {
                        await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                    }
                    if (await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>() == PermissionStatus.Granted)
                    {
                        var location = await Geolocation.GetLocationAsync();
                        if (location != null)
                        {
                            latitude = location.Latitude;
                            longitude = location.Longitude;
                        }
                        else
                        {
                            return "Location not found";
                        }
                    }
                    else
                    {
                        return "Location permission not granted";
                    }
                }
                else
                {
                    latitude = carLocation.Latitude;
                    longitude = carLocation.Longitude;
                }

                var package = new
                {
                    id = SecureStorage.GetAsync("DeviceId").Result,
                    phoneNumber = SecureStorage.GetAsync("PhoneNumber").Result,
                    datetime = DateTime.Now,
                    lat = latitude + "," + longitude,
                    language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                    gate = selectedId,
                };

                // TEMP UNAVAIBLE
                // prendere la chiave del gate, non del server, quindi criptare con la pubblica del gate
                // e firmare con la privata del telefono
                /*
                string? privateDeviceKey = await SecureStorage.GetAsync("PrivateDeviceKey");
                string? publicServerKey = await SecureStorage.GetAsync("ServerKey");
                if (string.IsNullOrEmpty(privateDeviceKey) || string.IsNullOrEmpty(publicServerKey))
                {
                    handleError("Keys not found.");
                    return;
                }
                string? phoneNumber = await SecureStorage.GetAsync("PhoneNumber");
                string? deviceId = await SecureStorage.GetAsync("DeviceId");
                if (string.IsNullOrEmpty(phoneNumber) || string.IsNullOrEmpty(deviceId))
                {
                    handleError("Phone number or device id not found.");
                    return;
                }


                bool outcome;
                StringContent encrypted;
                (outcome, encrypted) = CryptographyTools.TotalEncrypt(privateDeviceKey, publicServerKey, JsonConvert.SerializeObject(package), phoneNumber, deviceId);

                if (!outcome)
                {
                    handleError("Error while encrypting data.");
                    return;
                }
                */

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic("ringRequest/request")
                    .WithPayload(JsonConvert.SerializeObject(package))
                    .Build();
                try
                {
                    MqttClientPublishResult publishResult = mqttClient.PublishAsync(message).Result;
                    MqttClientPublishReasonCode publishResultCode = publishResult.ReasonCode;

                    if (publishResultCode == MqttClientPublishReasonCode.Success)
                    {
                        return "success";
                    }
                    else
                    {
                        return "Error while sending message";
                    }
                }
                catch (Exception)
                {
                    return "Internal error while sending message";
                }
            }
            catch (Exception)
            {
                return "Error while sending Open message";
            }
        }

        public static async Task<string> LogPostOpen(MainAPI mainAPI, string gateId, string gateName)
        {
            try
            {
                NotificationTools.makeToast("Message sent");
                string generateLogResponse = await mainAPI.GenerateLog(gateId);
                if (generateLogResponse != "success")
                {
                    return generateLogResponse;
                }
                else
                {
                    string? getAllGatesResponse;
                    List<Gate>? gatesReturned;
                    (getAllGatesResponse, gatesReturned) = await mainAPI.GetAllGates();

                    if (getAllGatesResponse != "success")
                    {
                        return getAllGatesResponse;
                    }
                    
                    string gatesPath = Path.Combine(FileSystem.AppDataDirectory, "gates.json");
                    var json = JsonConvert.SerializeObject(gatesReturned);
                    using (var writer = new StreamWriter(gatesPath, false))
                    {
                        await writer.WriteAsync(json);
                    }

                    return "success";
                }
            }
            catch (Exception)
            {
                return "Error while logging open";
            }
        }
    }
}
