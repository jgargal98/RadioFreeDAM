using System.Net.Http.Headers;

namespace RadioFreeDAM.Services;

/// <summary>
/// Sesión del usuario mientras la app está abierta (solo en memoria, como pide el PDF).
/// Al iniciar sesión pone el token en el HttpClient compartido, así todas las peticiones lo llevan.
/// </summary>
public class SessionService(HttpClient httpClient) : ISessionService
{
    public UserDto? CurrentUser { get; private set; }

    public bool IsLoggedIn => CurrentUser is not null;

    /// <summary>Se lanza al iniciar o cerrar sesión.</summary>
    public event EventHandler? SessionChanged;

    public void SignIn(AuthResponse response)
    {
        CurrentUser = response.User;
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", response.Token);
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SignOut()
    {
        if (!IsLoggedIn)
            return;

        CurrentUser = null;
        httpClient.DefaultRequestHeaders.Authorization = null;
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }
}
