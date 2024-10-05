using Android.App;
using AndroidX.Car.App;
using AndroidX.Car.App.Validation;
using Ring.Platforms.Android.Android_Auto.Sessions;

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
            return new AASession();
        }
    }
}
