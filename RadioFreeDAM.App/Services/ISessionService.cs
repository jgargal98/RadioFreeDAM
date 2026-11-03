namespace RadioFreeDAM.Services;

/// <summary>
/// Usuario que ha iniciado sesión. Por defecto vive en memoria (se pierde al cerrar la app);
/// si el usuario marca «Mantener la sesión iniciada», el token se guarda en el almacenamiento seguro del sistema.
/// </summary>
public interface ISessionService
{
    UserDto? CurrentUser { get; }

    bool IsLoggedIn { get; }

    /// <summary>Se lanza al iniciar o cerrar sesión.</summary>
    event EventHandler? SessionChanged;

    /// <param name="remember">true para recuperar la sesión la próxima vez que se abra la app.</param>
    Task SignInAsync(AuthResponse response, bool remember);

    void SignOut();

    /// <summary>
    /// Al abrir la app: si hay una sesión guardada y no ha caducado, la recupera y comprueba con la API
    /// que sigue siendo válida. Devuelve true si queda una sesión iniciada.
    /// </summary>
    Task<bool> RestoreAsync();
}
