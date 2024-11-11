using AndroidX.Car.App;
using AndroidX.Car.App.Model;
using AndroidX.Core.Graphics.Drawable;
using Ring.Platforms.Android.Android_Auto.OnClickListeners;
using Ring.Shared;
using Ring.Utils;
using Action = AndroidX.Car.App.Model.Action;

namespace Ring.Platforms.Android.Android_Auto.Screens
{
    public class AAGateScreen : Screen
    {
        private Gate _gate;
        private IMyMqttClient? _mqttClient;
        public AAGateScreen(CarContext carContext, Gate gate, IMyMqttClient? mqttClient) : base(carContext)
        {
            _gate = gate;
            _mqttClient = mqttClient;
        }

        public override ITemplate OnGetTemplate()
        {
            var gateIcon = IconCompat.CreateWithResource(CarContext, Resource.Drawable.gate_icon);
            var carIcon = new CarIcon.Builder(gateIcon).Build();
            
            var openAction = new Action.Builder()
                .SetTitle("Open")
                .SetBackgroundColor(CarColor.Green)
                .SetOnClickListener(new GateOnClickListener(CarContext, ScreenManager, _gate, _mqttClient))
                .Build();

            return new MessageTemplate.Builder(_gate.address)
                .SetTitle(_gate.name)
                .SetHeaderAction(Action.Back)
                .SetIcon(carIcon)
                .AddAction(openAction)
                .Build();
        }
    }
}
