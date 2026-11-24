namespace RadioFreeDAM.Services;

/// <summary>
/// Sleep timer. It lives in a singleton service (not in the page) so it keeps running when the user changes tab
/// or sends the app to the background; the foreground service of the player keeps the process alive.
/// </summary>
public class SleepTimerService : ISleepTimerService
{
    /// <summary>Margin after a minute boundary, so the rounded-up minutes have really changed when we wake up.</summary>
    private static readonly TimeSpan BoundaryMargin = TimeSpan.FromMilliseconds(100);

    private readonly IPlayerService _player;
    private readonly TimeProvider _time;

    private CancellationTokenSource? _cts;
    private DateTimeOffset _endsAt;
    private string? _lastStationId;

    public SleepTimerService(IPlayerService player, TimeProvider? time = null)
    {
        _player = player;
        _time = time ?? TimeProvider.System;
        _lastStationId = player.CurrentStation?.Id;

        _player.StateChanged += (_, _) => OnPlayerStateChanged();
    }

    public IReadOnlyList<int> Options { get; } = [15, 30, 45, 60, 90];

    public bool IsActive => _cts is not null;

    public int MinutesLeft => IsActive
        ? Math.Max(1, (int)Math.Ceiling((_endsAt - _time.GetUtcNow()).TotalMinutes))
        : 0;

    public event EventHandler? Changed;

    public void Start(TimeSpan duration)
    {
        Stop();

        _endsAt = _time.GetUtcNow() + duration;
        var cts = _cts = new CancellationTokenSource();
        _ = RunAsync(cts);

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Cancel()
    {
        if (!IsActive)
            return;

        Stop();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void Stop()
    {
        _cts?.Cancel();
        _cts = null;
    }

    /// <summary>
    /// Sleeps until the next minute boundary of the time left, so the text changes exactly when the minutes
    /// change instead of polling every second. The last stretch sleeps until the end.
    /// </summary>
    private async Task RunAsync(CancellationTokenSource cts)
    {
        try
        {
            while (true)
            {
                var remaining = _endsAt - _time.GetUtcNow();
                if (remaining <= TimeSpan.Zero)
                    break;

                var untilBoundary = TimeSpan.FromTicks(remaining.Ticks % TimeSpan.TicksPerMinute);
                if (untilBoundary == TimeSpan.Zero)
                    untilBoundary = TimeSpan.FromMinutes(1);

                var delay = untilBoundary + BoundaryMargin;
                if (delay >= remaining)
                {
                    await Task.Delay(remaining, _time, cts.Token);
                    break;
                }

                await Task.Delay(delay, _time, cts.Token);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (!cts.IsCancellationRequested)
                        Changed?.Invoke(this, EventArgs.Empty);
                });
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            // A cancel or a new timer may have arrived while we were switching to the main thread.
            if (cts.IsCancellationRequested)
                return;

            Stop();
            _player.Stop();
            Changed?.Invoke(this, EventArgs.Empty);
        });
    }

    /// <summary>
    /// If the user stops the radio, the timer has nothing left to do. Changing station also passes through
    /// "stopped", but then the station id changes and the timer keeps running.
    /// </summary>
    private void OnPlayerStateChanged()
    {
        var stationId = _player.CurrentStation?.Id;
        var sameStation = stationId == _lastStationId;
        _lastStationId = stationId;

        if (IsActive && sameStation && _player.State is PlaybackState.Stopped or PlaybackState.Error)
            Cancel();
    }
}
