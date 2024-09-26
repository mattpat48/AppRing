using Ring.ViewModel;
using Mapsui.UI.Maui;
using Mapsui.Projections;
using Mapsui;
using Mapsui.Styles;
using Color = Microsoft.Maui.Graphics.Color;
using Ring.Utils;
using Mapsui.Layers;
using Mapsui.Limiting;
using System.Net.NetworkInformation;
using Mapsui.Nts.Extensions;
namespace Ring;

public partial class MapPage : ContentPage
{
    List<Pin>? Pins;
    MapControl? MapControl;
    private IGeolocation _geolocation;
    private bool _disposed;

    public MapPage(MapViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        _geolocation = Geolocation.Default;
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

        c.SubtitleFontSize = 10;
        c.SubtitleFontColor = Color.FromArgb("575757");
        c.SubtitleTextAlignment = TextAlignment.Center;
        c.Subtitle = pin.Position.Latitude.ToString() + ", " + pin.Position.Longitude.ToString();
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

            if (startLocation == null)
            {
                return;
            }
            var lonLat = SphericalMercator.FromLonLat(startLocation.Longitude, startLocation.Latitude);
            var point = new MPoint(lonLat.x, lonLat.y);
            refreshMap(startLocation);

            map.Navigator.CenterOnAndZoomTo(point, map.Navigator.Resolutions[16]);
            mapView.MyLocationLayer.UpdateMyLocation(new Position(startLocation.Latitude, startLocation.Longitude), true);
            
            Pins = viewModel.SetPins();

            if (Pins == null)
            {
                return;
            }
            foreach (var pin in Pins)
            {
                var toAdd = new Pin();
                toAdd.Label = pin.Label;
                toAdd.Position = pin.Position;
                toAdd.IsVisible = true;
                toAdd.RotateWithMap = false;
                setPinCallout(toAdd);
                mapView.Pins.Add(toAdd);
            }
            mapView.PinClicked += (s, e) =>
            {
                if (e.Pin != null)
                {
                    if (e.NumOfTaps == 1)
                    {
                        if (e.Pin.Callout.IsVisible)
                        {
                            e.Pin.HideCallout();
                        }
                        else
                        {
                            foreach (var pin in mapView.Pins)
                            {
                                pin.HideCallout();

                            }
                            e.Pin.ShowCallout();
                        }
                    }
                }

                e.Handled = true;
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
            };

        }
        catch (Exception ex)
        {
            return;
        }
    }

    public void refreshMap(Location? location)
    {
        if (mapView.Map == null || location == null)
        {
            return;
        }

        var lonLat = SphericalMercator.FromLonLat(location.Longitude, location.Latitude);
        var point = new MPoint(lonLat.x, lonLat.y);

        if (mapView.MyLocationLayer == null)
        {
            return;
        }

        mapView.MyLocationLayer.UpdateMyLocation(new Position(location.Latitude, location.Longitude), true);

        // Optionally center map on the user's location
        mapView.Map.Navigator.CenterOn(point);

        return;
    }

    // Start tracking location
    private async Task StartLocationTrackingAsync()
    {
        try
        {
            var request = new GeolocationListeningRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(1));
            await _geolocation.StartListeningForegroundAsync(request);
            _geolocation.LocationChanged += (s, e) =>
            {
                refreshMap(e.Location);
            };
        }
        catch (Exception ex)
        {
        }
    }

    private void StopLocationTracking()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        mapView.MyLocationLayer?.Dispose();
        _geolocation.StopListeningForeground();
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


    protected override async void OnAppearing()
	{
        base.OnAppearing();
        var viewModel = BindingContext as MapViewModel;
        if (viewModel != null)
        {
            if (NetworkInterface.GetIsNetworkAvailable() || await IsGpsEnabledAsync() == false)
            {
                await createMap();
                labelNoCon.IsVisible = false;
                mapView.IsVisible = true;
                mapView.IsEnabled = true;
                await StartLocationTrackingAsync();
                return;
            }
            else
            {
                labelNoCon.IsVisible = true;
                mapView.IsEnabled = false;
                mapView.IsVisible = false;
                return;
            }
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopLocationTracking();
    }

    private void refresh_Swiped(object sender, EventArgs e)
    {
        if (NetworkInterface.GetIsNetworkAvailable())
        {
            labelNoCon.IsVisible = false;
            mapView.IsVisible = true;
            mapView.IsEnabled = true;
            return;
        }
    }
}