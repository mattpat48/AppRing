using Android.Content;
using AndroidX.Car.App;
using Ring.Platforms.Android.Android_Auto.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ring.Platforms.Android.Android_Auto.Sessions
{
    public class AASession : Session
    {
        public override Screen OnCreateScreen(Intent p0)
        {
            // TODO: chiedere i permessi per la posizione
            return new AAScreenMenu(CarContext);
        }
    }
}
