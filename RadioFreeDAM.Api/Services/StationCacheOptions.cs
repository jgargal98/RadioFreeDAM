namespace RadioFreeDAM.Api.Services;

/// <summary>
/// Cuánto tiempo se reutilizan las respuestas de Radio Browser. Sección "StationCache" de appsettings.json.
/// </summary>
public class StationCacheOptions
{
    public const string SectionName = "StationCache";

    /// <summary>Tiempo durante el que la lista de populares se considera actual.</summary>
    public TimeSpan TopStationsDuration { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>Tiempo durante el que el resultado de una búsqueda se considera actual.</summary>
    public TimeSpan SearchDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Tiempo máximo que se conserva un resultado caducado para usarlo de respaldo
    /// si Radio Browser falla. Pasado este tiempo se borra de memoria.
    /// </summary>
    public TimeSpan FallbackDuration { get; set; } = TimeSpan.FromHours(24);
}
