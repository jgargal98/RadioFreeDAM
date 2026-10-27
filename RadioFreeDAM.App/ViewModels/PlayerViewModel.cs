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
    [NotifyPropertyChangedFor(nameof(HasStation), nameof(StationInitial), nameof(StationSubtitle),
        nameof(StationQuality), nameof(HasFavicon))]
    public partial StationDto? CurrentStation { get; set; }

    public bool HasStation => CurrentStation is not null;

    public bool HasFavicon => !string.IsNullOrWhiteSpace(CurrentStation?.FaviconUrl);

    public string StationInitial => string.IsNullOrWhiteSpace(CurrentStation?.Name)
        ? "?"
        : CurrentStation.Name[..1].ToUpperInvariant();

    public string StationSubtitle => CurrentStation is null
        ? string.Empty
        : StationText.Describe(CurrentStation.Country, CurrentStation.Tags, maxTags: 4);

    /// <summary>Por ejemplo "MP3 · 128 kbps". Vacío si Radio Browser no lo indica.</summary>
    public string StationQuality
    {
        get
        {
            if (CurrentStation is null)
                return string.Empty;

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(CurrentStation.Codec))
                parts.Add(CurrentStation.Codec);
            if (CurrentStation.Bitrate > 0)
                parts.Add($"{CurrentStation.Bitrate} kbps");
            return string.Join(" · ", parts);
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPlaying), nameof(IsLoading), nameof(IsActive), nameof(HasError),
        nameof(StatusText), nameof(ToggleButtonText))]
    public partial PlaybackState State { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public bool IsPlaying => State == PlaybackState.Playing;

    public bool IsLoading => State == PlaybackState.Loading;

    /// <summary>Sonando o conectando: el botón principal sirve para detener.</summary>
    public bool IsActive => State is PlaybackState.Playing or PlaybackState.Loading;

    public bool HasError => State == PlaybackState.Error;

    public string StatusText => State switch
    {
        PlaybackState.Loading => "Conectando…",
        PlaybackState.Playing => "Reproduciendo",
        PlaybackState.Error => "Error de reproducción",
        _ => "Detenido"
    };

    public string ToggleButtonText => IsActive ? "Detener" : "Reproducir";

    [RelayCommand]
    private void TogglePlayback()
    {
        if (IsActive)
            _playerService.Stop();
        else
            _playerService.Play();
    }

    [RelayCommand]
    private Task GoToHomeAsync() => _navigationService.GoToHomeAsync();

    private void UpdateFromPlayer()
    {
        CurrentStation = _playerService.CurrentStation;
        State = _playerService.State;
        ErrorMessage = _playerService.ErrorMessage;
    }
}
