using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Devices;
using System.Net.Http;
using System.Security.Cryptography;
using Newtonsoft.Json;
using System.Text;
using System.Diagnostics;
using Ring.Services;

namespace Ring.ViewModel;

public partial class RegisterViewModel : ObservableObject
{
    // numero di telefono
    [ObservableProperty]
    string phoneNumber;

    // messaggio di errore
    [ObservableProperty]
    string errorText;

    // risposta del server
    [ObservableProperty]
    string response;

    string deviceId;

    private RegisterAPI _registerAPI;

    // costruttore per il viewmodel
    public RegisterViewModel()
    {
        PhoneNumber = string.Empty;
        ErrorText = string.Empty;
        Response = string.Empty;
        this.deviceId = string.Empty;
        _registerAPI = new RegisterAPI();
    }

    [RelayCommand]
    async Task Next()
    {
        // controllo se il numero di telefono è valido
        if (!string.IsNullOrEmpty(PhoneNumber))
        {
            if (PhoneNumber.Length == 10)
            {
                ErrorText = string.Empty;

                // genero la chiave pubblica e privata
                var rsa = RSA.Create(2048);
                var publicKey = rsa.ExportRSAPublicKeyPem();
                var privateKey = rsa.ExportRSAPrivateKeyPem();

                // salvo la chiave pubblica e privata
                await SecureStorage.SetAsync("PublicDeviceKey", publicKey);
                await SecureStorage.SetAsync("PrivateDeviceKey", privateKey);
                await SecureStorage.SetAsync("PhoneNumber", PhoneNumber);

                // genero un id univoco per il dispositivo se non esiste 
                deviceId = await SecureStorage.GetAsync("DeviceId");
                if (string.IsNullOrEmpty(deviceId))
                {
                    deviceId = Guid.NewGuid().ToString();
                    await SecureStorage.SetAsync("DeviceId", deviceId);
                }

                // richiedo la registrazione al server
                var signInResponse = await _registerAPI.SignInRequest();
                if (signInResponse == "success")
                {
                    // vado alla pagina di verifica
                    var verificationVm = new VerificationViewModel();
                    var verificationPage = new VerificationPage(verificationVm);
                    await Shell.Current.Navigation.PushModalAsync(verificationPage);
                }
                else
                {
                    ErrorText = signInResponse;
                }
            }
            else
            {
                ErrorText = "Invalid phone number";
            }
        }
    }
}
