using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ring.Services;
using Ring.Utils;
using System.Collections.ObjectModel;

namespace Ring.ViewModel;

[QueryProperty(nameof(GateId), "GateId")]
[QueryProperty(nameof(GateName), "GateName")]
[QueryProperty(nameof(HttpClientProperty), "HttpClient")]

public partial class UsersViewModel : ObservableObject
{
    [ObservableProperty]
    string gateId;
    [ObservableProperty]
    string gateName;

    [ObservableProperty]
    string titleText;
    [ObservableProperty]
    bool isRemoveEnabled;

    [ObservableProperty]
    ObservableCollection<string> users;

    [ObservableProperty]
    HttpClient httpClientProperty;
    private UsersAPI _usersAPI;

    public UsersViewModel()
    {
        GateId = string.Empty;
        GateName = string.Empty;
        TitleText = string.Empty;
        IsRemoveEnabled = false;
        
        Users = new ObservableCollection<string>();
    }

    public async Task OnAppearing()
    {
        if (HttpClientProperty != null)
        {
            _usersAPI = new UsersAPI(HttpClientProperty);
        }
        TitleText = GateName + " users";
        await GetAllUsers();
        IsRemoveEnabled = true;
        return;
    }

    public void handleError(string error)
    {
        NotificationTools.makeToast(error);
        return;
    }

    private async Task GetAllUsers()
    {
        string? response;
        List<string>? usersReturned;
        (response, usersReturned) = await _usersAPI.GetUsersPerGate(GateId);
        if (response == "success")
        {
            if (usersReturned == null || usersReturned.Count == 0)
            {
                Users = new ObservableCollection<string>();
            }
            else if (usersReturned != null && usersReturned.Count > 0)
            {
                Users = usersReturned.ToObservableCollection();
            }
        }
        else
        {
            handleError(response);
        }
        return;
    }

    [RelayCommand]
    public async Task AddUser()
    {
        var navigationParameters = new Dictionary<string, object>
        {
            { "GateId", GateId},
            { "GateName", GateName},
            { "HttpClient", HttpClientProperty},
            { "UsersAPI", _usersAPI}
        };
        await Shell.Current.GoToAsync(nameof(AddPage), navigationParameters);

        return;
    }

    [RelayCommand]
    public async Task RemoveUser(string toRemove)
    {
        IsRemoveEnabled = false;
        if (toRemove == await SecureStorage.GetAsync("PhoneNumber"))
        {
            handleError("You can't remove yourself");
            IsRemoveEnabled = true;
            return;
        }
        string toRemoveResponse;
        toRemoveResponse = await _usersAPI.RemoveUserRequest(GateId, toRemove);

        if (toRemoveResponse == "success")
        {
            await GetAllUsers();
            NotificationTools.makeToast("User removed");
        }
        else
        {
            handleError(toRemoveResponse);
        }
        IsRemoveEnabled = true;
        return;
    }
}
