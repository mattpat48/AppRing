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

    // costruttore per il viewmodel
    public MainViewModel()
    {
        IsLoading = true;
        IsContentVisible = false;

        IsOpenEnabled = false;
        IsRetryVisible = false;

        MessageText = string.Empty;
        TextColor = "White";

        // inizializzo il client e server mqtt
        mqttServer = new MqttFactory();
        mqttClient = mqttServer.CreateMqttClient();

        // inizializzo le opzioni del server mqtt
        options = new MqttClientOptionsBuilder()
            // ATTENZIONE: inserire l'indirizzo IP del server MQTT
            .WithTcpServer("127.0.0.1", 1883)
            //.WithCredentials("username", "password")
            .Build();

        _httpClient = new HttpClient();
        _registerAPI = new RegisterAPI(_httpClient);
        _verificationAPI = new VerificationAPI(_httpClient);
    }

    [RelayCommand]
    public async Task Connect()
    {
        IsLoading = true;
        IsContentVisible = false;
        MessageText = string.Empty;

        try
        {
            await mqttClient.ConnectAsync(options);
            await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("ringRequest/request").Build());
            IsOpenEnabled = true;
            IsRetryVisible = false;
        }
        catch (Exception ex)
        {
            MessageText = ex.Message;
            TextColor = "Red";
            IsRetryVisible = true;
        }

        IsLoading = false;
        IsContentVisible = true;
    }

    [RelayCommand]
    public async Task Check()
    {
        IsLoading = true;

        var phoneNumber = await SecureStorage.GetAsync("PhoneNumber");
        var isVerified = await SecureStorage.GetAsync("IsVerified");
        if (string.IsNullOrEmpty(phoneNumber) || string.IsNullOrEmpty(isVerified))
        {
            IsContentVisible = false;
            IsLoading = false;

            var registerVm = new RegisterViewModel(_registerAPI, _verificationAPI);
            var registerPage = new RegisterPage(registerVm);
            await Shell.Current.Navigation.PushModalAsync(registerPage);
        }
        else
        {
            await Connect();
        }
    }

    [RelayCommand]
    async Task Open()
    {
        MessageText = string.Empty;

        var toSend = new
        {
            id = await SecureStorage.GetAsync("DeviceId"),
            datetime = DateTime.Now,
            lat = "Lat",
            language = "language",
            key = SecureStorage.GetAsync("PublicKey"),
            phone = await SecureStorage.GetAsync("PhoneNumber")
        };

        IsOpenEnabled = false;

        var message = new MqttApplicationMessageBuilder()
            .WithTopic("ringRequest/request")
            .WithPayload(toSend.ToString())
            .Build();
        try
        {
            await mqttClient.PublishAsync(message);
            TextColor = "Green";
            MessageText = "Message sent";
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