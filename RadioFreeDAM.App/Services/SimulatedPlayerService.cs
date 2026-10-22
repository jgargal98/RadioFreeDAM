namespace RadioFreeDAM.Services;

/// <summary>
/// Reproductor simulado de la Fase 1: cambia entre reproduciendo y detenido, pero no suena audio.
/// En la Fase 2 se sustituye por un reproductor real que use StreamUrl.
/// </summary>
public class SimulatedPlayerService : IPlayerService
{
    public StationDto? CurrentStation { get; private set; }

    public PlaybackState State { get; private set; } = PlaybackState.Stopped;

    public event EventHandler? StateChanged;

    public void Select(StationDto station)
    {
        if (CurrentStation?.Id == station.Id)
            return;

        CurrentStation = station;
        State = PlaybackState.Stopped;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Play()
    {
        if (CurrentStation is null || State == PlaybackState.Playing)
            return;

        State = PlaybackState.Playing;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Stop()
    {
        if (State == PlaybackState.Stopped)
            return;

        State = PlaybackState.Stopped;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
