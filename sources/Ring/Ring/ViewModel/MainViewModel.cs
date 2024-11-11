using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MQTTnet.Client;
using Ring.Services;
using Ring.Utils;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Core.Extensions;
using System.Net.NetworkInformation;
using Ring.Shared;

namespace Ring.ViewModel;

public partial class MainViewModel : ObservableObject
{

    // caricamento si o no
    [ObservableProperty]
    bool isLoading;
    [ObservableProperty]
    bool isGateLoading;
    [ObservableProperty]
    bool isContentVisible;

    [ObservableProperty]
    bool isOpenEnabled;
    [ObservableProperty]
    bool isUsersEnabled;

    [ObservableProperty]
    string gateNameDisplay;

    [ObservableProperty]
    bool gateSelected;
    [ObservableProperty]
    bool noGates;
    [ObservableProperty]
    bool hasGates;
    [ObservableProperty]
    bool noLogs;
    [ObservableProperty]
    bool hasLogs;
    [ObservableProperty]
    bool? isAdmin;

    [ObservableProperty]
    bool notConnected;

    // server, client e opzioni per scambio mqtt
    private IMqttClient _mqttClient;
    private MqttClientOptions _options;
   
    private HttpClient _httpClient;
    private MainAPI _mainAPI;
    private RegisterAPI _registerAPI;

    [ObservableProperty]
    ObservableCollection<Gate> gates;
    Gate selectedGate;

    [ObservableProperty]
    ObservableCollection<Log> logs;

    string _gatesPath;

    // costruttore per il viewmodel
    public MainViewModel()
    {
        IsLoading = false;
        IsGateLoading = false;
        IsContentVisible = false;

        IsOpenEnabled = true;

        GateNameDisplay = string.Empty;

        GateSelected = false;
        NoGates = false;
        HasGates = false;
        NoLogs = false;
        HasLogs = false;
        IsAdmin = false;

        NotConnected = false;

        Gates = new List<Gate>().ToObservableCollection();
        selectedGate = new Gate(string.Empty, string.Empty, 0, 0, "n", DateTime.MinValue);

        Logs = new List<Log>().ToObservableCollection();

        _gatesPath = Path.Combine(FileSystem.AppDataDirectory, "gates.json");
    }

    public void AssignHttpClient(HttpClient httpClient, MainAPI mainAPI)
    {
        _httpClient = httpClient;
        _mainAPI = mainAPI;
        _registerAPI = new RegisterAPI(_httpClient);
        return;
    }

    public void AssignMqttClient(IMqttClient sharedMqttClient, MqttClientOptions options)
    {
        _mqttClient = sharedMqttClient;
        _options = options;
        return;
    }


    [RelayCommand]
    async Task Reload()
    {
        try
        {
            var location = await Geolocation.GetLocationAsync(new GeolocationRequest
            {
                DesiredAccuracy = GeolocationAccuracy.Medium,
                Timeout = TimeSpan.FromSeconds(10)
            });
            if (!NetworkInterface.GetIsNetworkAvailable() || location == null)
            {
                IsContentVisible = false;
                NotConnected = true;
                return;
            }
            IsLoading = true;
            NotConnected = false;
            IsContentVisible = false;
            GateSelected = false;
            HasLogs = false;
            await Check();
            await GetGates();
            IsLoading = false;
            IsContentVisible = true;
            return;
        }
        catch (Exception ex)
        {
            handleError(ex.Message);
            return;
        }
    }

    public void handleError(string message)
    {
        IsOpenEnabled = false;
        NotificationTools.makeToast(message);
        IsLoading = false;
        IsGateLoading = false;
        IsOpenEnabled = true;
        return;
    }

    public void resetShown()
    {
        GateSelected = false;
        selectedGate = new Gate(string.Empty, string.Empty, 0, 0, "n", DateTime.MinValue);
        NoGates = false;
        HasGates = false;
        NoLogs = false;
        HasLogs = false;
    }

    async Task GetGates()
    {
        string getGatesResponse;
        List<Gate>? gatesReturned;
        try
        {
            (getGatesResponse, gatesReturned) = await _mainAPI.GetAllGates();
            if (getGatesResponse != "success")
            {
                handleError("Error while retrieving gates.");
                resetShown();
                return;
            }
            else if (gatesReturned == null || gatesReturned.Count() == 0)
            {
                NoGates = true;
                return;
            }

            foreach (var gate in gatesReturned)
            {
                gate.address = await AddressFromLocation(gate.latitude, gate.longitude);
            }
            Gates = gatesReturned.ToObservableCollection();

            var json = JsonConvert.SerializeObject(gatesReturned);
            using (var writer = new StreamWriter(_gatesPath, false))
            {
                await writer.WriteAsync(json);
            }

            if (Gates.Count() == 1)
            {
                await SelectGate(Gates.First());
                return;
            }
            else
            {
                IsLoading = false;
                GateSelected = false;
                NoGates = false;
                HasGates = true;
                return;
            }
        }
        catch (Exception)
        {
            handleError("Internal exception error while getting gates.");
            resetShown();
            return;
        }
    }

    private async Task<string?> AddressFromLocation(double latitude, double longitude)
    {
        IEnumerable<Placemark> placemarks = await Geocoding.Default.GetPlacemarksAsync(latitude, longitude);

        Placemark? placemark = placemarks?.FirstOrDefault();

        if (placemark != null)
        {
            return
                $"{placemark.Thoroughfare} {placemark.SubThoroughfare}, {placemark.Locality} {placemark.PostalCode}, {placemark.AdminArea}, {placemark.CountryName}";
        }
        else
        {
            return null;
        }
    }
    

    async Task<bool> GetLogs()
    {
        string getLogsResponse;
        List<Log>? logsReturned;

        string logsToAddPath = Path.Combine(FileSystem.AppDataDirectory, "logsToUpdate.json");
        if (File.Exists(logsToAddPath))
        {
            var toAdd = JsonConvert.DeserializeObject<List<Log>>(File.ReadAllText(logsToAddPath));
            if (toAdd != null && toAdd.Count() > 0)
            {
                string logsUpdatedResponse = await _mainAPI.ManuallyAddLogs(toAdd);
                if (logsUpdatedResponse != "success")
                {
                    handleError("Error while updating logs.");
                    HasLogs = false;
                    NoLogs = false;
                }
                else
                {
                    File.Delete(logsToAddPath);
                }
            }
        }

        try
        {
            if (selectedGate == null)
            {
                handleError("No gate selected.");
                return false;
            }
            (getLogsResponse, logsReturned) = await _mainAPI.GetLogs(selectedGate.gateId);
            if (getLogsResponse != "success")
            {
                handleError("Error while retrieving logs.");
                NoLogs = false;
                HasLogs = false;
                return false;
            }

            if (logsReturned != null && logsReturned.Count() > 0)
            {
                Logs = logsReturned
                    .OrderByDescending(x => x.date.Date)
                    .ThenByDescending(x => x.date.TimeOfDay)
                    .ToObservableCollection();
                NoLogs = false;
                HasLogs = true;
                return true;
            }
            else
            {
                return false;
            }
        }
        catch (Exception)
        {
            handleError("Error while retrieving logs.");
            NoLogs = false;
            HasLogs = false;
            return false;
        }
    }

    [RelayCommand]
    private async Task SelectGate(Gate selected)
    {
        GateSelected = false;
        IsGateLoading = true;
        
        HasLogs = false;
        NoLogs = false;

        selectedGate = selected;

        GateNameDisplay = selectedGate.name;

        (string isAdminResponse, IsAdmin) = await _mainAPI.IsAdmin(selected.gateId);
        if (isAdminResponse != "success")
        {
            handleError("Error while checking admin permissions.");
            resetShown();
            return;
        }
        if (IsAdmin == false || IsAdmin == null)
        {
            IsUsersEnabled = false;
        }
        else
        {
            IsUsersEnabled = true;
        }

        IsOpenEnabled = true;

        bool logs = await GetLogs();

        IsGateLoading = false;
        GateSelected = true;

        if (logs)
        {
            HasLogs = true;
            NoLogs = false;
        }
        else
        {
            HasLogs = false;
            NoLogs = true;
        }

        return;
    }

    [RelayCommand]
    public async Task ManageUsers()
    {
        IsUsersEnabled = false;
        IsOpenEnabled = false;

        var navigationParameters = new Dictionary<string, object>
        {
            { "GateId", selectedGate.gateId},
            { "GateName", selectedGate.name},
            { "HttpClient", _httpClient}
        };
        await Shell.Current.GoToAsync(nameof(UsersPage), navigationParameters);

        IsUsersEnabled = true;
        IsOpenEnabled = true;
        return;
    }


    public async Task Disconnect()
    {
        try
        {
            await _mqttClient.DisconnectAsync();
        }
        catch (Exception ex)
        {
            handleError(ex.Message);
        }
    }

    [RelayCommand]
    public async Task Check()
    {
        try
        {
            string getServerKeyResponse = await _registerAPI.GetServerKey();
            if (getServerKeyResponse != "success")
            {
                handleError(getServerKeyResponse);
                resetShown();
                IsLoading = false;
                IsContentVisible = false;
                NotConnected = true;
                return;
            }
            string? phoneNumber = await SecureStorage.GetAsync("PhoneNumber");
            string? isVerified = await SecureStorage.GetAsync("IsVerified");
            if (string.IsNullOrEmpty(phoneNumber) || string.IsNullOrEmpty(isVerified) || string.Compare(isVerified, "n") == 0)
            {
                IsLoading = false;
                if (_mqttClient != null)
                {
                    MqttManager.StopService();
                }
                await ResetDefault();
                var navigationParameters = new Dictionary<string, object>
                {
                    { "HttpClient", _httpClient}
                };
                await Shell.Current.GoToAsync(nameof(RegisterPage), navigationParameters);
                return;
            }
            else
            {
                var logoutResponse = await _mainAPI.CheckLogout();
                if (logoutResponse == "success")
                {
                    if (!GateSelected) await GetGates();
                    IsContentVisible = true;
                    if (_mqttClient == null || !_mqttClient.IsConnected)
                    {
                        MqttManager.StartService();
                    }

                    IsLoading = false;
                    NotConnected = false;
                    return;
                }
                else
                {
                    if (_mqttClient != null)
                    {
                        MqttManager.StopService();
                    }
                    await ResetDefault();
                    var navigationParameters = new Dictionary<string, object>
                    {
                        { "HttpClient", _httpClient}
                    };
                    await Shell.Current.GoToAsync(nameof(RegisterPage), navigationParameters);
                    return;
                }    
            }
        }
        catch (Exception ex)
        {
            NoGates = false;
            HasGates = false;
            NoLogs = false;
            HasLogs = false;
            handleError(ex.Message);
            return;
        }
    }

    [RelayCommand]
    public async Task OpenClicked()
    {
        string openResult = await MqttManager.Open(_mqttClient, _options, selectedGate.gateId);
        if (openResult != "success")
        {
            handleError(openResult);
            return;
        }
        else
        {
            string logResponse = await MqttManager.LogPostOpen(_mainAPI, selectedGate.gateId, selectedGate.name);
            if (logResponse != "success")
            {
                handleError(logResponse);
                return;
            }
            await GetLogs();
            return;
        }
    }

    public async Task ResetDefault()
    {
        if (_mqttClient != null && _mqttClient.IsConnected) await Disconnect();
        using (var writer = new StreamWriter(_gatesPath, false))
        {
            await writer.WriteAsync("{}");
        }
        Gates = new List<Gate>().ToObservableCollection();
        selectedGate = new Gate(string.Empty, string.Empty, 0, 0, "n", DateTime.MinValue);
        GateNameDisplay = string.Empty;
        GateSelected = false;
        NoGates = false;
        HasGates = false;
        NoLogs = false;
        HasLogs = false;
    }

    public async Task OnAppearing()
    {
        try
        {
            IsContentVisible = false;
            PermissionStatus locationStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (locationStatus != PermissionStatus.Granted)
            {
                await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }
            var location = await Geolocation.GetLocationAsync(new GeolocationRequest
            {
                DesiredAccuracy = GeolocationAccuracy.Medium,
                Timeout = TimeSpan.FromSeconds(10)
            });
            if (!NetworkInterface.GetIsNetworkAvailable() || location == null)
            {
                IsContentVisible = false;
                NotConnected = true;
                return;
            }
            PermissionStatus notificationStatus = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
            if (notificationStatus != PermissionStatus.Granted)
            {
                await Permissions.RequestAsync<Permissions.PostNotifications>();
            }
            IsLoading = true;
            await Check();
            IsLoading = false;
            IsContentVisible = true;
        }
        catch (Exception ex)
        {
            handleError(ex.Message);
            resetShown();
            IsContentVisible = false;
            NotConnected = true;
            return;
        }
        return;
    }
}