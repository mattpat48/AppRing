using Ring.ViewModel;

namespace Ring;

public partial class VerificationPage : ContentPage
{
	public VerificationPage(VerificationViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        var viewModel = BindingContext as VerificationViewModel;
        if (viewModel != null)
        {
            viewModel.OnAppearing();
        }
    }
}