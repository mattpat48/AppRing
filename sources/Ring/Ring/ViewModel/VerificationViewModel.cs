using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
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
        if (!string.IsNullOrEmpty(VerificationCode))
        {
            if (VerificationCode.Length == 8)
            {
                ErrorText = string.Empty;
                await Shell.Current.Navigation.PopToRootAsync();
            }
            else
            {
                ErrorText = "Codice di verifica non valido";
            }
        }
    }

    [RelayCommand]
    async Task Resend()
    {
        VerificationCode = string.Empty;
    }

    [RelayCommand]
    async Task Back()
    {
        await Shell.Current.Navigation.PopModalAsync();
    }

}
