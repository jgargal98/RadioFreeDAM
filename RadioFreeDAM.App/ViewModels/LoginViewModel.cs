using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RadioFreeDAM.ViewModels;

/// <summary>Pantalla Iniciar sesión.</summary>
public partial class LoginViewModel(
    IAuthApiService authApi,
    ISessionService session,
    IFavoritesService favorites,
    INavigationService navigation) : ObservableObject
{
    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    /// <summary>Casilla «Mantener la sesión iniciada».</summary>
    [ObservableProperty]
    public partial bool RememberMe { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsBusy)
            return;

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrEmpty(Password))
        {
            ErrorMessage = "Escribe tu correo y tu contraseña.";
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            var response = await authApi.LoginAsync(new LoginRequest { Email = Email.Trim(), Password = Password });
            await AccountFlow.CompleteSignInAsync(response, RememberMe, session, favorites, navigation);
            Password = string.Empty;
        }
        catch (Exception ex) when (ApiException.IsCommunicationError(ex))
        {
            ErrorMessage = ApiException.ToUserMessage(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task GoToRegisterAsync()
    {
        ErrorMessage = null;
        return navigation.GoToRegisterAsync();
    }
}

/// <summary>Pasos comunes al terminar el login o el registro.</summary>
internal static class AccountFlow
{
    public static async Task CompleteSignInAsync(
        AuthResponse response,
        bool remember,
        ISessionService session,
        IFavoritesService favorites,
        INavigationService navigation)
    {
        await session.SignInAsync(response, remember);
        await navigation.CloseAccountPagesAsync();

        try
        {
            await favorites.LoadAsync();
        }
        catch (Exception ex) when (ApiException.IsCommunicationError(ex))
        {
            // La sesión ya está iniciada; la pantalla Favoritos mostrará el error y permitirá reintentar.
        }
    }
}
