using Ring.ViewModel;

namespace Ring
{
    public partial class MainPage : ContentPage
    {

        public MainPage(MainViewModel viewModel)
        {
            InitializeComponent();
            // faccio binding del viewmodel con la pagina
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            var viewModel = BindingContext as MainViewModel;
            if (viewModel != null) {
                await viewModel.Connect(); 
                await viewModel.Back();
            }
            
        }
    }
}