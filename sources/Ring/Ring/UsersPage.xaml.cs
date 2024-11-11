using Ring.ViewModel;

namespace Ring;

public partial class UsersPage : ContentPage
{
	public UsersPage(UsersViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	protected override async void OnAppearing()
	{
        var viewModel = BindingContext as UsersViewModel;
        if (viewModel != null)
		{
            await viewModel.OnAppearing();
        }
    }
}