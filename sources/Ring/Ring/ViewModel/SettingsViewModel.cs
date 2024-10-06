using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ring.Utils;
namespace Ring.ViewModel;

public partial class SettingsViewModel : ObservableObject
{
    string _logsPath;
    string _gatesPath;

    private HttpClient _httpClient;

    public SettingsViewModel()
    {
        _logsPath = Path.Combine(FileSystem.AppDataDirectory, "logs.json");
        _gatesPath = Path.Combine(FileSystem.AppDataDirectory, "gates.json");
    }

    [RelayCommand]
    public void RemoveNumber()
    {
        try
        {
            SecureStorage.Remove("PhoneNumber");
            SecureStorage.Remove("MultipleDevices");
            Shell.Current.GoToAsync($"{nameof(MainPage)}");
            return;
        }
        catch (Exception)
        {
            NotificationTools.makeToast("Error while removing the phone number");
            return;
        }
    }

    [RelayCommand]
    public async Task MultipleDevices()
    {
        try
        {
            var multipleDevices = await SecureStorage.GetAsync("MultipleDevices");
            if (multipleDevices == "y")
            {
                string? phonenumber = await SecureStorage.GetAsync("PhoneNumber");
                if (string.IsNullOrEmpty(phonenumber))
                {
                    return;
                }
                var navigationParameters = new Dictionary<string, object>
                {
                    { "PhoneNumber", phonenumber},
                    { "HttpClient", _httpClient}
                };
                await Shell.Current.GoToAsync(nameof(MultiplePage), navigationParameters);
                return;
            }
            else
            {
                NotificationTools.makeToast("You don't have multiple devices");
                return;
            }
        }
        catch (Exception)
        {
            NotificationTools.makeToast("Error while checking multiple devices");
            return;
        }
    }

    internal void AssignHttpClient(HttpClient sharedClient)
    {
        _httpClient = sharedClient;
    }
}
