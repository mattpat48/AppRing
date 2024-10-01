using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;

namespace Ring.Utils
{
    public static class MqttListenerManager
    {
        public static void StartService()
        {
            MainActivity.ActivityCurrent?.StartMqttService();
        }

        public static void StopService()
        {
            MainActivity.ActivityCurrent?.StopMqttService();
        }

    }
}
