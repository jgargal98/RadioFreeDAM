namespace RadioFreeDAM.Models;

/// <summary>
/// Estados del reproductor, independientes de la librería de audio que se use.
/// </summary>
public enum PlaybackState
{
    Stopped,

    /// <summary>Conectando con el stream o cargando el búfer.</summary>
    Loading,

    Playing,

    /// <summary>El stream no se ha podido reproducir. El motivo está en IPlayerService.ErrorMessage.</summary>
    Error
}
