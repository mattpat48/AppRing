using Android.App;
using AndroidX.Car.App;
using AndroidX.Car.App.Validation;
using MQTTnet;
using MQTTnet.Client;
using Ring.Platforms.Android.Android_Auto.Sessions;
using Ring.Shared;

namespace Ring.Platforms.Android.Android_Auto.Services
{
    [Service(Exported = true)]
    [IntentFilter(new string[] { "androidx.car.app.CarAppService" }, Categories = new[] { "androidx.car.app.category.POI" })]
    public class AACarService : CarAppService
    {
        public override HostValidator CreateHostValidator()
        {
            return HostValidator.AllowAllHostsValidator;
        }

        public override Session OnCreateSession()
        {
            var sharedMqttClient = new MyMqttClient();
            string? storedPhoneNumber = SecureStorage.GetAsync("PhoneNumber").Result;
            string? storedDeviceId = SecureStorage.GetAsync("DeviceId").Result;
            string? mqttServerAddress = System.Environment.GetEnvironmentVariable("MQTT_SERVER");
            string? mqttServerPort = System.Environment.GetEnvironmentVariable("MQTT_PORT");

            sharedMqttClient.sharedMqttClient = new MqttFactory().CreateMqttClient();
            sharedMqttClient.sharedOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(mqttServerAddress, int.Parse(mqttServerPort))
                .WithClientId(storedPhoneNumber + "_" + storedDeviceId + "_automotive")
                .WithCredentials("user", "ringuser")
                .Build();
            return new AASession(sharedMqttClient);
        }
    }
}
