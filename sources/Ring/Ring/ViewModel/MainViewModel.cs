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

    [ObservableProperty]
    bool buttonIsEnabled;

    // proprietà osservabile del testo di errore
    [ObservableProperty]
    string connectionErrorText;

    // proprietà osservabile del messaggio
    [ObservableProperty]
    string messageText;


    private MqttFactory mqttServer;
    private IMqttClient mqttClient;
    private MqttClientOptions options;

    // costruttore per il viewmodel
    public MainViewModel()
    {
        ButtonIsEnabled = false;

        ConnectionErrorText = string.Empty;
        MessageText = string.Empty;
        MessageText = string.Empty;

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
        ConnectionErrorText = string.Empty;
        try
        {
            await mqttClient.ConnectAsync(options, CancellationToken.None);
            await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("ringRequest/request").Build());
            ButtonIsEnabled = true;
        }
        catch (Exception ex)
        {
            ConnectionErrorText = ex.Message;
        }
    }

    [RelayCommand]
    public async Task Back()
    {
        var phoneNumber = await SecureStorage.GetAsync("PhoneNumber");
        if (string.IsNullOrEmpty(phoneNumber)) { await Shell.Current.GoToAsync(nameof(RegisterPage)); }
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
        };

        ButtonIsEnabled = false;

        var message = new MqttApplicationMessageBuilder()
            .WithTopic("ringRequest/request")
            .WithPayload(toSend.ToString())
            .Build();
        try
        {
            await mqttClient.PublishAsync(message);
            MessageText = "Message sent";
        }
        catch (Exception ex)
        {
            MessageText = ex.Message;
        }

        ButtonIsEnabled = true;
    }

    [RelayCommand]
    async Task RemoveNumber ()
    {
        SecureStorage.Remove("PhoneNumber");
        await Back();
    }

}