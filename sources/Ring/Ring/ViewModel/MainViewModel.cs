using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MQTTnet;
using MQTTnet.Client;
using System.Text.Json;

namespace Ring.ViewModel;

public partial class MainViewModel : ObservableObject
{

    // proprietà osservabili del bottone
    [ObservableProperty]
    string buttonText;
    [ObservableProperty]
    bool buttonIsEnabled;

    // proprietà osservabile del testo di errore
    [ObservableProperty]
    string connectionErrorText;
    [ObservableProperty]
    string messageErrorText;

    // proprietà osservabile del messaggio
    [ObservableProperty]
    string messageText;


    private MqttFactory mqttServer;
    private IMqttClient mqttClient;
    private MqttClientOptions options;

    public async Task Connect()
    {
        ConnectionErrorText = "";
        try
        {
            await mqttClient.ConnectAsync(options, CancellationToken.None);
            await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("ringRequest/request").Build());
        }
        catch (Exception ex)
        {
            ConnectionErrorText = ex.Message;
        }
    }

    // costruttore per il viewmodel
    public MainViewModel()
    {
        ButtonText = "Open";
        ButtonIsEnabled = true;

        ConnectionErrorText = "";
        MessageErrorText = "";
        MessageText = "";
        
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
    async Task Open()
    {
        MessageErrorText = "";

        var toSend = new
        {
            id = "IdDevice",
            datetime = "DateTime",
            lat = "Lat",
            language = "Lng",
            key = "Key",
        };

        var message = new MqttApplicationMessageBuilder()
            .WithTopic("ringRequest/request")
            .WithPayload(toSend.ToString())
            .Build();
        try
        {
            await mqttClient.PublishAsync(message);
        }
        catch (Exception ex)
        {
            MessageErrorText = ex.Message;
        }
    }
}