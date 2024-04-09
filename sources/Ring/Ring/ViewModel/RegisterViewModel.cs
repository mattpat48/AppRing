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

    // prefisso di nazionalità
    [ObservableProperty]
    List<string> phonePrefixes;
    [ObservableProperty]
    string selectedPhonePrefix;

    // messaggio di errore
    [ObservableProperty]
    string errorText;

    [ObservableProperty]
    bool rememberLogin;

    string deviceId;
    private RegisterAPI _registerAPI;
    private VerificationAPI _verificationAPI;

    // costruttore per il viewmodel
    public RegisterViewModel(RegisterAPI registerAPI, VerificationAPI verificationAPI)
    {
        PhoneNumber = string.Empty;
        PhonePrefixes = new List<string> { "+39", "+44", "+1", "+49", "+33", "+34", "+31", "+32", "+30", "+41", "+43", "+45", "+46", "+47", "+48"};
        SelectedPhonePrefix = "+39";
        ErrorText = string.Empty;
        rememberLogin = false;
        this.deviceId = string.Empty;
        _registerAPI = registerAPI;
        _verificationAPI = verificationAPI;
    }

    [RelayCommand]
    async Task Next()
    {
        // controllo se il numero di telefono è valido
        if (!string.IsNullOrEmpty(PhoneNumber))
        {
            PhoneNumber = PhoneNumber.Replace(" ", string.Empty);
            if (PhoneNumber.Length == 10)
            {
                string storingNumber = SelectedPhonePrefix + PhoneNumber;
                ErrorText = string.Empty;

                // genero la chiave pubblica e privata
                using RSA rsa = RSA.Create(2048);
                string publicKey = rsa.ExportRSAPublicKeyPem();
                string privateKey = rsa.ExportRSAPrivateKeyPem();

                // genero un id univoco per il dispositivo se non esiste 
                deviceId = await SecureStorage.GetAsync("DeviceId");
                if (string.IsNullOrEmpty(deviceId))
                {
                    deviceId = Guid.NewGuid().ToString();
                }

                await SecureStorage.SetAsync("PublicDeviceKey", publicKey);
                await SecureStorage.SetAsync("PrivateDeviceKey", privateKey);
                await SecureStorage.SetAsync("PhoneNumber", storingNumber);
                await SecureStorage.SetAsync("DeviceId", deviceId);
                await SecureStorage.SetAsync("IsVerified", "n");
                if (RememberLogin == true)
                    await SecureStorage.SetAsync("RememberLogin", "y");
                else
                    await SecureStorage.SetAsync("RememberLogin", "n");

                // richiedo la registrazione al server
                var signInResponse = await _registerAPI.SignInRequest();
                if (signInResponse == "success")
                {
                    // vado alla pagina di verifica
                    var phoneNumber = await SecureStorage.GetAsync("PhoneNumber");
                    var sendSMSResponse = await _verificationAPI.SendSMS(phoneNumber);
                    if (sendSMSResponse == "success")
                    {
                        var verificationVm = new VerificationViewModel(_registerAPI, _verificationAPI);
                        var verificationPage = new VerificationPage(verificationVm);
                        await Shell.Current.Navigation.PushModalAsync(verificationPage);
                    }
                    else
                    {
                        ErrorText = sendSMSResponse;
                        SecureStorage.Remove("ServerKey");
                        SecureStorage.Remove("PhoneNumber");
                        await SecureStorage.SetAsync("IsVerified", "n");
                        SecureStorage.Remove("RememberLogin");
                    }
                }
                else
                {
                    ErrorText = signInResponse;
                    SecureStorage.Remove("ServerKey");
                    SecureStorage.Remove("PhoneNumber");
                    await SecureStorage.SetAsync("IsVerified", "n");
                    SecureStorage.Remove("RememberLogin");
                }
            }
            else
            {
                ErrorText = "Invalid phone number";
            }
        }
    }
}
