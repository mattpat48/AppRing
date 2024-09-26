using Ring.ViewModel;

namespace Ring;

public partial class MultiplePage : ContentPage
{
	public MultiplePage(MultipleViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	protected override async void OnAppearing()
	{
        var viewModel = BindingContext as MultipleViewModel;
        if (viewModel != null)
		{
            await viewModel.OnAppearing();
        }
    }
}