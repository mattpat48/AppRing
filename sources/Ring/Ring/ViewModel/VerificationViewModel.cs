using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ring.Services;
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

    public async void makeToast(string message)
    {

        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        string text = message;
        ToastDuration duration = ToastDuration.Short;
        double fontSize = 14;

        var toast = Toast.Make(text, duration, fontSize);
        await toast.Show(cancellationTokenSource.Token);
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
                        makeToast(hasManyDevicesResponse);
                        IsVerifyEnabled = true;
                        return;
                    }
                    else if (manydevs)
                    {
                        //var multipleVm = new MultipleViewModel();
                        //var multiplePage = new MultiplePage(multipleVm);
                        //await Shell.Current.Navigation.PushModalAsync(multiplePage);
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
                        await Shell.Current.GoToAsync("../..");
                        return;
                    }
                }
                else
                {
                    IsVerifyEnabled = true;
                    makeToast("Invalid code");
                    SecureStorage.Remove("IsVerified");
                    return;
                }
            }
            else
            {
                IsVerifyEnabled = true;
                makeToast("Invalid code");
                SecureStorage.Remove("IsVerified");
                return;
            }
        }
        catch (Exception ex)
        {
            IsVerifyEnabled = true;
            makeToast(ex.Message);
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
            makeToast("Phone number not found");
            IsVerifyEnabled = true;
            return;
        }
        var sendSMSResponse = await _verificationAPI.SendSMS(phoneNumber);
        if (sendSMSResponse == "success")
        {
            makeToast("SMS sent successfully");
            IsVerifyEnabled = true;
            return;
        }
        else
        {
            makeToast("Error while sending SMS");
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
