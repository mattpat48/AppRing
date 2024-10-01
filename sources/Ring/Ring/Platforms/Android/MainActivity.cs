using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Work;
using Ring.Platforms.Android;

namespace Ring
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        public static MainActivity? ActivityCurrent { get; set; }
        public MainActivity()
        {
            ActivityCurrent = this;
        }

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            CreateNotificationChannel();
            var intent = new Intent(this, typeof(MqttListenerService));
            StartService(intent);
        }

        private void CreateNotificationChannel()
        {
            #pragma warning disable CA1416
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel("mqtt_channel", "Mqtt Channel", NotificationImportance.Default)
                {
                    Description = "Channel per il servizio MQTT"
                };

                var notificationManager = (NotificationManager)GetSystemService(NotificationService);
                if (notificationManager != null)
                {
                    notificationManager.CreateNotificationChannel(channel);
                }
            }
        }


    }
}
