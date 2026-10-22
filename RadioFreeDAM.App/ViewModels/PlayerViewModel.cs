using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RadioFreeDAM.ViewModels;

/// <summary>
/// Pantalla Reproductor: muestra la emisora seleccionada y permite reproducir o detener.
/// </summary>
public partial class PlayerViewModel : ObservableObject
{
    private readonly IPlayerService _playerService;
    private readonly INavigationService _navigationService;

    public PlayerViewModel(IPlayerService playerService, INavigationService navigationService)
    {
        _playerService = playerService;
        _navigationService = navigationService;

        _playerService.StateChanged += (_, _) => UpdateFromPlayer();
        UpdateFromPlayer();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStation), nameof(StationInitial), nameof(StationSubtitle))]
    public partial StationDto? CurrentStation { get; set; }

    public bool HasStation => CurrentStation is not null;

    public string StationInitial => string.IsNullOrWhiteSpace(CurrentStation?.Name)
        ? "?"
        : CurrentStation.Name[..1].ToUpperInvariant();

    public string StationSubtitle => CurrentStation is null
        ? string.Empty
        : string.IsNullOrWhiteSpace(CurrentStation.Tags)
            ? CurrentStation.Country
            : $"{CurrentStation.Country} · {CurrentStation.Tags.Replace(",", ", ")}";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText), nameof(ToggleButtonText))]
    public partial bool IsPlaying { get; set; }

    public string StatusText => IsPlaying ? "Reproduciendo" : "Detenido";

    public string ToggleButtonText => IsPlaying ? "Detener" : "Reproducir";

    [RelayCommand]
    private void TogglePlayback()
    {
        if (IsPlaying)
            _playerService.Stop();
        else
            _playerService.Play();
    }

    [RelayCommand]
    private Task GoToHomeAsync() => _navigationService.GoToHomeAsync();

    private void UpdateFromPlayer()
    {
        CurrentStation = _playerService.CurrentStation;
        IsPlaying = _playerService.State == PlaybackState.Playing;
    }
}
