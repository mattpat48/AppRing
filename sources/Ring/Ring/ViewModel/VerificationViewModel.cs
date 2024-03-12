using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Ring.ViewModel;

public partial class VerificationViewModel : ObservableObject
{
    [ObservableProperty]
    string verificationCode;

    [ObservableProperty]
    bool isVerifyEnabled;

    [ObservableProperty]
    string errorText;

    public VerificationViewModel()
    {
        VerificationCode = string.Empty;
        IsVerifyEnabled = false;
        ErrorText = string.Empty;
    }

    [RelayCommand]
    async Task Verify()
    {
        // verifica del codice di verifica
        if (!string.IsNullOrEmpty(VerificationCode))
        {
            if (VerificationCode.Length == 8)
            {
                // codice di verifica corretto, vado sulla main page
                ErrorText = string.Empty;
                await SecureStorage.SetAsync("IsVerified", "yes");
                await Shell.Current.Navigation.PopToRootAsync();
            }
            else
            {
                ErrorText = "Codice di verifica non valido";
            }
        }
    }

    // comando per inviare nuovamente il codice di verifica
    [RelayCommand]
    async Task Resend()
    {
        VerificationCode = string.Empty;
    }

    // comando per tornare indietro
    [RelayCommand]
    async Task Back()
    {
        SecureStorage.Remove("PublicDeviceKey");
        SecureStorage.Remove("PrivateDeviceKey");
        SecureStorage.Remove("ServerKey");
        SecureStorage.Remove("PhoneNumber");
        SecureStorage.Remove("DeviceId");

        var registerVm = new RegisterViewModel();
        var registerPage = new RegisterPage(registerVm);
        await Shell.Current.Navigation.PushModalAsync(registerPage);
    }

}
