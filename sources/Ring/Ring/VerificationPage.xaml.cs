using Ring.ViewModel;

namespace Ring;

public partial class VerificationPage : ContentPage
{
	public VerificationPage(VerificationViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
    }
}