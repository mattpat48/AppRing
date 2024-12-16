using MQTTnet;
using MQTTnet.Client;
using Ring.Services;
using Ring.Shared;
using Ring.ViewModel;

namespace Ring;

public partial class MainPage : ContentPage
{
    
    private readonly IMyHttpClient _myHttpClient;
    private readonly IMyMqttClient _myMqttClient;

    string? storedPhoneNumber;
    string? storedDeviceId;
    string? mqttServerAddress;
    string? mqttServerPort;

    public MainPage(MainViewModel viewModel, IMyHttpClient myHttpClient, IMyMqttClient myMqttClient)
    {
        InitializeComponent();
        // faccio binding del viewmodel con la pagina
        BindingContext = viewModel;
        _myHttpClient = myHttpClient;
        _myMqttClient = myMqttClient;

        HttpClientHandler clientHandler = new HttpClientHandler();
        clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
        _myHttpClient.sharedClient = new HttpClient(clientHandler);
        _myHttpClient.sharedMainAPI = new MainAPI(_myHttpClient.sharedClient);

        viewModel.AssignHttpClient(_myHttpClient.sharedClient, _myHttpClient.sharedMainAPI);

        storedPhoneNumber = SecureStorage.GetAsync("PhoneNumber").Result;
        storedDeviceId = SecureStorage.GetAsync("DeviceId").Result;
        mqttServerAddress = System.Environment.GetEnvironmentVariable("MQTT_SERVER");
        mqttServerPort = System.Environment.GetEnvironmentVariable("MQTT_PORT");

        var mqttFactory = new MQTTnet.MqttFactory();
        _myMqttClient.sharedMqttClient = mqttFactory.CreateMqttClient();

        if (!string.IsNullOrEmpty(storedPhoneNumber))
        {
            _myMqttClient.sharedOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(mqttServerAddress, int.Parse(mqttServerPort))
            .WithClientId(storedPhoneNumber + "_" + storedDeviceId)
            .WithCredentials("user", "ringuser")
            .Build();
        }

        viewModel.AssignMqttClient(_myMqttClient.sharedMqttClient, _myMqttClient.sharedOptions);

        Shell.SetTabBarIsVisible(this, true);
    }

    protected override async void OnAppearing()
    {
        var viewModel = BindingContext as MainViewModel;
        if (viewModel != null) {
            await viewModel.OnAppearing();
        }
        storedPhoneNumber = await SecureStorage.GetAsync("PhoneNumber");
        if (!string.IsNullOrEmpty(storedPhoneNumber) && _myMqttClient.sharedMqttClient != null)
        {
            _myMqttClient.sharedOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(mqttServerAddress, int.Parse(mqttServerPort))
                .WithClientId(storedPhoneNumber + "_" + storedDeviceId)
                .WithCredentials("user", "ringuser")
                .Build();
            viewModel.AssignMqttClient(_myMqttClient.sharedMqttClient, _myMqttClient.sharedOptions);
        }
    }

    protected override async void OnDisappearing()
    {
        var viewModel = BindingContext as MainViewModel;
        if (viewModel != null)
        {
            viewModel.resetShown();
        }
    }
}