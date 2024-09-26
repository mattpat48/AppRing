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
namespace Ring.ViewModel;

public partial class MapViewModel : ObservableObject
{

    public List<Pin> Pins;
    public string _filePath;

    private HttpClient _httpClient;
    private MainAPI _mainAPI;

    public MapViewModel()
    {
        Pins = new List<Pin>();
        _filePath = Path.Combine(FileSystem.AppDataDirectory, "gates.json");
    }

    public async void makeToast(string message)
    {

        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        string text = message;
        ToastDuration duration = ToastDuration.Short;
        double fontSize = 14;

        var toast = Toast.Make(text, duration, fontSize);
        await toast.Show(cancellationTokenSource.Token);
    }

    public List<Pin>? SetPins()
    {
        if (!File.Exists(_filePath))
        {
            return null;
        }

        string json = File.ReadAllText(_filePath);
        List<Gate>? gates = JsonConvert.DeserializeObject<List<Gate>>(json);

        if (gates == null)
        {
            makeToast("No gates found");
            return null;
        }

        Pins = new List<Pin>();
        foreach (Gate gate in gates)
        {
            Pins.Add(new Pin
            {
                Label = gate.name,
                Position = new Position(gate.latitude, gate.longitude)
            });
        }

        return Pins;
    }

    public void AssignHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _mainAPI = new MainAPI(_httpClient);
        return;
    }

    public void OnAppearing()
    {
        if (!NetworkInterface.GetIsNetworkAvailable())
        {
            makeToast("No internet connection");
        }
    }
}
