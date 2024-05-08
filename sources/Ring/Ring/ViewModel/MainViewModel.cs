using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MQTTnet;
using MQTTnet.Client;
using Ring.Services;
using Ring.Utils;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Core.Extensions;
using System.Globalization;
using Ring.ViewModel;

namespace Ring.ViewModel;

public partial class MainViewModel : ObservableObject
{

    // caricamento si o no
    [ObservableProperty]
    bool isLoading;

    [ObservableProperty]
    bool isOpenEnabled;
    [ObservableProperty]
    bool isReloadEnabled;
    [ObservableProperty]
    bool isAddEnabled;

    // messaggio di errore
    [ObservableProperty]
    string messageText;
    [ObservableProperty]
    string textColor;

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

    // server, client e opzioni per scambio mqtt
    private MqttFactory mqttServer;
    private IMqttClient mqttClient;
    private MqttClientOptions? options;

    private HttpClient _httpClient;
    private MainAPI _mainAPI;
    private RegisterAPI _registerAPI;
    private VerificationAPI _verificationAPI;
    private AddAPI _addAPI;

    [ObservableProperty]
    ObservableCollection<Gate> gates;
    Gate selectedGate;

    [ObservableProperty]
    ObservableCollection<Log> logs;

    

    // costruttore per il viewmodel
    public MainViewModel()
    {
        IsLoading = false;

        IsOpenEnabled = true;
        IsReloadEnabled = false;

        MessageText = string.Empty;
        TextColor = "White";

        GateNameDisplay = string.Empty;

        GateSelected = false;
        NoGates = false;
        HasGates = false;
        NoLogs = false;
        HasLogs = false;
        IsAdmin = false;

        // inizializzo il client e server mqtt
        mqttServer = new MqttFactory();
        mqttClient = mqttServer.CreateMqttClient();

        string? mqttServerAddress = Environment.GetEnvironmentVariable("MQTT_SERVER");
        string? mqttServerPort = Environment.GetEnvironmentVariable("MQTT_PORT");

        HttpClientHandler clientHandler = new HttpClientHandler();
        clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

        _httpClient = new HttpClient(clientHandler);
        _mainAPI = new MainAPI(_httpClient);
        _registerAPI = new RegisterAPI(_httpClient);
        _verificationAPI = new VerificationAPI(_httpClient);
        _addAPI = new AddAPI(_httpClient);

        Gates = new List<Gate>().ToObservableCollection();
        selectedGate = new Gate(string.Empty, string.Empty);

        Logs = new List<Log>().ToObservableCollection();

        if (string.IsNullOrEmpty(mqttServerAddress) || string.IsNullOrEmpty(mqttServerPort))
        {
            handleError("MQTT server address or port not found.");
            return;
        }
        else
        {
            // inizializzo le opzioni del server mqtt
            options = new MqttClientOptionsBuilder()
                // ATTENZIONE: inserire l'indirizzo IP del server MQTT
                .WithTcpServer(mqttServerAddress, int.Parse(mqttServerPort))
                .WithCredentials("user", "ringuser")
                .Build();
        }
    }

    public void handleError(string message)
    {
        MessageText = message;
        TextColor = "Red";
        IsOpenEnabled = false;
        IsReloadEnabled = true;
        IsLoading = false;
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
            Gates = gatesReturned.ToObservableCollection();
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

    async Task GetLogs()
    {
        string getLogsResponse;
        List<Log>? logsReturned;
        try
        {
            (getLogsResponse, logsReturned) = await _mainAPI.GetLogs(selectedGate.gateId);
            if (getLogsResponse != "success")
            {
                handleError("Error while retrieving logs.");
                return;
            }

            if (logsReturned != null && logsReturned.Count() > 0)
            {
                Logs = logsReturned.ToObservableCollection();
                NoLogs = false;
                HasLogs = true;
            }
            else
            {
                NoLogs = true;
                HasLogs = false;
            }
        }
        catch (Exception)
        {
            handleError("Error while retrieving logs.");
            return;
        }
    }


    [RelayCommand]
    private async Task  SelectGate(Gate selected)
    {
        HasLogs = false;
        NoLogs = false;

        selectedGate = selected;

        GateNameDisplay = selectedGate.name;
        IsLoading = false;
        IsReloadEnabled = true;
        IsOpenEnabled = true;
        GateSelected = true;
        NoGates = false;
        HasGates = true;


        await GetLogs();

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

        

        return;
    }

    [RelayCommand]
    private async Task AddUser()
    {
        IsAddEnabled = false;
        IsOpenEnabled = false;
        MessageText = string.Empty;

        var addVm = new AddViewModel(_addAPI, selectedGate.gateId, selectedGate.name);
        var addPage = new AddPage(addVm);
        await Shell.Current.Navigation.PushModalAsync(addPage);

        IsAddEnabled = true;
        IsOpenEnabled = true;
        IsReloadEnabled = true;
        return;
    }


    [RelayCommand]
    private async Task<bool> Connect()
    {
        IsOpenEnabled = false;
        IsReloadEnabled = false;
        MessageText = string.Empty;

        if (!mqttClient.IsConnected)
        {
            try
            {
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
                        IsReloadEnabled = true;
                        IsLoading = false;
                    }
                    else
                    {
                        handleError("Subscription failed");

                    }
                }
                else
                {
                    handleError("Connection failed");
                }

            }
            catch (Exception ex)
            {
                handleError(ex.Message);
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
        IsLoading = true;
        IsReloadEnabled = false;
        
        try
        {
            string getServerKeyResponse = await _registerAPI.GetServerKey();
            if (getServerKeyResponse != "success")
            {
                handleError(getServerKeyResponse);
                return;
            }
            string? phoneNumber = await SecureStorage.GetAsync("PhoneNumber");
            string? isVerified = await SecureStorage.GetAsync("IsVerified");
            if (string.IsNullOrEmpty(phoneNumber) || string.IsNullOrEmpty(isVerified) || string.Compare(isVerified, "n") == 0)
            {
                IsLoading = false;

                var registerVm = new RegisterViewModel(_registerAPI, _verificationAPI);
                var registerPage = new RegisterPage(registerVm);
                await Shell.Current.Navigation.PushModalAsync(registerPage);
                return;
            }
            else
            {
                var logoutResponse = await _mainAPI.CheckLogout();
                if (logoutResponse == "success")
                {
                    if (!GateSelected) await GetGates();
                    await Connect();
                    IsLoading = false;
                    IsReloadEnabled = true;
                    return;
                }
                else
                {
                    var registerVm = new RegisterViewModel(_registerAPI, _verificationAPI);
                    var registerPage = new RegisterPage(registerVm);
                    await Shell.Current.Navigation.PushModalAsync(registerPage);
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
    async Task Reload()
    {
        HasGates = false;
        NoGates = false;
        HasLogs = false;
        NoLogs = false;
        IsLoading = true;
        IsReloadEnabled = false;
        await GetGates();
        if (!mqttClient.IsConnected)
        {
            await Connect();
        }
        IsReloadEnabled = true;
        IsLoading = false;
        MessageText = string.Empty;
        return;
    }


    [RelayCommand]
    async Task Open()
    {
        bool connected = await Connect();
        if (!connected)
        {
            return;
        }
        MessageText = string.Empty;
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
                handleError("Location permission not granted!");
                return;
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
                    gate = selectedGate.gateId,
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
                        TextColor = "Green";
                        MessageText = "Message sent";
                        string generateLogResponse = await _mainAPI.GenerateLog(selectedGate.gateId);
                        if (generateLogResponse != "success")
                        {
                            handleError(generateLogResponse);
                        }
                    }
                    else
                    {
                        MessageText = "Message not sent";
                        TextColor = "Red";
                    }
                }
                catch (Exception ex)
                {
                    MessageText = ex.Message;
                }
                IsReloadEnabled = true;
                IsOpenEnabled = true;
                return;
            }
            else
            {
                handleError("Error while getting location.");
                return;
            }
        }
        catch (Exception ex)
        {
            handleError(ex.Message);
            return;
        }
    }

    public async Task ResetDefault()
    {
        await Disconnect();
        Gates = new List<Gate>().ToObservableCollection();
        selectedGate = new Gate(string.Empty, string.Empty);
        GateNameDisplay = string.Empty;
        GateSelected = false;
        NoGates = false;
        HasGates = false;
        NoLogs = false;
        HasLogs = false;
    }


    [RelayCommand]
    public async Task GoToSettings()
    {
        var settingsVm = new SettingsViewModel(_registerAPI, _verificationAPI, mqttClient);
        var settingsPage = new SettingsPage(settingsVm);
        await Shell.Current.Navigation.PushModalAsync(settingsPage);
        return;
    }
}