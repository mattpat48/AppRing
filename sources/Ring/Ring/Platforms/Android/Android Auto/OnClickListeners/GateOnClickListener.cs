using AndroidX.Car.App;
using AndroidX.Car.App.Model;
using MQTTnet.Client;
using MQTTnet.Server;
using MQTTnet;
using Ring.Utils;
using System.Globalization;
using Newtonsoft.Json;
using Android.Locations;
using Android.Content;

using Application = Android.App.Application;
using Location = Android.Locations.Location;
using Ring.Shared;

namespace Ring.Platforms.Android.Android_Auto.OnClickListeners
{
    public class GateOnClickListener : Java.Lang.Object, IOnClickListener
    {
        private readonly CarContext _carContext;
        private readonly ScreenManager _screenManager;
        private readonly Gate _gate;

        private IMqttClient? mqttClient;
        private MqttClientOptions? options;

        LocationManager locationManager;
        string locationProvider;

        public GateOnClickListener(CarContext carContext, ScreenManager screenManager, Gate gate, IMyMqttClient myMqttClient)
        {
            _carContext = carContext;
            _screenManager = screenManager;
            _gate = gate;

            mqttClient = myMqttClient.sharedMqttClient;
            options = myMqttClient.sharedOptions;

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
                string openResult = MqttManager.Open(mqttClient, options, _gate.gateId, true, GetCurrentLocation()).Result;
                if (openResult == "success")
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
                        _gate.gateId,
                        DateTime.Now)
                        );

                    var toAdd = JsonConvert.SerializeObject(logsToUpdate);
                    using (var writer = new StreamWriter(_logsToUpdatePath, false))
                    {
                        writer.WriteAsync(toAdd);
                    }

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
