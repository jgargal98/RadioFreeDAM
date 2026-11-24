namespace RadioFreeDAM.Services;

/// <summary>Stops the radio after a chosen time (to fall asleep with the radio on).</summary>
public interface ISleepTimerService
{
    /// <summary>Durations offered to the user, in minutes.</summary>
    IReadOnlyList<int> Options { get; }

    bool IsActive { get; }

    /// <summary>Whole minutes left, rounded up (the last minute counts as 1). Zero when there is no timer.</summary>
    int MinutesLeft { get; }

    /// <summary>Raised when the timer starts, is cancelled, ends, or the minutes left change.</summary>
    event EventHandler? Changed;

    /// <summary>Starts the timer. Replaces the previous one if there was one.</summary>
    void Start(TimeSpan duration);

    void Cancel();
}
