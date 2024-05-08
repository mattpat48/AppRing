using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ring.Services;

namespace Ring.ViewModel;

public partial class VerificationViewModel : ObservableObject
{
    [ObservableProperty]
    string verificationCode;

    [ObservableProperty]
    bool isVerifyEnabled;

    [ObservableProperty]
    string messageText;
    [ObservableProperty]
    string colorText;


    private RegisterAPI _registerAPI;
    private VerificationAPI _verificationAPI;

    public VerificationViewModel(RegisterAPI registerAPI, VerificationAPI verificationAPI)
    {
        VerificationCode = string.Empty;
        IsVerifyEnabled = true;
        MessageText = string.Empty;
        ColorText = "White";
        _registerAPI = registerAPI;
        _verificationAPI = verificationAPI;
    }

    [RelayCommand]
    async Task Verify()
    {
        IsVerifyEnabled = false;
        // verifica del codice di verifica
        VerificationCode = VerificationCode.Replace(" ", string.Empty);
        if (!string.IsNullOrEmpty(VerificationCode) && VerificationCode.Length == 8)
        {
            var response = await _verificationAPI.VerifyCode(VerificationCode);
            if (response == "success")
            {
                // codice di verifica corretto, vado sulla main page
                MessageText = string.Empty;
                await SecureStorage.SetAsync("IsVerified", "y");
                await Shell.Current.Navigation.PopToRootAsync();
                return;
            }
            else
            {
                IsVerifyEnabled = true;
                ColorText = "Red";
                MessageText = "Verification code does not match!";
                SecureStorage.Remove("IsVerified");
                return;
            }
        }
        else
        {
            IsVerifyEnabled = true;
            ColorText = "Red";
            MessageText = "Insert a valid code!";
            SecureStorage.Remove("IsVerified");
            return;
        }
    }

    // comando per inviare nuovamente il codice di verifica
    [RelayCommand]
    async Task Resend()
    {
        IsVerifyEnabled = false;
        string? phoneNumber = await SecureStorage.GetAsync("PhoneNumber");
        if (phoneNumber == null)
        {
            ColorText = "Red";
            MessageText = "Phone number not found!";
            IsVerifyEnabled = true;
            return;
        }
        var sendSMSResponse = await _verificationAPI.SendSMS(phoneNumber);
        if (sendSMSResponse == "success")
        {
            ColorText = "White";
            MessageText = "We sent you another code.";
            IsVerifyEnabled = true;
            return;
        }
        else
        {
            ColorText = "Red";
            MessageText = "Error while sending SMS.";
            IsVerifyEnabled = true;
            return;
        }
    }

    // comando per tornare indietro
    [RelayCommand]
    async Task Back()
    {
        IsVerifyEnabled = false;
        SecureStorage.Remove("ServerKey");
        SecureStorage.Remove("PhoneNumber");
        SecureStorage.Remove("IsVerified");

        //var registerVm = new RegisterViewModel(_registerAPI, _verificationAPI);
        //var registerPage = new RegisterPage(registerVm);
        //await Shell.Current.Navigation.PushModalAsync(registerPage);
        await Shell.Current.Navigation.PopAsync();
    }

}
