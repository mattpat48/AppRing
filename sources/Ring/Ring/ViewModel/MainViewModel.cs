using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MQTTnet;
using MQTTnet.Client;
using System.Text.Json;
using Microsoft.Maui.Storage;
using System.Runtime.InteropServices;

namespace Ring.ViewModel;

public partial class MainViewModel : ObservableObject
{

    // proprietà osservabile del caricamento
    [ObservableProperty]
    bool isLoading;
    [ObservableProperty]
    bool isContentVisible;

    [ObservableProperty]
    bool isOpenEnabled;
    [ObservableProperty]
    bool isRetryVisible;

    // proprietà osservabile del messaggio
    [ObservableProperty]
    string messageText;
    [ObservableProperty]
    string textColor;


    private MqttFactory mqttServer;
    private IMqttClient mqttClient;
    private MqttClientOptions options;

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
            .WithTcpServer("10.20.100.50", 1883)
            //.WithCredentials("username", "password")
            .Build();
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
        if (string.IsNullOrEmpty(phoneNumber))
        {
            IsContentVisible = false;
            IsLoading = false;

            var registerVm = new RegisterViewModel();
            var registerPage = new RegisterPage(registerVm);
            await Shell.Current.Navigation.PushModalAsync(registerPage);
        }
        else { IsContentVisible = true; }
        IsLoading = false;
    }

    [RelayCommand]
    async Task Open()
    {
        MessageText = string.Empty;

        var toSend = new
        {
            id = "IdDevice",
            datetime = "DateTime",
            lat = "Lat",
            language = "Lng",
            key = "Key",
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
            TextColor = "Red";
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