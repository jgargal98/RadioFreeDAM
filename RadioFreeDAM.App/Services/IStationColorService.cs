namespace RadioFreeDAM.Services;

/// <summary>Color de acento de una emisora, sacado de su logo.</summary>
public interface IStationColorService
{
    /// <summary>
    /// Color dominante del logo, o null si la emisora no tiene logo, no se puede descargar
    /// o es en blanco y negro. Nunca lanza excepciones: el color es un adorno.
    /// </summary>
    Task<Color?> GetAccentColorAsync(StationDto station, CancellationToken cancellationToken = default);
}
