using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace RadioFreeDAM.Services;

/// <summary>
/// Sesión del usuario. Al iniciar sesión pone el token en el HttpClient compartido, así todas las peticiones lo llevan.
/// Si se pide recordarla, guarda token, caducidad y usuario en SecureStorage (cifrado por el sistema operativo).
/// </summary>
public class SessionService(HttpClient httpClient, IAuthApiService authApi) : ISessionService
{
    private const string StorageKey = "rfd_session";

    public UserDto? CurrentUser { get; private set; }

    public bool IsLoggedIn => CurrentUser is not null;

    public event EventHandler? SessionChanged;

    public async Task SignInAsync(AuthResponse response, bool remember)
    {
        Apply(response.Token, response.User);

        if (remember)
            await SaveAsync(new StoredSession(response.Token, response.ExpiresAt, response.User));
        else
            Forget();
    }

    public void SignOut()
    {
        Forget();

        if (!IsLoggedIn)
            return;

        CurrentUser = null;
        httpClient.DefaultRequestHeaders.Authorization = null;
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task<bool> RestoreAsync()
    {
        var stored = await LoadAsync();
        if (stored is null)
            return false;

        if (stored.ExpiresAt <= DateTime.UtcNow)
        {
            Forget();
            return false;
        }

        // Se entra ya con los datos guardados, para no esperar a la red.
        Apply(stored.Token, stored.User);

        try
        {
            // Comprueba con la API que el token sigue valiendo (y que el usuario existe).
            var user = await authApi.GetCurrentUserAsync();
            CurrentUser = user;
            await SaveAsync(stored with { User = user });
            SessionChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            SignOut();
            return false;
        }
        catch (Exception ex) when (ApiException.IsCommunicationError(ex))
        {
            // Sin conexión con la API: se mantiene la sesión; se comprobará en la siguiente petición.
        }

        return true;
    }

    private void Apply(string token, UserDto user)
    {
        CurrentUser = user;
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }

    private static async Task SaveAsync(StoredSession session)
    {
        try
        {
            await SecureStorage.Default.SetAsync(StorageKey, JsonSerializer.Serialize(session));
        }
        catch (Exception)
        {
            // Algunos dispositivos no tienen almacenamiento seguro: la sesión funciona igual, solo que no se recuerda.
        }
    }

    private static async Task<StoredSession?> LoadAsync()
    {
        try
        {
            var json = await SecureStorage.Default.GetAsync(StorageKey);
            return json is null ? null : JsonSerializer.Deserialize<StoredSession>(json);
        }
        catch (Exception)
        {
            // Datos dañados o almacenamiento no disponible: como si no hubiera sesión guardada.
            Forget();
            return null;
        }
    }

    private static void Forget()
    {
        try
        {
            SecureStorage.Default.Remove(StorageKey);
        }
        catch (Exception)
        {
            // Nada que borrar.
        }
    }

    /// <summary>Lo que se guarda para recordar la sesión.</summary>
    private sealed record StoredSession(string Token, DateTime ExpiresAt, UserDto User);
}
