using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Plugin.LocalNotification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ring.Utils
{
    public static class NotificationTools
    {
        public static async void makeToast(string message)
        {

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            string text = message;
            ToastDuration duration = ToastDuration.Short;
            double fontSize = 14;

            var toast = Toast.Make(text, duration, fontSize);
            await toast.Show(cancellationTokenSource.Token);
        }

        public static void SendNotification(string title, string description)
        {

            var notification = new NotificationRequest
            {
                NotificationId = (new object().GetHashCode()),
                Title = title,
                Description = description,
                Schedule = new NotificationRequestSchedule
                {
                    NotifyTime = DateTime.Now.AddSeconds(1)
                }
            };
            LocalNotificationCenter.Current.Show(notification);
        }

    }
}
