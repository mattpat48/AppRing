using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
namespace Ring.ViewModel;

public partial class SettingsViewModel : ObservableObject
{

    public SettingsViewModel()
    {
        
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
    public async Task RemoveNumber()
    {
        try
        {
            SecureStorage.Remove("PhoneNumber");
            Shell.Current.CurrentItem = Shell.Current.Items.FirstOrDefault(item => item.Route == "MainPage");
            return;
        }
        catch (Exception ex)
        {
            makeToast("Error while removing the phone number");
            return;
        }
    }
}
