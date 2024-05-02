using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MQTTnet;
using MQTTnet.Client;
using Ring.Services;
using Ring.Utils;
using Newtonsoft.Json;
using System.Text;
using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Core.Extensions;
using System.Globalization;

namespace Ring.ViewModel;

public partial class MainViewModel : ObservableObject
{

    // caricamento si o no
    [ObservableProperty]
    bool isLoading;
    // visibilità del contenuto della pagina
    [ObservableProperty]
    bool isContentVisible;

    [ObservableProperty]
    bool isOpenEnabled;
    [ObservableProperty]
    bool isReloadEnabled;

    // messaggio di errore
    [ObservableProperty]
    string messageText;
    [ObservableProperty]
    string textColor;

    [ObservableProperty]
    string gateIdDisplay;
    [ObservableProperty]
    string gateNameDisplay;

    [ObservableProperty]
    bool gateSelected;
    [ObservableProperty]
    bool gateNotSelected;

    // server, client e opzioni per scambio mqtt
    private MqttFactory mqttServer;
    private IMqttClient mqttClient;
    private MqttClientOptions? options;

    private HttpClient _httpClient;
    private MainAPI _mainAPI;
    private RegisterAPI _registerAPI;
    private VerificationAPI _verificationAPI;

    [ObservableProperty]
    ObservableCollection<Gate> gates;
    Gate selectedGate;

    

    // costruttore per il viewmodel
    public MainViewModel()
    {
        IsLoading = false;
        IsContentVisible = false;

        IsOpenEnabled = true;
        IsReloadEnabled = false;

        MessageText = string.Empty;
        TextColor = "White";

        GateIdDisplay = string.Empty;
        GateNameDisplay = string.Empty;

        GateSelected = false;
        GateNotSelected = false;

        // inizializzo il client e server mqtt
        mqttServer = new MqttFactory();
        mqttClient = mqttServer.CreateMqttClient();

        string? mqttServerAddress = Environment.GetEnvironmentVariable("MQTT_SERVER");
        string? mqttServerPort = Environment.GetEnvironmentVariable("MQTT_PORT");

        _httpClient = new HttpClient();
        _mainAPI = new MainAPI(_httpClient);
        _registerAPI = new RegisterAPI(_httpClient);
        _verificationAPI = new VerificationAPI(_httpClient);

        Gates = [];
        selectedGate = new Gate(string.Empty, string.Empty);

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
        IsContentVisible = true;
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
                handleError(getGatesResponse);
                return;
            }
            else if (gatesReturned == null || gatesReturned.Count() == 0)
            {
                handleError("No gates to retrieve.");
                return;
            }
            Gates = gatesReturned.ToObservableCollection();
            if (Gates.Count() == 1)
            {
                selectedGate = Gates.First();
                GateIdDisplay = "Gate ID: " + selectedGate.gateId;
                GateNameDisplay = "Gate name: " + selectedGate.name;
                IsLoading = false;
                IsContentVisible = true;
                IsReloadEnabled = true;
                IsOpenEnabled = true;
                GateSelected = true;
                GateNotSelected = false;
                return;
            }
            else
            {
                IsLoading = false;
                IsContentVisible = true;
                GateSelected = false;
                GateNotSelected = true;
                return;
            }
        }
        catch (Exception ex)
        {
            handleError(ex.Message);
            return;
        }
    }

    [RelayCommand]
    private void SelectGate(Gate selected)
    {
        selectedGate = selected;
        GateIdDisplay = "Gate ID: " + selected.gateId;
        GateNameDisplay = "Gate Name: " + selected.name;
        GateSelected = true;
        GateNotSelected = false;
        return;
    }

    [RelayCommand]
    private void DeselectGate()
    {
        selectedGate = new Gate(string.Empty, string.Empty);
        GateIdDisplay = string.Empty;
        GateNameDisplay = string.Empty;
        GateSelected = false;
        GateNotSelected = true;
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
                        IsReloadEnabled = false;
                        IsContentVisible = true;
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
                handleError("Error getting server key.");
                return;
            }
            string? phoneNumber = await SecureStorage.GetAsync("PhoneNumber");
            string? isVerified = await SecureStorage.GetAsync("IsVerified");
            if (string.IsNullOrEmpty(phoneNumber) || string.IsNullOrEmpty(isVerified) || string.Compare(isVerified, "n") == 0)
            {
                IsLoading = false;
                IsContentVisible = false;

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
                    IsContentVisible = true;
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
        IsLoading = true;
        IsReloadEnabled = false;
        await GetGates();
        if (!mqttClient.IsConnected)
        {
            await Connect();
        }
        IsReloadEnabled = true;
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
            // TEMP UNAVAIBLE
            /*
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
            */
            location = new Location(45.4642, 9.1900);

            if (location != null)
            {
                var package = new
                {
                    id = await SecureStorage.GetAsync("DeviceId"),
                    datetime = DateTime.Now,
                    lat = location.Latitude + "," + location.Longitude,
                    language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                    gate = selectedGate.gateId,
                };

                IsOpenEnabled = false;

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

                // TEMP UNAVAIBLE
                /*
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

    [RelayCommand]
    async Task RemoveNumber ()
    {
        try
        {
            SecureStorage.Remove("PhoneNumber");
            await mqttClient.DisconnectAsync();
            var registerVm = new RegisterViewModel(_registerAPI, _verificationAPI);
            var registerPage = new RegisterPage(registerVm);
            await Shell.Current.Navigation.PushModalAsync(registerPage);
        }
        catch (Exception ex)
        {
            handleError(ex.Message);
        }
    }

}