using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MQTTnet;
using MQTTnet.Client;
using System.Text.Json;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Devices;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Ring.Services;
using Newtonsoft.Json;
using System.Text;

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

    // server, client e opzioni per scambio mqtt
    private MqttFactory mqttServer;
    private IMqttClient mqttClient;
    private MqttClientOptions options;

    private HttpClient _httpClient;
    private RegisterAPI _registerAPI;
    private VerificationAPI _verificationAPI;

    private readonly string _server;

    // costruttore per il viewmodel
    public MainViewModel()
    {
        IsLoading = false;
        IsContentVisible = false;

        IsOpenEnabled = true;
        IsRetryVisible = false;

        MessageText = string.Empty;
        TextColor = "White";

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
        _registerAPI = new RegisterAPI(_httpClient);
        _verificationAPI = new VerificationAPI(_httpClient);

        _server = Environment.GetEnvironmentVariable("API_SERVER");
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
                        MessageText = "Subscribe failed";
                        TextColor = "Red";
                        IsRetryVisible = true;
                        IsContentVisible = true;
                        IsLoading = false;
                    }
                }
                else
                {
                    MessageText = "Connection failed";
                    TextColor = "Red";
                    IsRetryVisible = true;
                    IsContentVisible = true;
                    IsLoading = false;
                }

            }
            catch (Exception ex)
            {
                MessageText = ex.Message;
                TextColor = "Red";
                IsRetryVisible = true;
                IsContentVisible = true;
                IsLoading = false;
            }
        }
        return mqttClient.IsConnected;
    }

    public async Task Disconnect()
    {
        await mqttClient.DisconnectAsync();
    }

    public async Task<string> CheckLogout()
    {
        var url = _server + "/api/v1/auth/checklogout";

        var toSend = new
        {
            Number = await SecureStorage.GetAsync("PhoneNumber"),
            Id = await SecureStorage.GetAsync("DeviceId")
        };

        string jsonRequestBody = JsonConvert.SerializeObject(toSend);
        var stringContent = new StringContent(jsonRequestBody, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await _httpClient.PostAsync(url, stringContent);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return responseContent;
        }
        else
        {
            return "success";
        }
    }

    [RelayCommand]
    public async Task Check()
    {

        IsLoading = true;

        string phoneNumber = await SecureStorage.GetAsync("PhoneNumber");
        string isVerified = await SecureStorage.GetAsync("IsVerified");
        var logoutResponse = await CheckLogout();
        if (string.IsNullOrEmpty(phoneNumber) || string.IsNullOrEmpty(isVerified) || string.Compare(isVerified, "n") == 0 || string.Compare(logoutResponse, "success") != 0)
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
            if (logoutResponse == "success")
            {
                await Connect();
            }
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
            phone = await SecureStorage.GetAsync("PhoneNumber")
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
        await Check();
    }

}