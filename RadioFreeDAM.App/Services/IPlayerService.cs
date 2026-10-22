namespace RadioFreeDAM.Services;

/// <summary>
/// Estado único del reproductor para toda la app: qué emisora está seleccionada y si suena.
/// Inicio y Reproductor leen de aquí, por eso siempre muestran lo mismo.
/// </summary>
public interface IPlayerService
{
    StationDto? CurrentStation { get; }

    PlaybackState State { get; }

    /// <summary>Se lanza cada vez que cambia la emisora o el estado de reproducción.</summary>
    event EventHandler? StateChanged;

    /// <summary>Selecciona una emisora. Si era otra la que sonaba, la detiene.</summary>
    void Select(StationDto station);

    void Play();

    void Stop();
}
