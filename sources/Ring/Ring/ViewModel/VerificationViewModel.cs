using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ring.Services;
using Ring.Utils;
using System.Net.Http;

namespace Ring.ViewModel;

[QueryProperty(nameof(HttpClientProperty), "HttpClient")]
public partial class VerificationViewModel : ObservableObject
{
    [ObservableProperty]
    string verificationCode;
    [ObservableProperty]
    bool isVerifyEnabled;

    private VerificationAPI _verificationAPI;
    [ObservableProperty]
    HttpClient httpClientProperty;

    public VerificationViewModel()
    {
        VerificationCode = string.Empty;
        IsVerifyEnabled = true;
    }

    public void OnAppearing()
    {
        if (HttpClientProperty != null)
        {
            _verificationAPI = new VerificationAPI(HttpClientProperty);
        }
        return;
    }

    [RelayCommand]
    async Task Verify()
    {
        try
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
                    await SecureStorage.SetAsync("IsVerified", "y");

                    string hasManyDevicesResponse;
                    bool manydevs;
                    (hasManyDevicesResponse, manydevs) = await _verificationAPI.HasManyDevices();
                    if (hasManyDevicesResponse != "success")
                    {
                        NotificationTools.makeToast(hasManyDevicesResponse);
                        IsVerifyEnabled = true;
                        return;
                    }
                    else if (manydevs)
                    {
                        await SecureStorage.SetAsync("MultipleDevices", "y");
                        var phonenumber = await SecureStorage.GetAsync("PhoneNumber");
                        var navigationParameters = new Dictionary<string, object>
                        {
                            { "PhoneNumber", phonenumber},
                            { "HttpClient", HttpClientProperty}
                        };
                        await Shell.Current.GoToAsync(nameof(MultiplePage), navigationParameters);
                        return;
                    }
                    else
                    {
                        await SecureStorage.SetAsync("MultipleDevices", "n");
                        await Shell.Current.GoToAsync("../..");
                        return;
                    }
                }
                else
                {
                    IsVerifyEnabled = true;
                    NotificationTools.makeToast("Invalid code");
                    SecureStorage.Remove("IsVerified");
                    return;
                }
            }
            else
            {
                IsVerifyEnabled = true;
                NotificationTools.makeToast("Invalid code");
                SecureStorage.Remove("IsVerified");
                return;
            }
        }
        catch (Exception ex)
        {
            IsVerifyEnabled = true;
            NotificationTools.makeToast(ex.Message);
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
            NotificationTools.makeToast("Phone number not found");
            IsVerifyEnabled = true;
            return;
        }
        var sendSMSResponse = await _verificationAPI.SendSMS(phoneNumber);
        if (sendSMSResponse == "success")
        {
            NotificationTools.makeToast("SMS sent successfully");
            IsVerifyEnabled = true;
            return;
        }
        else
        {
            NotificationTools.makeToast("Error while sending SMS");
            IsVerifyEnabled = true;
            return;
        }
    }

    // comando per tornare indietro
    [RelayCommand]
    async Task Back()
    {
        IsVerifyEnabled = false;
        SecureStorage.Remove("PhoneNumber");
        SecureStorage.Remove("IsVerified");

        //var registerVm = new RegisterViewModel(_registerAPI, _verificationAPI);
        //var registerPage = new RegisterPage(registerVm);
        //await Shell.Current.Navigation.PushModalAsync(registerPage);
        var navigationParameters = new Dictionary<string, object>
        {
            { "HttpClient", HttpClientProperty}
        };
        await Shell.Current.GoToAsync("..", navigationParameters);
    }

}
