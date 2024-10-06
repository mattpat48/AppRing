using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using Mapsui.Projections;
using Mapsui;
using Mapsui.UI.Maui;
using Newtonsoft.Json;
using Ring.Utils;
using System.Net.NetworkInformation;
using Ring.Services;
using CommunityToolkit.Mvvm.Input;

namespace Ring.ViewModel;

public partial class MapViewModel : ObservableObject
{

    public List<Pin> Pins;
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

    public MapViewModel(MainViewModel mainViewModel)
    {
        Pins = new List<Pin>();
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
        List<Gate>? gates = JsonConvert.DeserializeObject<List<Gate>>(json);

        if (gates == null)
        {
            NotificationTools.makeToast("No gates found");
            return null;
        }

        Pins = new List<Pin>();
        foreach (Gate gate in gates)
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

    public void AssignHttpClient(HttpClient httpClient)
    {
        //_httpClient = httpClient;
        return;
    }

    public void OnGateSelected(string name, string id)
    {
        GateSelected = true;
        GateNotSelected = false;

        SelectedGateName = name;
        SelectedGateId = id;

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
    async Task OpenGate()
    {
        IsOpenEnabled = false;
        if (SelectedGateId == string.Empty)
        {
            NotificationTools.makeToast("Please select a gate");
            IsOpenEnabled = true;
            return;
        }

        string response = await _mainViewModel.Open(SelectedGateId);
        if (response == "success")
        {
            NotificationTools.makeToast("Message sent");
            IsOpenEnabled = true;
            return;
        }

        NotificationTools.makeToast(response);
        IsOpenEnabled = true;
        return;
    }

    public void OnAppearing()
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
    }
}
