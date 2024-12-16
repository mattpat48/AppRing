using Ring.ViewModel;
using Mapsui.UI.Maui;
using Mapsui.Projections;
using Mapsui;
using Mapsui.Styles;
using Color = Microsoft.Maui.Graphics.Color;
using Mapsui.Limiting;
using System.Net.NetworkInformation;
using GeolocatorPlugin;
using GeolocatorPlugin.Abstractions;
using Ring.Shared;
namespace Ring;

public partial class MapPage : ContentPage
{
    List<Pin>? Pins;
    private IGeolocation _geolocation;
    private readonly IMyHttpClient _myHttpClient;
    private readonly IMyMqttClient _myMqttClient;
    private DateTime lastOpenCall = DateTime.MinValue;

    //private CancellationTokenSource _locationUpdateCts;
    //private const int LocationUpdateInterval = 5000;

    public MapPage(MapViewModel viewModel, IMyHttpClient myHttpClient, IMyMqttClient myMqttClient)
    {
        InitializeComponent();
        BindingContext = viewModel;

        _geolocation = Geolocation.Default;
        _myMqttClient = myMqttClient;
        _myHttpClient = myHttpClient;
        viewModel.AssignHttpClient(_myHttpClient.sharedClient, _myHttpClient.sharedMainAPI);
        viewModel.AssignMqttClient(_myMqttClient.sharedMqttClient, _myMqttClient.sharedOptions);
    }

    public void setPinCallout(Pin pin)
    {
        Callout c = pin.Callout;

        c.Type = CalloutType.Detail;

        c.Padding = new Thickness(4, 4, 4, 4);
        c.RectRadius = 10;
        c.RotateWithMap = false;
        c.ArrowAlignment = ArrowAlignment.Bottom;

        c.TitleFontSize = 20;
        c.Title = pin.Label;

        /*
        c.SubtitleFontSize = 15;
        c.SubtitleFontColor = Color.FromArgb("575757");
        c.SubtitleTextAlignment = TextAlignment.Center;
        c.Subtitle = pin.Address;
        */
        c.Subtitle = "";

        c.IsClosableByClick = true;
        c.ShadowWidth = 5;
        return;
    }

    public async Task createMap()
    {
        try
        {
            var viewModel = BindingContext as MapViewModel;
            if (viewModel == null)
            {
                return;
            }

            var map = new Mapsui.Map();

            map?.Layers.Add(Mapsui.Tiling.OpenStreetMap.CreateTileLayer());

            var panBounds = GetLimits();
            map.Navigator.Limiter = new ViewportLimiterKeepWithinExtent();
            map.Navigator.RotationLock = true;
            map.Navigator.OverridePanBounds = panBounds;

            mapView.Map = map;

            var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(1));
            var startLocation = await _geolocation.GetLocationAsync(request);
            var lonLat = SphericalMercator.FromLonLat(startLocation.Longitude, startLocation.Latitude);
            var point = new MPoint(lonLat.x, lonLat.y);

            if (startLocation != null)
            {

                map.Navigator.CenterOnAndZoomTo(point, map.Navigator.Resolutions[16]);
                mapView.MyLocationLayer.UpdateMyLocation(new Mapsui.UI.Maui.Position(startLocation.Latitude, startLocation.Longitude), true);
            }
            
            Pins = await viewModel.SetPins();

            if (Pins == null)
            {
                return;
            }
            foreach (var pin in Pins)
            {
                var toAdd = new Pin();
                toAdd.Label = pin.Label;
                toAdd.Position = pin.Position;
                toAdd.Tag = pin.Tag;
                toAdd.IsVisible = true;
                toAdd.RotateWithMap = false;
                toAdd.Address = pin.Address;
                setPinCallout(toAdd);
                mapView.Pins.Add(toAdd);
            }
            mapView.PinClicked += (s, e) =>
            {
                if (e.Pin != null)
                {
                    e.Pin.ShowCallout();
                    if (e.Pin.Label != null && e.Pin.Tag != null)
                    {
                        viewModel.OnGateSelected(e.Pin.Label, e.Pin.Tag.ToString(), e.Pin.Address.ToString());
                    }
                }

                e.Handled = true;
            };
            
            mapView.SelectedPinChanged += (s, e) =>
            {
                viewModel.OnGateNotSelected();
                if (e.SelectedPin != null)
                {
                    e.SelectedPin.ShowCallout();
                    if (e.SelectedPin.Label != null && e.SelectedPin.Tag != null)
                    {
                        viewModel.OnGateSelected(e.SelectedPin.Label, e.SelectedPin.Tag.ToString(), e.SelectedPin.Address.ToString());
                    }
                }
                foreach (var pin in mapView.Pins)
                {
                    if (pin != e.SelectedPin)
                    {
                        pin.HideCallout();
                    }
                }
            };

            mapView.MapClicked += (s, e) =>
            {
                foreach (var pin in mapView.Pins)
                {
                    if (pin.Callout.IsVisible)
                    {
                        pin.HideCallout();
                    }
                }
                viewModel.OnGateNotSelected();
            };

        }
        catch (Exception)
        {
            return;
        }
    }

    public void refreshMap(double latitude, double longitude)
    {
        mapView.MyLocationLayer.UpdateMyLocation(new Mapsui.UI.Maui.Position(latitude, longitude), true);
        return;
    }

    private static MRect GetLimits()
    {
        var (minX, minY) = SphericalMercator.FromLonLat(-180, -85.05);
        var (maxX, maxY) = SphericalMercator.FromLonLat(180, 85.05);
        return new MRect(minX, minY, maxX, maxY);
    }

    public async Task<bool> IsGpsEnabledAsync()
    {
        // Check if location permissions are granted
        var locationPermission = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

        if (locationPermission != PermissionStatus.Granted)
        {
            locationPermission = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (locationPermission != PermissionStatus.Granted)
            {
                // Permission not granted, GPS status cannot be determined
                return false;
            }
        }

        try
        {
            // Try to get the current location to determine if GPS is enabled
            var location = await Geolocation.GetLastKnownLocationAsync();

            if (location != null)
            {
                // GPS is enabled, and we have a recent location
                return true;
            }
            else
            {
                // Attempt to get an updated location
                location = await Geolocation.GetLocationAsync(new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.Medium,
                    Timeout = TimeSpan.FromSeconds(10)
                });

                if (location != null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        catch (Exception)
        {
            // Other error occurred (e.g., timeout)
            return false;
        }
    }

    public async void ToggleGPS(bool toggleOn)
    {
        if (toggleOn)
        {
            if (CrossGeolocator.Current.IsListening)
            {
                return;
            }
            if (await CrossGeolocator.Current.StartListeningAsync(TimeSpan.FromSeconds(1), 3, true, new ListenerSettings
            {
                ActivityType = ActivityType.OtherNavigation,
                AllowBackgroundUpdates = true,
                DeferLocationUpdates = true,
                ListenForSignificantChanges = false,
                PauseLocationUpdatesAutomatically = false,
            }))
            {
                CrossGeolocator.Current.PositionChanged += CrossGeolocator_Current_PositionChanged;
                CrossGeolocator.Current.PositionError += CrossGeolocator_Current_PositionError;
            }
        }
        else
        {
            if (!CrossGeolocator.Current.IsListening)
            {
                return;
            }
            if (await CrossGeolocator.Current.StopListeningAsync())
            {
                CrossGeolocator.Current.PositionChanged -= CrossGeolocator_Current_PositionChanged;
                CrossGeolocator.Current.PositionError -= CrossGeolocator_Current_PositionError;
            }
        }
    }

    private void CrossGeolocator_Current_PositionError(object? sender, PositionErrorEventArgs e)
    {
    }

    private async void CrossGeolocator_Current_PositionChanged(object? sender, PositionEventArgs e)
    {
        refreshMap(e.Position.Latitude, e.Position.Longitude);
        var viewModel = BindingContext as MapViewModel;
        if ((DateTime.Now - lastOpenCall).TotalMinutes >= 1)
        {
            lastOpenCall = DateTime.Now;
            await viewModel.OnLocationChanged(e.Position.Latitude, e.Position.Longitude);
        }
    }

    protected override async void OnAppearing()
	{
        base.OnAppearing();
        var viewModel = BindingContext as MapViewModel;
        if (viewModel != null)
        {
            if (NetworkInterface.GetIsNetworkAvailable() && await IsGpsEnabledAsync() == true)
            {
                await viewModel.OnAppearing();
                await createMap();
                labelNoCon.IsVisible = false;
                mapContainer.IsVisible = true;
                mapContainer.IsEnabled = true;
                ToggleGPS(true);
                return;
            }
            else
            {
                labelNoCon.IsVisible = true;
                mapContainer.IsEnabled = false;
                mapContainer.IsVisible = false;
                return;
            }
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        ToggleGPS(false);
    }

    private void refresh_Swiped(object sender, EventArgs e)
    {
        if (NetworkInterface.GetIsNetworkAvailable())
        {
            labelNoCon.IsVisible = false;
            mapContainer.IsVisible = true;
            mapContainer.IsEnabled = true;
            return;
        }
    }
}