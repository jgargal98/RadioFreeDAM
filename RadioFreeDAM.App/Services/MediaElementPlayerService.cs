using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;

namespace RadioFreeDAM.Services;

/// <summary>
/// Reproductor real basado en MediaElement (CommunityToolkit.Maui.MediaElement).
/// MediaElement es un control visual, así que vive en PlayerPage; la página se lo entrega
/// a este servicio con <see cref="Attach"/> y el servicio lo maneja y traduce sus eventos
/// a nuestro <see cref="PlaybackState"/>.
/// </summary>
public class MediaElementPlayerService : IPlayerService
{
    /// <summary>Si el stream no empieza a sonar en este tiempo, se considera que la emisora no responde.</summary>
    private static readonly TimeSpan LoadingTimeout = TimeSpan.FromSeconds(15);

    private MediaElement? _mediaElement;
    private CancellationTokenSource? _loadingTimeoutCts;

    public StationDto? CurrentStation { get; private set; }

    public PlaybackState State { get; private set; } = PlaybackState.Stopped;

    public string? ErrorMessage { get; private set; }

    public event EventHandler? StateChanged;

    /// <summary>Lo llama PlayerPage al crearse para entregar su control de audio.</summary>
    public void Attach(MediaElement mediaElement)
    {
        if (_mediaElement == mediaElement)
            return;

        if (_mediaElement is not null)
        {
            _mediaElement.StateChanged -= OnMediaStateChanged;
            _mediaElement.MediaFailed -= OnMediaFailed;
        }

        _mediaElement = mediaElement;
        _mediaElement.StateChanged += OnMediaStateChanged;
        _mediaElement.MediaFailed += OnMediaFailed;
    }

    public void Select(StationDto station)
    {
        if (CurrentStation?.Id == station.Id)
            return;

        StopPlayback();
        CurrentStation = station;
        SetState(PlaybackState.Stopped);
    }

    public void Play()
    {
        if (CurrentStation is null || _mediaElement is null)
            return;

        if (State is PlaybackState.Playing or PlaybackState.Loading)
            return;

        SetState(PlaybackState.Loading);
        StartLoadingTimeout();

        // Con ShouldAutoPlay="True" en la vista, asignar la fuente ya empieza la reproducción.
        // Se asigna cada vez para conectar con el directo y no con un búfer antiguo.
        _mediaElement.Source = MediaSource.FromUri(CurrentStation.StreamUrl);
    }

    public void Stop()
    {
        StopPlayback();
        SetState(PlaybackState.Stopped);
    }

    private void StopPlayback()
    {
        CancelLoadingTimeout();

        if (_mediaElement is null)
            return;

        _mediaElement.Stop();
        _mediaElement.Source = null;
    }

    private void OnMediaStateChanged(object? sender, MediaStateChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Si el usuario ya ha detenido o hay un error, ignoramos los avisos que lleguen tarde.
            if (State is PlaybackState.Stopped or PlaybackState.Error)
                return;

            switch (e.NewState)
            {
                case MediaElementState.Opening:
                case MediaElementState.Buffering:
                    SetState(PlaybackState.Loading);
                    break;

                case MediaElementState.Playing:
                    CancelLoadingTimeout();
                    SetState(PlaybackState.Playing);
                    break;

                case MediaElementState.Failed:
                    Fail("No se ha podido reproducir esta emisora.");
                    break;

                case MediaElementState.Paused:
                case MediaElementState.Stopped:
                    // Lo ha parado el sistema o se ha cortado el stream.
                    CancelLoadingTimeout();
                    SetState(PlaybackState.Stopped);
                    break;
            }
        });
    }

    private void OnMediaFailed(object? sender, MediaFailedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (State == PlaybackState.Stopped)
                return;

            Fail("No se ha podido reproducir esta emisora. Puede que el stream no esté disponible ahora mismo.");
        });
    }

    private void Fail(string message)
    {
        StopPlayback();
        ErrorMessage = message;
        State = PlaybackState.Error;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SetState(PlaybackState state)
    {
        ErrorMessage = null;
        State = state;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void StartLoadingTimeout()
    {
        CancelLoadingTimeout();
        var cts = _loadingTimeoutCts = new CancellationTokenSource();

        Task.Delay(LoadingTimeout, cts.Token).ContinueWith(task =>
        {
            if (task.IsCanceled)
                return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (!cts.IsCancellationRequested && State == PlaybackState.Loading)
                    Fail("La emisora no responde. Comprueba tu conexión o prueba con otra.");
            });
        }, TaskScheduler.Default);
    }

    private void CancelLoadingTimeout()
    {
        _loadingTimeoutCts?.Cancel();
        _loadingTimeoutCts = null;
    }
}
