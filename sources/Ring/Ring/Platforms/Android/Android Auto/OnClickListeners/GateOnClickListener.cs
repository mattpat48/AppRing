using AndroidX.Car.App;
using AndroidX.Car.App.Model;
using MQTTnet.Client;
using MQTTnet.Server;
using MQTTnet;
using Ring.Utils;
using System.Globalization;
using Newtonsoft.Json;
using Ring.Services;
using Android.Locations;
using Android.Content;

using Application = Android.App.Application;
using Android.Gms.Location;
using Location = Android.Locations.Location;

namespace Ring.Platforms.Android.Android_Auto.OnClickListeners
{
    public class GateOnClickListener : Java.Lang.Object, IOnClickListener
    {
        private readonly CarContext _carContext;
        private readonly ScreenManager _screenManager;
        private readonly Gate _gate;

        private MqttFactory? mqttServer;
        private IMqttClient? mqttClient;
        private MqttClientOptions? options;

        LocationManager locationManager;
        string locationProvider;

        public GateOnClickListener(CarContext carContext, ScreenManager screenManager, Gate gate)
        {
            _carContext = carContext;
            _screenManager = screenManager;
            _gate = gate;

            mqttServer = null;
            mqttClient = null;
            options = null;

            locationManager = (LocationManager)Application.Context.GetSystemService(Context.LocationService);

            var criteria = new Criteria { Accuracy = Accuracy.Fine };
            locationProvider = locationManager.GetBestProvider(criteria, true);
        }

        public void OnClick()
        {
            Open();
        }

        private void Open()
        {
            string? phoneNumber = SecureStorage.GetAsync("PhoneNumber").Result;
            if (string.IsNullOrEmpty(phoneNumber))
            {
                try
                {
                    CarToast.MakeText(_carContext, "Error: no number was found", CarToast.LengthLong).Show();
                    _screenManager.PopToRoot();
                    return;
                }
                catch (Exception e)
                {
                    CarToast.MakeText(_carContext, "Error: " + e.Message, CarToast.LengthLong).Show();
                    return;
                }
            }
            else
            {
                string openResult = OpenGate(_gate.gateId).Result;
                if (openResult == "success")
                {
                    CarToast.MakeText(_carContext, "Message Sent", CarToast.LengthShort).Show();
                    return;
                }
                else
                {
                    CarToast.MakeText(_carContext, "Error: " + openResult, CarToast.LengthLong).Show();
                    return;
                }
            }
        }

        public bool Connect()
        {

            if (mqttClient == null || !mqttClient.IsConnected)
            {
                try
                {
                    if (mqttClient == null)
                    {
                        mqttServer = new MqttFactory();
                        mqttClient = mqttServer.CreateMqttClient();

                        string? mqttServerAddress = Environment.GetEnvironmentVariable("MQTT_SERVER");
                        string? mqttServerPort = Environment.GetEnvironmentVariable("MQTT_PORT");
                        if (string.IsNullOrEmpty(mqttServerAddress) || string.IsNullOrEmpty(mqttServerPort))
                        {
                            return false;
                        }

                        string? number = SecureStorage.GetAsync("PhoneNumber").Result;

                        options = new MqttClientOptionsBuilder()
                            // ATTENZIONE: inserire l'indirizzo IP del server MQTT
                            .WithClientId(number + " sender car")
                            .WithTcpServer(mqttServerAddress, int.Parse(mqttServerPort))
                            .WithCredentials("user", "ringuser")
                            .Build();
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

        public async Task<string> OpenGate(string selectedId)
        {
            bool connected = Connect();
            if (!connected)
            {
                return "Not connected to MQTT server";
            }

            try
            {
                Location? location = null;
                var locationGranted = Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>().Result;
                if (locationGranted != PermissionStatus.Granted)
                {
                    locationGranted = Permissions.RequestAsync<Permissions.LocationWhenInUse>().Result;
                }
                if (locationGranted == PermissionStatus.Granted)
                {
                    location = GetCurrentLocation();
                }
                else
                {
                    return "Location permission not granted";
                }

                if (location != null)
                {

                    var package = new
                    {
                        id = SecureStorage.GetAsync("DeviceId").Result,
                        phoneNumber = SecureStorage.GetAsync("PhoneNumber").Result,
                        datetime = DateTime.Now,
                        lat = location.Latitude + "," + location.Longitude,
                        language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                        gate = selectedId,
                    };

                    // TEMP UNAVAIBLE
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
                            string _logsToUpdatePath = Path.Combine(FileSystem.AppDataDirectory, "logsToUpdate.json");
                            List<Log> logsToUpdate = new List<Log>();
                            if (File.Exists(_logsToUpdatePath))
                            {
                                logsToUpdate = JsonConvert.DeserializeObject<List<Log>>(File.ReadAllText(_logsToUpdatePath));
                            }
                            logsToUpdate.Add(new Log(
                                SecureStorage.GetAsync("PhoneNumber").Result,
                                SecureStorage.GetAsync("DeviceId").Result,
                                selectedId,
                                DateTime.Now)
                                );

                            var toAdd = JsonConvert.SerializeObject(logsToUpdate);
                            using (var writer = new StreamWriter(_logsToUpdatePath, false))
                            {
                                await writer.WriteAsync(toAdd);
                            }

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
                else
                {
                    return "Location not found";
                }
            }
            catch (Exception)
            {
                return "Error while sending Open message";
            }
        }

        public Location GetCurrentLocation()
        {
            var location = locationManager.GetLastKnownLocation(locationProvider);

            // Se non troviamo una posizione, possiamo richiedere aggiornamenti
            if (location == null)
            {
                var tcs = new TaskCompletionSource<Location>();
                var listener = new LocationListener(locationManager, tcs);
                locationManager.RequestSingleUpdate(locationProvider, listener, null);

                location = tcs.Task.Result;
            }

            return location;
        }

    }
} 
