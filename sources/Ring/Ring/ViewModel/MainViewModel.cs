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
    bool isRetryVisible;

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
    private MqttClientOptions options;

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
        IsRetryVisible = false;

        MessageText = string.Empty;
        TextColor = "White";

        GateIdDisplay = string.Empty;
        GateNameDisplay = string.Empty;

        GateSelected = false;
        GateNotSelected = false;

        // inizializzo il client e server mqtt
        mqttServer = new MqttFactory();
        mqttClient = mqttServer.CreateMqttClient();

        string mqttServerAddress = Environment.GetEnvironmentVariable("MQTT_SERVER");
        string mqttServerPort = Environment.GetEnvironmentVariable("MQTT_PORT");

        // inizializzo le opzioni del server mqtt
        options = new MqttClientOptionsBuilder()
            // ATTENZIONE: inserire l'indirizzo IP del server MQTT
            .WithTcpServer(mqttServerAddress, int.Parse(mqttServerPort))
            .WithCredentials("user", "ringuser")
            .Build();

        _httpClient = new HttpClient();
        _mainAPI = new MainAPI(_httpClient);
        _registerAPI = new RegisterAPI(_httpClient);
        _verificationAPI = new VerificationAPI(_httpClient);

        Gates = new ObservableCollection<Gate>();
        selectedGate = new Gate(string.Empty, string.Empty);
    }

    public void handleError(string message)
    {
        MessageText = message;
        TextColor = "Red";
        IsOpenEnabled = false;
        IsRetryVisible = true;
        IsContentVisible = true;
        IsLoading = false;
        return;
    }

    async Task GetGates()
    {
        string getGatesResponse;
        List<Gate> gatesReturned;
        try
        {
            (getGatesResponse, gatesReturned) = await _mainAPI.GetAllGates();
            if (getGatesResponse != "success")
            {
                handleError(getGatesResponse);
                return;
            }
            else if (gatesReturned != null && gatesReturned.Count() == 0)
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
                IsRetryVisible = false;
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
    private async Task<bool> Connect()
    {
        IsOpenEnabled = false;
        IsRetryVisible = false;
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
                        IsRetryVisible = false;
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
        await mqttClient.DisconnectAsync();
    }

    [RelayCommand]
    public async Task Check()
    {
        IsLoading = true;
        
        try
        {
            string getServerKeyResponse = await _registerAPI.GetServerKey();
            if (getServerKeyResponse != "success")
            {
                handleError("Error getting server key.");
                return;
            }
            string phoneNumber = await SecureStorage.GetAsync("PhoneNumber");
            string isVerified = await SecureStorage.GetAsync("IsVerified");
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
                    await GetGates();
                    await Connect();
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
    async Task Open()
    {

        bool connected = await Connect();
        if (!connected)
        {
            return;
        }
        MessageText = string.Empty;

        var toSend = new
        {
            id = await SecureStorage.GetAsync("DeviceId"),
            datetime = DateTime.Now,
            lat = "Lat",
            language = "language",
            key = await SecureStorage.GetAsync("PublicDeviceKey"),
            phone = await SecureStorage.GetAsync("PhoneNumber"),
            gate = selectedGate.gateId
        };

        IsOpenEnabled = false;

        var message = new MqttApplicationMessageBuilder()
            .WithTopic("ringRequest/request")
            .WithPayload(toSend.ToString())
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
    }

    [RelayCommand]
    async Task RemoveNumber ()
    {
        SecureStorage.Remove("PhoneNumber");
        await mqttClient.DisconnectAsync();
        var registerVm = new RegisterViewModel(_registerAPI, _verificationAPI);
        var registerPage = new RegisterPage(registerVm);
        await Shell.Current.Navigation.PushModalAsync(registerPage);
    }

}