using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;
using System.Security.Cryptography;

namespace Ring.ViewModel;

public partial class RegisterViewModel : ObservableObject
{
    // numero di telefono
    [ObservableProperty]
    string phoneNumber;

    // messaggio di errore
    [ObservableProperty]
    string errorText;

    // costruttore per il viewmodel
    public RegisterViewModel()
    {
        ErrorText = string.Empty;
        PhoneNumber = string.Empty;
    }

    [RelayCommand]
    async Task Register()
    {
        if (!string.IsNullOrEmpty(PhoneNumber))
        {
            if (PhoneNumber.Length == 10)
            {
                ErrorText = string.Empty;

                var rsa = RSA.Create(2048);
                var publicKey = rsa.ExportSubjectPublicKeyInfo();
                var privateKey = rsa.ExportRSAPrivateKey();

                await SecureStorage.SetAsync("PrivateKey", Convert.ToBase64String(privateKey));
                await SecureStorage.SetAsync("PublicKey", Convert.ToBase64String(publicKey));
                await SecureStorage.SetAsync("PhoneNumber", PhoneNumber);

                await Shell.Current.GoToAsync("//MainPage");
            }
            else
            {
                ErrorText = "Numero di telefono non valido";
            }
        }
    }
}
