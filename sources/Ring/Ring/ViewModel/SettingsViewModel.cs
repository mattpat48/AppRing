using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MQTTnet.Client;
using MQTTnet.Server;
using Ring.Services;

namespace Ring.ViewModel;

public partial class SettingsViewModel : ObservableObject
{

    [ObservableProperty]
    string errorText;

    private RegisterAPI _registerAPI;
    private VerificationAPI _verificationAPI;
    private IMqttClient _mqttClient;

    public SettingsViewModel(RegisterAPI registerAPI, VerificationAPI verificationAPI, IMqttClient mqttClient)
    {
        errorText = string.Empty;

        _registerAPI = registerAPI;
        _verificationAPI = verificationAPI;
        _mqttClient = mqttClient;
    }

    [RelayCommand]
    async Task RemoveNumber()
    {
        try
        {
            SecureStorage.Remove("PhoneNumber");
            await _mqttClient.DisconnectAsync();
            var registerVm = new RegisterViewModel(_registerAPI, _verificationAPI);
            var registerPage = new RegisterPage(registerVm);
            await Shell.Current.Navigation.PopToRootAsync();
            await Shell.Current.Navigation.PushModalAsync(registerPage);
            return;
        }
        catch (Exception)
        {
            ErrorText = "Error while deleting number.";
            return;
        }
    }

    [RelayCommand]
    async Task Close()
    {
        await Shell.Current.Navigation.PopAsync();
        return;
    }
}
