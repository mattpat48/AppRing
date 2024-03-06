using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;

namespace Ring.ViewModel;

public partial class RegisterViewModel : ObservableObject
{
    // numero di telefono
    [ObservableProperty]
    string phoneNumber;

    // costruttore per il viewmodel
    public RegisterViewModel()
    {
        PhoneNumber = string.Empty;
    }

    [RelayCommand]
    async Task Register()
    {
        if (!string.IsNullOrEmpty(PhoneNumber))
        {
            await SecureStorage.SetAsync("PhoneNumber", PhoneNumber);
            await Shell.Current.GoToAsync("//MainPage");
        }
    }
}
