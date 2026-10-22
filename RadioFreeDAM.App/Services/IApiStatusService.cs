namespace RadioFreeDAM.Services;

/// <summary>
/// Comprueba si nuestra API está arrancada y responde.
/// </summary>
public interface IApiStatusService
{
    /// <summary>Devuelve true si la API responde correctamente; false si no hay conexión.</summary>
    Task<bool> IsServerAvailableAsync();
}
