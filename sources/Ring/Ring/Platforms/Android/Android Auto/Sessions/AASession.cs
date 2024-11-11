using Android.Content;
using AndroidX.Car.App;
using Ring.Platforms.Android.Android_Auto.Screens;
using Ring.Shared;

namespace Ring.Platforms.Android.Android_Auto.Sessions
{
    public class AASession : Session
    {
        private readonly IMyMqttClient _mqttClient;

        public AASession(IMyMqttClient mqttClient)
        {
            _mqttClient = mqttClient;
        }

        public override Screen OnCreateScreen(Intent p0)
        {
            return new AAGatesGrid(CarContext, _mqttClient);
        }
    }
}
