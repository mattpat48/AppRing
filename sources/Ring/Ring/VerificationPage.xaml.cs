using Microsoft.Maui.Controls;
using Ring.ViewModel;

namespace Ring;

public partial class VerificationPage : ContentPage
{
	public VerificationPage(VerificationViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
    }

    protected override async void OnAppearing()
	{
        base.OnAppearing();
        Opacity = 0;
        await this.FadeTo(1, 500);
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        Opacity = 1;
        await this.FadeTo(0, 500);
    }
}