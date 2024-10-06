using AndroidX.Car.App;
using AndroidX.Car.App.Model;
using Ring.Platforms.Android.Android_Auto.Enums;
using Ring.Platforms.Android.Android_Auto.Screens;
using Ring.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ring.Platforms.Android.Android_Auto.OnClickListeners
{
    public class NavigationOnClickListener : Java.Lang.Object, IOnClickListener
    {
        private readonly CarContext _carContext;
        private readonly ScreenManager _screenManager;
        private readonly AAScreen _toNavigateTo;
        private readonly Gate? _gate;

        public NavigationOnClickListener(CarContext carContext, ScreenManager screenManager, AAScreen toNavigateTo, Gate gate = null)
        {
            _carContext = carContext;
            _screenManager = screenManager;
            _toNavigateTo = toNavigateTo;
            _gate = gate;
        }

        public void OnClick()
        {
            switch (_toNavigateTo)
            {
                case AAScreen.None:
                    return;
                case AAScreen.GateGrid:
                    _screenManager.Push(new AAGatesGrid(_carContext));
                    break;
                case AAScreen.GateScreen:
                    _screenManager.Push(new AAGateScreen(_carContext, _gate));
                    break;
                case AAScreen.Pop:
                    _screenManager.Pop();
                    break;
                default:
                    return;
            }
        }
    }
}
