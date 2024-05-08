using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Security.Cryptography;
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

    [ObservableProperty]
    bool isNextEnabled;
    [ObservableProperty]
    string nextText;

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
        isNextEnabled = true;
        NextText = "Next";
    }

    public void OnAppearing()
    {
        ErrorText = string.Empty;
        SecureStorage.Remove("ServerKey");
        SecureStorage.Remove("PhoneNumber");
        SecureStorage.Remove("IsVerified");
        SecureStorage.Remove("RememberLogin");
    }

    public void handleError(string error)
    {
        ErrorText = error;
        NextText = "Retry";
        IsNextEnabled = true;
        SecureStorage.Remove("ServerKey");
        SecureStorage.Remove("PhoneNumber");
        SecureStorage.Remove("IsVerified");
        SecureStorage.Remove("RememberLogin");
    }

    [RelayCommand]
    async Task Next()
    {
        IsNextEnabled = false;
        // controllo se il numero di telefono è valido
        if (!string.IsNullOrEmpty(PhoneNumber))
        {
            PhoneNumber = PhoneNumber.Replace(" ", string.Empty);
            if (PhoneNumber.Length == 10)
            {
                try
                {
                    string storingNumber = SelectedPhonePrefix + PhoneNumber;
                    ErrorText = string.Empty;

                    // genero la chiave pubblica e privata se non esistono
                    string? existingPublicKey = await SecureStorage.GetAsync("PublicDeviceKey");
                    string? existingPrivateKey = await SecureStorage.GetAsync("PrivateDeviceKey");
                    if (string.IsNullOrEmpty(existingPublicKey) || string.IsNullOrEmpty(existingPrivateKey))
                    {
                        using RSA rsa = RSA.Create(2048);
                        string publicKey = rsa.ExportRSAPublicKeyPem();
                        string privateKey = rsa.ExportRSAPrivateKeyPem();

                        await SecureStorage.SetAsync("PublicDeviceKey", publicKey);
                        await SecureStorage.SetAsync("PrivateDeviceKey", privateKey);
                    }

                    // genero un id univoco per il dispositivo se non esiste 
                    deviceId = await SecureStorage.GetAsync("DeviceId");
                    if (string.IsNullOrEmpty(deviceId))
                    {
                        deviceId = Guid.NewGuid().ToString();
                    }
                    string getServerKeyResponse = await _registerAPI.GetServerKey();
                    if (getServerKeyResponse != "success")
                    {
                        handleError("Error while retrieving server key.");
                        return;
                    }
                    
                    await SecureStorage.SetAsync("PhoneNumber", storingNumber);
                    await SecureStorage.SetAsync("DeviceId", deviceId);
                    if (RememberLogin == true)
                        await SecureStorage.SetAsync("RememberLogin", "y");
                    else
                        await SecureStorage.SetAsync("RememberLogin", "n");

                    // richiedo la registrazione al server
                    var  signInResponse = await _registerAPI.SignInRequest();
                    if (signInResponse == "success")
                    {
                        // vado alla pagina di verifica
                        string? phoneNumber = await SecureStorage.GetAsync("PhoneNumber");
                        if (phoneNumber == null)
                        {
                            handleError("Error while retrieving phone number.");
                            return;
                        }
                        var sendSMSResponse = await _verificationAPI.SendSMS(phoneNumber);
                        if (sendSMSResponse == "success")
                        {
                            var verificationVm = new VerificationViewModel(_registerAPI, _verificationAPI);
                            var verificationPage = new VerificationPage(verificationVm);
                            await Shell.Current.Navigation.PushModalAsync(verificationPage);
                            return;
                        }
                        else
                        {
                            handleError("Error while sending SMS.");
                            return;
                        }
                    }
                    else
                    {
                        handleError("Error while signing in.");
                        return;
                    }
                }
                catch (Exception)
                {
                    handleError("Internal exception error while signing in.");
                    return;
                }
            }
            else
            {
               handleError("Invalid phone number");
                return;
            }
        }
    }
}
