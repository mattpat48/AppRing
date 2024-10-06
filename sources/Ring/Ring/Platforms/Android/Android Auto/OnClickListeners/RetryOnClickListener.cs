using AndroidX.Car.App;
using AndroidX.Car.App.Model;
using Ring.Platforms.Android.Android_Auto.Screens;

namespace Ring.Platforms.Android.Android_Auto.OnClickListeners
{
    internal class RetryOnClickListener : Java.Lang.Object, IOnClickListener
    {
        private readonly CarContext _carContext;
        private readonly ScreenManager _screenManager;

        public RetryOnClickListener(CarContext carContext, ScreenManager screenManager)
        {
            _carContext = carContext;
            _screenManager = screenManager;
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
                _screenManager.Push(new AAGatesGrid(_carContext));
                return;
            }
        }
    }
}
