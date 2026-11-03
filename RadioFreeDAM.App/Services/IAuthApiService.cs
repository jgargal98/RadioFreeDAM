namespace RadioFreeDAM.Services;

/// <summary>Registro e inicio de sesión contra nuestra API.</summary>
public interface IAuthApiService
{
    /// <exception cref="ApiException">400 si los datos no son válidos, 409 si el correo ya existe.</exception>
    Task<AuthResponse> RegisterAsync(RegisterRequest request);

    /// <exception cref="ApiException">401 si el correo o la contraseña no son correctos.</exception>
    Task<AuthResponse> LoginAsync(LoginRequest request);

    /// <summary>Usuario del token actual. Sirve para comprobar si una sesión guardada sigue valiendo.</summary>
    /// <exception cref="ApiException">401 si el token ya no vale.</exception>
    Task<UserDto> GetCurrentUserAsync();
}
