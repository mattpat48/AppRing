using Ring.ViewModel;

namespace Ring;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    public async void RemoveNumber(object sender, EventArgs e)
    {
        var viewModel = BindingContext as SettingsViewModel;
        if (viewModel != null)
        {
            Shell.SetTabBarIsVisible(this, false);
            await viewModel.RemoveNumber();
        }
    }
}