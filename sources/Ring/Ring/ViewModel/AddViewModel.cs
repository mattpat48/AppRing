using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ring.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Ring.ViewModel;

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

    // messaggio di errore
    [ObservableProperty]
    string messageText;
    [ObservableProperty]
    string textColor;
    [ObservableProperty]
    string titleText;

    private AddAPI _addAPI;

    public AddViewModel(AddAPI addAPI, string gateId, string gateName)
    {
        PhoneNumber = string.Empty;
        PhonePrefixes = new List<string> { "+39", "+44", "+1", "+49", "+33", "+34", "+31", "+32", "+30", "+41", "+43", "+45", "+46", "+47", "+48" };
        SelectedPhonePrefix = "+39";
        MessageText = string.Empty;
        TextColor = "White";
        TitleText = "Add a new user to " + gateName;
        isAddEnabled = true;
        AddText = "Add";
        _addAPI = addAPI;
        GateId = gateId;
    }

    public void handleError(string error)
    {
        TextColor = "Red";
        MessageText = error;
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
                    MessageText = string.Empty;

                    // richiedo la registrazione al server
                    var addUserResponse = await _addAPI.AddUserRequest(GateId, addingNumber);
                    if (addUserResponse == "success")
                    {
                        TextColor = "Green";
                        MessageText = "User Added.";
                        await Shell.Current.Navigation.PopToRootAsync();
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
        await Shell.Current.Navigation.PopAsync();
        return;
    }
}
