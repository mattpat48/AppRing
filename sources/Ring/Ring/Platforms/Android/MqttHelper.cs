using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ring.Platforms.Android
{
    public static class MqttHelper
    {
        public static T GetService<T>() where T : class
        {
            return MauiApplication.Current.Services.GetService<T>();
        }
    }

}
