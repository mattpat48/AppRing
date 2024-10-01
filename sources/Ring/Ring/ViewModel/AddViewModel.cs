using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ring.Services;
using Ring.Utils;

namespace Ring.ViewModel;

[QueryProperty(nameof(GateId), "GateId")]
[QueryProperty(nameof(GateName), "GateName")]
[QueryProperty(nameof(HttpClientProperty), "HttpClient")]

public partial class AddViewModel : ObservableObject
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
    bool isAddEnabled;
    [ObservableProperty]
    string addText;

    [ObservableProperty]
    string gateId;
    [ObservableProperty]
    string gateName;

    // messaggio di errore
    [ObservableProperty]
    string titleText;

    private AddAPI _addAPI;
    [ObservableProperty]
    HttpClient httpClientProperty;

    public AddViewModel()
    {
        PhoneNumber = string.Empty;

        PhonePrefixes = new List<string> { "+39", "+44", "+1", "+49", "+33", "+34", "+31", "+32", "+30", "+41", "+43", "+45", "+46", "+47", "+48" };
        SelectedPhonePrefix = "+39";

        isAddEnabled = true;
        AddText = "Add";   
    }

    public void OnAppearing()
    {
        if (HttpClientProperty != null)
        {
            _addAPI = new AddAPI(HttpClientProperty);
        }
        TitleText = "Add user to " + GateName;
        return;
    }

    public void handleError(string error)
    {
        NotificationTools.makeToast(error);
        AddText = "Retry";
        IsAddEnabled = true;
    }

    [RelayCommand]
    async Task Add()
    {
        IsAddEnabled = false;
        // controllo se il numero di telefono è valido
        if (!string.IsNullOrEmpty(PhoneNumber))
        {
            PhoneNumber = PhoneNumber.Replace(" ", string.Empty);
            if (PhoneNumber.Length == 10)
            {
                try
                {
                    string addingNumber = SelectedPhonePrefix + PhoneNumber;

                    // richiedo la registrazione al server
                    var addUserResponse = await _addAPI.AddUserRequest(GateId, addingNumber);
                    if (addUserResponse == "success")
                    {
                        NotificationTools.makeToast("User added successfully");
                        await Shell.Current.GoToAsync("..");
                    }
                    else
                    {
                        handleError("Error while adding user.");
                        return;
                    }
                }
                catch (Exception)
                {
                    handleError("Internal exception error while adding.");
                    return;
                }
            }
            else
            {
                handleError("Invalid phone number.");
                return;
            }
        }
    }


    [RelayCommand]
    async Task Close()
    {
        IsAddEnabled = false;
        await Shell.Current.GoToAsync("..");
        return;
    }
}
