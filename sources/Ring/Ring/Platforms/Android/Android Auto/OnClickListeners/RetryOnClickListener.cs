using AndroidX.Car.App;
using AndroidX.Car.App.Model;
using Ring.Platforms.Android.Android_Auto.Screens;
using Ring.Shared;

namespace Ring.Platforms.Android.Android_Auto.OnClickListeners
{
    internal class RetryOnClickListener : Java.Lang.Object, IOnClickListener
    {
        private readonly CarContext _carContext;
        private readonly ScreenManager _screenManager;
        private readonly IMyMqttClient _mqttClient;

        public RetryOnClickListener(CarContext carContext, ScreenManager screenManager, IMyMqttClient myMqttClient)
        {
            _carContext = carContext;
            _screenManager = screenManager;
            _mqttClient = myMqttClient;
        }
        public void OnClick()
        {
            string? phoneNumber = SecureStorage.GetAsync("PhoneNumber").Result;
            if (string.IsNullOrEmpty(phoneNumber))
            {
                CarToast.MakeText(_carContext, "Error: no number was found", CarToast.LengthLong).Show();
            }
            else
            {
                _screenManager.PopToRoot();
                _screenManager.Push(new AAGatesGrid(_carContext, _mqttClient));
                return;
            }
        }
    }
}
