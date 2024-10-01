using Ring.Utils;
using Ring.ViewModel;

namespace Ring;

public partial class SettingsPage : ContentPage
{
    private readonly IMyHttpClient _myHttpClient;
    public SettingsPage(SettingsViewModel viewModel, IMyHttpClient myHttpClient)
    {
        InitializeComponent();
        BindingContext = viewModel;

        _myHttpClient = myHttpClient;
        viewModel.AssignHttpClient(_myHttpClient.sharedClient);
    }
}