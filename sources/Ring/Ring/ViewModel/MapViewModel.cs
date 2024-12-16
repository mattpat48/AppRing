using CommunityToolkit.Mvvm.ComponentModel;
using Mapsui.UI.Maui;
using Newtonsoft.Json;
using Ring.Utils;
using System.Net.NetworkInformation;
using Ring.Services;
using CommunityToolkit.Mvvm.Input;
using Ring.Shared;
using MQTTnet.Client;

namespace Ring.ViewModel;

public partial class MapViewModel : ObservableObject
{

    public List<Pin> Pins;
    public List<Gate> Gates;
    public string _filePath;
    private MainViewModel _mainViewModel;

    [ObservableProperty]
    public bool gateSelected;
    [ObservableProperty]
    public bool gateNotSelected;
    [ObservableProperty]
    bool isOpenEnabled;

    [ObservableProperty]
    public string selectedGateName;
    [ObservableProperty]
    public string selectedGateId;
    [ObservableProperty]
    public string selectedGateAddress;

    private HttpClient _httpClient;
    private MainAPI _mainAPI;

    private IMqttClient _mqttClient;
    private MqttClientOptions _options;

    private const double EarthRadiusMeters = 6371000;
    private const double RadiusToOpenGate = 10;

    public MapViewModel(MainViewModel mainViewModel)
    {
        Pins = new List<Pin>();
        Gates = new List<Gate>();
        _filePath = Path.Combine(FileSystem.AppDataDirectory, "gates.json");
        _mainViewModel = mainViewModel;

        GateSelected = false;
        GateNotSelected = true;
        IsOpenEnabled = false;

        SelectedGateName = string.Empty;
        SelectedGateId = string.Empty;
    }

    public async Task<List<Pin>?> SetPins()
    {
        if (!File.Exists(_filePath))
        {
            return null;
        }

        string json = File.ReadAllText(_filePath);
        Gates = JsonConvert.DeserializeObject<List<Gate>>(json);

        if (Gates == null || Gates.Count() == 0)
        {
            NotificationTools.makeToast("No gates found");
            return null;
        }

        Pins = new List<Pin>();
        foreach (Gate gate in Gates)
        {
            Pins.Add(new Pin
            {
                Label = gate.name,
                Tag = gate.gateId,
                Position = new Position(gate.latitude, gate.longitude),
                Address = gate.address
            });
        }

        return Pins;
    }

    internal void AssignHttpClient(HttpClient sharedClient, MainAPI sharedMainAPI)
    {
        _httpClient = sharedClient;
        _mainAPI = sharedMainAPI;
    }

    public void AssignMqttClient(IMqttClient sharedMqttClient, MqttClientOptions sharedOptions)
    {
        _mqttClient = sharedMqttClient;
        _options = sharedOptions;
    }

    public void OnGateSelected(string name, string id, string address)
    {
        
        GateSelected = true;
        GateNotSelected = false;

        SelectedGateName = name;
        SelectedGateId = id;
        SelectedGateAddress = address;

        return;
    }

    public void OnGateNotSelected()
    {
        GateSelected = false;
        GateNotSelected = true;

        SelectedGateName = string.Empty;
        SelectedGateId = string.Empty;

        return;
    }

    [RelayCommand]
    async Task OpenGate(Gate? autoGate = null)
    {
        string toOpenGateId;
        string toOpenGateName;
        if (autoGate == null)
        {
            toOpenGateId = SelectedGateId;
            toOpenGateName = SelectedGateName;
        }
        else
        {
            toOpenGateId = autoGate.gateId;
            toOpenGateName = autoGate.name;
        }
        IsOpenEnabled = false;

        if (autoGate != null)
        {
            NotificationTools.makeToast("This gate is not set to auto-open");
            IsOpenEnabled = true;
            return;
        }

        string response = await MqttManager.Open(_mqttClient, _options, toOpenGateId);
        if (response != "success")
        {
            NotificationTools.makeToast(response);
            IsOpenEnabled = true;
            return;
        }
        else
        {
            string logResponse = await MqttManager.LogPostOpen(_mainAPI, toOpenGateId, toOpenGateName);
            if (logResponse != "success")
            {
                NotificationTools.makeToast(logResponse);
                IsOpenEnabled = true;
                return;
            }
            string json = File.ReadAllText(_filePath);
            Gates = JsonConvert.DeserializeObject<List<Gate>>(json);
            IsOpenEnabled = true;
            return;
        }
    }

    public async Task OnLocationChanged(double lat, double lon)
    {
        foreach (var gate in Gates)
        {
            double distance = CalculateDistance(lat, lon, gate.latitude, gate.longitude);
            if (distance <= 15)
            {
                string autoOpenResponse;
                string autoOpen;
                (autoOpenResponse, autoOpen) = await _mainAPI.GetAutoOpen(gate.gateId);
                if (autoOpenResponse != "success")
                {
                    NotificationTools.makeToast(autoOpenResponse);
                    return;
                }
                if (autoOpen == "y")
                {
                    await OpenGate(autoGate: gate);
                }
            }
        }
    }

    private double CalculateDistance(double userLat, double userLon, double gateLat, double gateLon)
    {
        var sCoord = new Location(userLat, userLon);
        var eCoord = new Location(gateLat, gateLon);

        return sCoord.CalculateDistance(eCoord, DistanceUnits.Kilometers) * 1000;
    }

    

    public async Task OnAppearing()
    {
        try
        {
            GateSelected = false;
            GateNotSelected = true;
            IsOpenEnabled = false;
            SelectedGateName = string.Empty;
            SelectedGateId = string.Empty;
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                NotificationTools.makeToast("No internet connection");
            }
            var location = await Geolocation.GetLocationAsync(new GeolocationRequest
            {
                DesiredAccuracy = GeolocationAccuracy.Medium,
                Timeout = TimeSpan.FromSeconds(10)
            });
        }
        catch (Exception ex)
        {
            NotificationTools.makeToast(ex.Message);
        }
    }
}
