using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Security.Cryptography;
using Ring.Services;
using Ring.Utils;

namespace Ring.ViewModel;

[QueryProperty (nameof(HttpClientProperty), "HttpClient")]
public partial class RegisterViewModel : ObservableObject
{
    // numero di telefono
    [ObservableProperty]
    string phoneNumber;
    [ObservableProperty]
    bool rememberLogin;

    // prefisso di nazionalità
    [ObservableProperty]
    List<string> phonePrefixes;
    [ObservableProperty]
    string selectedPhonePrefix;

    // proprietà per pulsanti e testi
    [ObservableProperty]
    bool isNextEnabled;
    [ObservableProperty]
    string nextText;

    private string deviceId;

    private RegisterAPI _registerAPI;
    private VerificationAPI _verificationAPI;
    [ObservableProperty]
    HttpClient httpClientProperty;

    // costruttore per il viewmodel
    public RegisterViewModel()
    {
        PhoneNumber = string.Empty;
        rememberLogin = false;

        PhonePrefixes = new List<string> { "+39", "+44", "+1", "+49", "+33", "+34", "+31", "+32", "+30", "+41", "+43", "+45", "+46", "+47", "+48"};
        SelectedPhonePrefix = "+39";

        isNextEnabled = true;
        NextText = "Next";
        
        this.deviceId = string.Empty;
    }

    public void OnAppearing()
    {
        IsNextEnabled = true;
        NextText = "Next";

        SecureStorage.Remove("PhoneNumber");
        SecureStorage.Remove("IsVerified");
        SecureStorage.Remove("RememberLogin");
        if (HttpClientProperty != null)
        {
            _registerAPI = new RegisterAPI(HttpClientProperty);
            _verificationAPI = new VerificationAPI(HttpClientProperty);
        }
        return;
    }

    public void handleError(string error)
    {
        NotificationTools.makeToast(error);
        NextText = "Retry";
        IsNextEnabled = true;
        SecureStorage.Remove("PhoneNumber");
        SecureStorage.Remove("IsVerified");
        SecureStorage.Remove("RememberLogin");
        return;
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
                    // compongo il numero che devo salvare in memoria
                    string storingNumber = SelectedPhonePrefix + PhoneNumber;

                    // genero la chiave pubblica e privata se non esistono
                    string? existingPublicKey = await SecureStorage.GetAsync("PublicDeviceKey");
                    string? existingPrivateKey = await SecureStorage.GetAsync("PrivateDeviceKey");
                    if (string.IsNullOrEmpty(existingPublicKey) || string.IsNullOrEmpty(existingPrivateKey))
                    {
                        using RSA rsa = RSA.Create(1024);
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
                    string? serverKey = await SecureStorage.GetAsync("ServerKey");
                    if (string.IsNullOrEmpty(serverKey))
                    {
                        string getServerKeyResponse = await _registerAPI.GetServerKey();
                        if (getServerKeyResponse != "success")
                        {
                            handleError("Error while retrieving server key.");
                            return;
                        }
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
                            //var verificationVm = new VerificationViewModel(_registerAPI, _verificationAPI);
                            //var verificationPage = new VerificationPage(verificationVm);
                            //await Shell.Current.Navigation.PushModalAsync(verificationPage);
                            var navigationParameters = new Dictionary<string, object>
                            {
                                { "HttpClient", HttpClientProperty}
                            };
                            await Shell.Current.GoToAsync(nameof(VerificationPage), navigationParameters);
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
