using Microsoft.Extensions.Logging;
using Ring.ViewModel;
using CommunityToolkit.Maui;
using SkiaSharp.Views.Maui.Controls.Hosting;
using Plugin.LocalNotification;
using Ring.Shared;

namespace Ring
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder.UseMauiApp<App>().ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            }).UseMauiCommunityToolkit(options =>
            {
                options.SetShouldEnableSnackbarOnWindows(true);
            }).UseSkiaSharp()
            .UseLocalNotification();

            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<MainViewModel>();

            builder.Services.AddTransient<RegisterPage>();
            builder.Services.AddTransient<RegisterViewModel>();

            builder.Services.AddTransient<VerificationPage>();
            builder.Services.AddTransient<VerificationViewModel>();

            builder.Services.AddTransient<MultiplePage>();
            builder.Services.AddTransient<MultipleViewModel>();

            builder.Services.AddTransient<UsersPage>();
            builder.Services.AddTransient<UsersViewModel>();

            builder.Services.AddTransient<AddPage>();
            builder.Services.AddTransient<AddViewModel>();

            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<SettingsViewModel>();

            builder.Services.AddTransient<MapPage>();
            builder.Services.AddTransient<MapViewModel>();

            builder.Services.AddSingleton<IMyHttpClient, MyHttpClient>();
            builder.Services.AddSingleton<IMyMqttClient, MyMqttClient>();

            Environment.SetEnvironmentVariable("MQTT_PORT", "1883");
            //Environment.SetEnvironmentVariable("MQTT_SERVER", "10.20.100.50");
            Environment.SetEnvironmentVariable("MQTT_SERVER", "vainnhomeserver.ddns.net");

            //Environment.SetEnvironmentVariable("API_SERVER", "https://10.20.100.50:7046");
            Environment.SetEnvironmentVariable("API_SERVER", "https://192.168.1.47:7046");
            //Environment.SetEnvironmentVariable("API_SERVER", "https://192.168.0.150:7046");

            return builder.Build();
        }
    }
}