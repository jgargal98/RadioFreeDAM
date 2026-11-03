namespace RadioFreeDAM.Api.Services;

/// <summary>
/// Configuración de los tokens JWT. La clave (Jwt:Key) es secreta: va en user-secrets, nunca en el repositorio.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Clave para firmar los tokens. Al menos 32 caracteres.</summary>
    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = "RadioFreeDAM.Api";

    public string Audience { get; set; } = "RadioFreeDAM.App";

    public int ExpirationDays { get; set; } = 7;
}
