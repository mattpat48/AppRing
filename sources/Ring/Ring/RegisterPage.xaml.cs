using Microsoft.Maui.Controls;
using Ring.ViewModel;

namespace Ring;

public partial class RegisterPage : ContentPage
{
	public RegisterPage(RegisterViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        var viewModel = BindingContext as RegisterViewModel;
        if (viewModel != null)
        {
            viewModel.OnAppearing();
        }
    }
}