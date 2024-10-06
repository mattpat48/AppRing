using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Maui.Alerts;
using MQTTnet;
using MQTTnet.Client;
using Ring.Services;
using Ring.Utils;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Core.Extensions;
using System.Globalization;
using CommunityToolkit.Maui.Core;
using System.Net.NetworkInformation;

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
    bool isAddEnabled;

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
    private MqttFactory mqttServer;
    private IMqttClient mqttClient;
    private MqttClientOptions? options;

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
        selectedGate = new Gate(string.Empty, string.Empty, 0, 0);

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


    [RelayCommand]
    async Task Reload()
    {
        IsLoading = true;
        NotConnected = false;
        IsContentVisible = false;
        HasLogs = false;
        await Check();
        await GetGates();
        var location = await Geolocation.GetLocationAsync(new GeolocationRequest
        {
            DesiredAccuracy = GeolocationAccuracy.Medium,
            Timeout = TimeSpan.FromSeconds(10)
        });
        if (!NetworkInterface.GetIsNetworkAvailable() || location == null)
        {
            NotConnected = true;
            IsContentVisible = false;
            return;
        }
        IsLoading = false;
        IsContentVisible = true;
        return;
    }

    public void handleError(string message)
    {
        NotificationTools.makeToast(message);
        IsOpenEnabled = false;
        IsLoading = false;
        NotConnected = true;
        return;
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
                HasLogs = false;
                return false;
            }

            if (logsReturned != null && logsReturned.Count() > 0)
            {
                Logs = logsReturned
                    .OrderByDescending(x => x.date.Date)
                    .ThenByDescending(x => x.date.TimeOfDay)
                    .ToObservableCollection();
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
            return;
        }
        if (IsAdmin == false || IsAdmin == null)
        {
            IsAddEnabled = false;
        }
        else
        {
            IsAddEnabled = true;
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
    private async Task AddUser()
    {
        IsAddEnabled = false;
        IsOpenEnabled = false;

        var navigationParameters = new Dictionary<string, object>
        {
            { "GateId", selectedGate.gateId},
            { "GateName", selectedGate.name},
            { "HttpClient", _httpClient}
        };
        await Shell.Current.GoToAsync(nameof(AddPage), navigationParameters);

        IsAddEnabled = true;
        IsOpenEnabled = true;
        return;
    }


    [RelayCommand]
    public async Task<bool> Connect()
    {
        IsOpenEnabled = false;

        if (mqttClient == null || !mqttClient.IsConnected)
        {
            try
            {
                if (mqttClient == null)
                {
                    mqttServer = new MqttFactory();
                    mqttClient = mqttServer.CreateMqttClient();

                    string? mqttServerAddress = Environment.GetEnvironmentVariable("MQTT_SERVER");
                    string? mqttServerPort = Environment.GetEnvironmentVariable("MQTT_PORT");
                    if (string.IsNullOrEmpty(mqttServerAddress) || string.IsNullOrEmpty(mqttServerPort))
                    {
                        handleError("MQTT server address or port not found.");
                        IsContentVisible = false;
                        NotConnected = true;
                        return false;
                    }

                    string? number = await SecureStorage.GetAsync("PhoneNumber");
                    string? deviceId = await SecureStorage.GetAsync("DeviceId");

                    options = new MqttClientOptionsBuilder()
                        // ATTENZIONE: inserire l'indirizzo IP del server MQTT
                        .WithClientId(number + " / " + deviceId + " sender")
                        .WithTcpServer(mqttServerAddress, int.Parse(mqttServerPort))
                        .WithCredentials("user", "ringuser")
                        .Build();
                }

                MqttClientConnectResult connectionResult = await mqttClient.ConnectAsync(options);
                MqttClientConnectResultCode connectionResultCode = connectionResult.ResultCode;

                if (connectionResultCode == MqttClientConnectResultCode.Success)
                {
                    MqttClientSubscribeResult subscribeResult =
                        await mqttClient.SubscribeAsync(
                            new MqttTopicFilterBuilder()
                            .WithTopic("ringRequest/request")
                            .Build()
                        );
                    MqttClientSubscribeResultCode subscribeResultCode = subscribeResult.Items.First().ResultCode;
                    if (subscribeResultCode == MqttClientSubscribeResultCode.GrantedQoS0)
                    {
                        IsOpenEnabled = true;
                        IsLoading = false;
                    }
                    else
                    {
                        handleError("Subscription failed");
                        IsContentVisible = false;
                        NotConnected = true;
                        return false;
                    }
                }
                else
                {
                    handleError("Connection failed");
                    IsContentVisible = false;
                    NotConnected = true;
                    return false;
                }

            }
            catch (Exception)
            {
                handleError("Internal error while connecting to MQTT server.");
                IsContentVisible = false;
                NotConnected = true;
                return false;
            }
        }
        return mqttClient.IsConnected;
    }


    public async Task Disconnect()
    {
        try
        {
            await mqttClient.DisconnectAsync();
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
                MqttListenerManager.StopService();
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
                    if (mqttClient == null || !mqttClient.IsConnected)
                    {
                        bool connected = await Connect();
                        if (!connected)
                        {
                            IsContentVisible = false;
                            NotConnected = true;
                            return;
                        }
                    }

                    IsLoading = false;
                    NotConnected = false;
                    MqttListenerManager.StartService();
                    return;
                }
                else
                {
                    //var registerVm = new RegisterViewModel(_registerAPI, _verificationAPI);
                    //var registerPage = new RegisterPage(registerVm);
                    //await Shell.Current.Navigation.PushModalAsync(registerPage);

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
            handleError(ex.Message);
            return;
        }
    }

    [RelayCommand]
    public async Task OpenClicked()
    {
        await Open(selectedGate.gateId);
        return;
    }

    public async Task<string> Open(string selectedId)
    {
        bool connected = await Connect();
        if (!connected)
        {
            return "Not connected to MQTT server";
        }
        Location? location;

        try
        {
            
            var locationGranted = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (locationGranted != PermissionStatus.Granted)
            {
                await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }
            if (await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>() == PermissionStatus.Granted)
            {
                location = await Geolocation.GetLocationAsync();
            }
            else
            {
                NotificationTools.makeToast("Location permission not granted");
                return "Location permission not granted";
            }
            
            //location = new Location(45.4642, 9.1900);

            if (location != null)
            {
                
                var package = new
                {
                    id = await SecureStorage.GetAsync("DeviceId"),
                    phoneNumber = await SecureStorage.GetAsync("PhoneNumber"),
                    datetime = DateTime.Now,
                    lat = location.Latitude + "," + location.Longitude,
                    language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                    gate = selectedId,
                };

                IsOpenEnabled = false;

                // TEMP UNAVAIBLE
                /*
                string? privateDeviceKey = await SecureStorage.GetAsync("PrivateDeviceKey");
                string? publicServerKey = await SecureStorage.GetAsync("ServerKey");
                if (string.IsNullOrEmpty(privateDeviceKey) || string.IsNullOrEmpty(publicServerKey))
                {
                    handleError("Keys not found.");
                    return;
                }
                string? phoneNumber = await SecureStorage.GetAsync("PhoneNumber");
                string? deviceId = await SecureStorage.GetAsync("DeviceId");
                if (string.IsNullOrEmpty(phoneNumber) || string.IsNullOrEmpty(deviceId))
                {
                    handleError("Phone number or device id not found.");
                    return;
                }

                
                bool outcome;
                StringContent encrypted;
                (outcome, encrypted) = CryptographyTools.TotalEncrypt(privateDeviceKey, publicServerKey, JsonConvert.SerializeObject(package), phoneNumber, deviceId);

                if (!outcome)
                {
                    handleError("Error while encrypting data.");
                    return;
                }
                */

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic("ringRequest/request")
                    .WithPayload(JsonConvert.SerializeObject(package))
                    .Build();
                try
                {
                    MqttClientPublishResult publishResult = await mqttClient.PublishAsync(message);
                    MqttClientPublishReasonCode publishResultCode = publishResult.ReasonCode;

                    if (publishResultCode == MqttClientPublishReasonCode.Success)
                    {
                        string generateLogResponse = await _mainAPI.GenerateLog(selectedGate.gateId);
                        if (generateLogResponse != "success")
                        {
                            handleError(generateLogResponse);
                        }
                        await GetLogs();
                        IsOpenEnabled = true;
                        NotificationTools.makeToast("Message sent");
                        return "success";
                    }
                    else
                    {
                        NotificationTools.makeToast("Error while sending message");
                        IsOpenEnabled = true;
                        return "Error while sending message";
                    }
                }
                catch (Exception)
                {
                    NotificationTools.makeToast("Internal error while sending message");
                    IsOpenEnabled = true;
                    return "Internal error while sending message";
                }
            }
            else
            {
                NotificationTools.makeToast("Location not found");
                IsOpenEnabled = true;
                return "Location not found";
            }
        }
        catch (Exception)
        {
            NotificationTools.makeToast("Error while sending Open message");
            IsOpenEnabled = true;
            return "Error while sending Open message";
        }
    }

    public async Task ResetDefault()
    {
        if (mqttClient != null && mqttClient.IsConnected) await Disconnect();
        using (var writer = new StreamWriter(_gatesPath, false))
        {
            await writer.WriteAsync("{}");
        }
        Gates = new List<Gate>().ToObservableCollection();
        selectedGate = new Gate(string.Empty, string.Empty, 0, 0);
        GateNameDisplay = string.Empty;
        GateSelected = false;
        NoGates = false;
        HasGates = false;
        NoLogs = false;
        HasLogs = false;
    }

    public async Task OnAppearing()
    {
        IsContentVisible = false;
        var location = await Geolocation.GetLocationAsync(new GeolocationRequest
        {
            DesiredAccuracy = GeolocationAccuracy.Medium,
            Timeout = TimeSpan.FromSeconds(10)
        });
        if (!NetworkInterface.GetIsNetworkAvailable() || location == null)
        {
            NotConnected = true;
            IsContentVisible = false;
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
        return;
    }
}