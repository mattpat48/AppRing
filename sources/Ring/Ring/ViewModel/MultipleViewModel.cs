using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ring.Services;
using Ring.Utils;
using System.Collections.ObjectModel;

namespace Ring.ViewModel;

[QueryProperty(nameof(PhoneNumber), "PhoneNumber")]
[QueryProperty(nameof(HttpClientProperty), "HttpClient")]

public partial class MultipleViewModel : ObservableObject
{
    // messaggio di errore
    [ObservableProperty]
    string descriptionText;
    // numero di telefono
    [ObservableProperty]
    string phoneNumber;
    [ObservableProperty]
    bool isRemoveEnabled;

    [ObservableProperty]
    bool hasMultipleDevices;
    [ObservableProperty]
    bool hasNoDevices;

    [ObservableProperty]
    ObservableCollection<Utils.Device> devices;

    private MultipleAPI _multipleAPI;
    [ObservableProperty]
    HttpClient httpClientProperty;

    public MultipleViewModel()
    {
       DescriptionText = string.Empty;
       phoneNumber = string.Empty;
       isRemoveEnabled = true;

       devices = new ObservableCollection<Utils.Device>();
    }

    public async Task OnAppearing()
    {
        if (HttpClientProperty != null)
        {
            _multipleAPI = new MultipleAPI(HttpClientProperty);
        }
        DescriptionText = "We have found other devices with the same number you just used (" + PhoneNumber + ": would you like to remove the login from them?";
        await GetAllDevices();
        return;
    }

    public void handleError(string error)
    {
        NotificationTools.makeToast(error);
        return;
    }

    private async Task GetAllDevices()
    {
        string? response;
        List<Utils.Device>? devicesReturned;
        (response, devicesReturned) = await _multipleAPI.GetAllDevices();
        if (response == "success")
        {
            if (devicesReturned == null || devicesReturned.Count == 0)
            {
                Devices = new ObservableCollection<Utils.Device>();
                HasMultipleDevices = false;
                HasNoDevices = true;
            }
            else if (devicesReturned != null && devicesReturned.Count > 0)
            {
                Devices = devicesReturned.ToObservableCollection();
                HasNoDevices = false;
                HasMultipleDevices = true;
            }
        }
        else
        {
            handleError(response);
        }
        return;
    }

    [RelayCommand]
    async Task RemoveDevice(Utils.Device toRemove)
    {
        string toRemoveResponse;
        toRemoveResponse = await _multipleAPI.LogoutDeviceRequest(toRemove.deviceId);

        if (toRemoveResponse == "success")
        {
            await GetAllDevices();
            NotificationTools.makeToast("Device removed");
        }
        else
        {
            handleError(toRemoveResponse);
        }
        return;
    }


    [RelayCommand]
    async Task Next()
    {
        await Shell.Current.Navigation.PopToRootAsync();
        return;
    }
}
