using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RadioFreeDAM.ViewModels;

/// <summary>
/// Pantalla Crear cuenta. Comprueba lo básico antes de llamar a la API (para avisar al momento),
/// pero la validación que cuenta es la de la API.
/// </summary>
public partial class RegisterViewModel(
    IAuthApiService authApi,
    ISessionService session,
    IFavoritesService favorites,
    INavigationService navigation) : ObservableObject
{
    private const int MinPasswordLength = 8;

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ConfirmPassword { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (IsBusy)
            return;

        ErrorMessage = Validate();
        if (ErrorMessage is not null)
            return;

        IsBusy = true;
        try
        {
            var response = await authApi.RegisterAsync(new RegisterRequest
            {
                Name = Name.Trim(),
                Email = Email.Trim(),
                Password = Password
            });
            await AccountFlow.CompleteSignInAsync(response, session, favorites, navigation);
            Password = ConfirmPassword = string.Empty;
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
        {
            ErrorMessage = ApiException.ToUserMessage(ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Email) || string.IsNullOrEmpty(Password))
            return "Rellena el nombre, el correo y la contraseña.";

        if (!Email.Contains('@'))
            return "El correo no tiene un formato válido.";

        if (Password.Length < MinPasswordLength)
            return $"La contraseña debe tener al menos {MinPasswordLength} caracteres.";

        if (Password != ConfirmPassword)
            return "Las contraseñas no coinciden.";

        return null;
    }
}
