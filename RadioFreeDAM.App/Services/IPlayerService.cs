namespace RadioFreeDAM.Services;

/// <summary>
/// Estado único del reproductor para toda la app: qué emisora está seleccionada y si suena.
/// Inicio y Reproductor leen de aquí, por eso siempre muestran lo mismo.
/// </summary>
public interface IPlayerService
{
    StationDto? CurrentStation { get; }

    PlaybackState State { get; }

    /// <summary>Mensaje para el usuario cuando <see cref="State"/> es Error.</summary>
    string? ErrorMessage { get; }

    /// <summary>Se lanza (en el hilo de la interfaz) cada vez que cambia la emisora o el estado.</summary>
    event EventHandler? StateChanged;

    /// <summary>Selecciona una emisora. Si sonaba otra, la detiene; la nueva no suena hasta llamar a Play.</summary>
    void Select(StationDto station);

    void Play();

    void Stop();
}
