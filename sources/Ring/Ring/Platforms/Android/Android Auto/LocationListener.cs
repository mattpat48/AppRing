using Android.Locations;
using Android.OS;
using Android.Runtime;
using Location = Android.Locations.Location;

public class LocationListener : Java.Lang.Object, ILocationListener
{
    private TaskCompletionSource<Location> taskCompletionSource;
    private LocationManager locationManager;

    public LocationListener(LocationManager locationManager, TaskCompletionSource<Location> tcs)
    {
        this.taskCompletionSource = tcs;
        this.locationManager = locationManager;
    }

    public void OnLocationChanged(Location location)
    {
        taskCompletionSource.TrySetResult(location);
        locationManager.RemoveUpdates(this);
    }

    public void OnProviderDisabled(string provider)
    {
        throw new NotImplementedException();
    }

    public void OnProviderEnabled(string provider)
    {
        throw new NotImplementedException();
    }

    public void OnStatusChanged(string? provider, [GeneratedEnum] Availability status, Bundle? extras)
    {
        throw new NotImplementedException();
    }
}