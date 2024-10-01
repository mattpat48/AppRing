using Plugin.LocalNotification;
using Plugin.LocalNotification.EventArgs;

namespace Ring
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            LocalNotificationCenter.Current.NotificationActionTapped += OnNotificationTapped;
            MainPage = new AppShell();
        }

        private void OnNotificationTapped(NotificationActionEventArgs e)
        {
            if (Current is not null && Current.MainPage is not null)
            {
                Shell.Current.GoToAsync($"///{nameof(MainPage)}");
            }
            else
            {
                MainPage = new AppShell();
            }
        }
    }
}
