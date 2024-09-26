using Ring.ViewModel;

namespace Ring;

public partial class AddPage : ContentPage
{
	public AddPage(AddViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

    protected override void OnAppearing()
    {
        var viewModel = BindingContext as AddViewModel;
        if (viewModel != null)
        {
            viewModel.OnAppearing();
        }
    }
}