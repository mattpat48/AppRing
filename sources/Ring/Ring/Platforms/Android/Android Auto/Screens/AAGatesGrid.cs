using AndroidX.Car.App;
using AndroidX.Car.App.Model;
using AndroidX.Core.Graphics.Drawable;
using Newtonsoft.Json;
using Ring.Platforms.Android.Android_Auto.OnClickListeners;
using Ring.Utils;
using Xamarin.Google.Crypto.Tink.Signature;
using Action = AndroidX.Car.App.Model.Action;

namespace Ring.Platforms.Android.Android_Auto.Screens
{
    public class AAGatesGrid : Screen
    {
        private List<Gate>? _gates;
        public AAGatesGrid(CarContext carContext) : base(carContext)
        {
            _gates = new List<Gate>();
        }

        public override ITemplate OnGetTemplate()
        {
            string? phoneNumber = SecureStorage.GetAsync("PhoneNumber").Result;
            if (string.IsNullOrEmpty(phoneNumber))
            {
                return NoNumberFoundScreen();
            }
            else
            {
                return GatesGridScreen();
            }
        }

        public ITemplate NoNumberFoundScreen()
        {
            var retryAction = new Action.Builder()
                .SetTitle("Retry")
                .SetBackgroundColor(CarColor.Blue)
                .SetOnClickListener(new RetryOnClickListener(CarContext, ScreenManager))
                .Build();

            return new MessageTemplate.Builder("No phone number found on device: login from your smartphone and try again.")
                .SetHeaderAction(Action.AppIcon)
                .AddAction(retryAction)
                .Build();
        }

        public ITemplate GatesGridScreen()
        {
            string _gatesPath = Path.Combine(FileSystem.AppDataDirectory, "gates.json");
            if (!File.Exists(_gatesPath) || File.ReadAllText(_gatesPath) == "{}")
            {
                _gates = new List<Gate>();
            }
            else
            {
                _gates = JsonConvert.DeserializeObject<List<Gate>>(File.ReadAllText(_gatesPath));
            }

            var gateIcon = IconCompat.CreateWithResource(CarContext, Resource.Drawable.gate_icon);
            var carIcon = new CarIcon.Builder(gateIcon).Build();

            var gridItemListBuilder = new ItemList.Builder()
                .SetNoItemsMessage("No gates to show");

            foreach (var gate in _gates)
            {
                var gridItem = new GridItem.Builder()
                    .SetTitle($"{gate.name}")
                    .SetImage(carIcon)
                    .SetOnClickListener(new NavigationOnClickListener(CarContext, ScreenManager, Enums.AAScreen.GateScreen, gate))
                    .Build();

                gridItemListBuilder.AddItem(gridItem);
            }

            var gridItemList = gridItemListBuilder.Build();

            return new GridTemplate.Builder()
                .SetHeaderAction(Action.AppIcon)
                .SetTitle("Ring Gates")
                .SetSingleList(gridItemList)
                .SetItemSize(1)
                .Build();
        }
    }
}
