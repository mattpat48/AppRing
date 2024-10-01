using Microsoft.Extensions.Logging;
using Ring.ViewModel;
using CommunityToolkit.Maui;
using Ring.Utils;
using SkiaSharp.Views.Maui.Controls.Hosting;
using Plugin.LocalNotification;

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

            builder.Services.AddSingleton<IMyHttpClient, MyHttpClient>();

            builder.Services.AddTransient<RegisterPage>();
            builder.Services.AddTransient<RegisterViewModel>();

            builder.Services.AddTransient<VerificationPage>();
            builder.Services.AddTransient<VerificationViewModel>();

            builder.Services.AddTransient<MultiplePage>();
            builder.Services.AddTransient<MultipleViewModel>();

            builder.Services.AddTransient<AddPage>();
            builder.Services.AddTransient<AddViewModel>();

            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<SettingsViewModel>();

            builder.Services.AddTransient<MapPage>();
            builder.Services.AddTransient<MapViewModel>();

            //Environment.SetEnvironmentVariable("MQTT_SERVER", "10.20.100.28");
            Environment.SetEnvironmentVariable("MQTT_SERVER", "192.168.1.48");
            Environment.SetEnvironmentVariable("MQTT_PORT", "1883");

            //Environment.SetEnvironmentVariable("API_SERVER", "https://10.20.100.50:7046");
            Environment.SetEnvironmentVariable("API_SERVER", "https://192.168.1.14:7046");

            return builder.Build();
        }
    }
}