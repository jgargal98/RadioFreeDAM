namespace RadioFreeDAM.Services;

/// <summary>Usuario que ha iniciado sesión. Se pierde al cerrar la app.</summary>
public interface ISessionService
{
    UserDto? CurrentUser { get; }

    bool IsLoggedIn { get; }

    /// <summary>Se lanza al iniciar o cerrar sesión.</summary>
    event EventHandler? SessionChanged;

    void SignIn(AuthResponse response);

    void SignOut();
}
