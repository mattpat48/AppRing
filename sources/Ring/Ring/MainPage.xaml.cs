using Ring.Services;
using Ring.Utils;
using Ring.ViewModel;

namespace Ring;

public partial class MainPage : ContentPage
{
    
    private readonly IMyHttpClient _myHttpClient;
    public MainPage(MainViewModel viewModel, IMyHttpClient myHttpClient)
    {
        InitializeComponent();
        // faccio binding del viewmodel con la pagina
        BindingContext = viewModel;
        _myHttpClient = myHttpClient;

        HttpClientHandler clientHandler = new HttpClientHandler();
        clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
        _myHttpClient.sharedClient = new HttpClient(clientHandler);
        _myHttpClient.sharedMainAPI = new MainAPI(_myHttpClient.sharedClient);

        viewModel.AssignHttpClient(_myHttpClient.sharedClient, _myHttpClient.sharedMainAPI);
        Shell.SetTabBarIsVisible(this, true);
    }

    protected override async void OnAppearing()
    {
        var viewModel = BindingContext as MainViewModel;
        if (viewModel != null) {
            await viewModel.OnAppearing();
        }
    }

}