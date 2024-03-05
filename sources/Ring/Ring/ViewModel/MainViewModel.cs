using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Ring.ViewModel;

public partial class MainViewModel : ObservableObject
{

    public MainViewModel()
    {
        Text = "Open";
        IsEnabled = true;
    }

    [ObservableProperty]
    string text;

    [ObservableProperty]
    bool isEnabled;

    [RelayCommand]
    void Open()
    {
        Text = "Opening";
        IsEnabled = false;
    }
}
