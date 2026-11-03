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
    private readonly ISessionService _session;
    private readonly IFavoritesService _favorites;
    private readonly IAccentService _accent;

    public PlayerViewModel(
        IPlayerService playerService,
        INavigationService navigationService,
        ISessionService session,
        IFavoritesService favorites,
        IAccentService accent)
    {
        _playerService = playerService;
        _navigationService = navigationService;
        _session = session;
        _favorites = favorites;
        _accent = accent;

        _playerService.StateChanged += (_, _) => UpdateFromPlayer();
        // El acento cambia con la emisora, con la elección del usuario y con el tema.
        _accent.AccentChanged += (_, _) => UpdateColors();
        _favorites.FavoritesChanged += (_, _) => UpdateFavorite();
        _session.SessionChanged += (_, _) => UpdateFavorite();
        UpdateFromPlayer();
        UpdateColors();
    }

    /// <summary>Fondo del Reproductor: degradado con el color de acento (por defecto, el de la emisora).</summary>
    [ObservableProperty]
    public partial Brush? PlayerBackground { get; set; }

    /// <summary>Color de las ondas de fondo.</summary>
    [ObservableProperty]
    public partial Color WaveColor { get; set; } = AccentPalette.DefaultAccent;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FavoriteButtonText))]
    public partial bool IsFavorite { get; set; }

    public string FavoriteButtonText => IsFavorite ? "En favoritas" : "Añadir a favoritas";

    [ObservableProperty]
    public partial bool IsFavoriteBusy { get; set; }

    /// <summary>Error al guardar o quitar la favorita (se muestra bajo el botón).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasFavoriteError))]
    public partial string? FavoriteErrorMessage { get; set; }

    public bool HasFavoriteError => !string.IsNullOrEmpty(FavoriteErrorMessage);

    /// <summary>Corazón: sin sesión lleva a Iniciar sesión; con sesión añade o quita la favorita.</summary>
    [RelayCommand]
    private async Task ToggleFavoriteAsync()
    {
        if (CurrentStation is null || IsFavoriteBusy)
            return;

        FavoriteErrorMessage = null;

        if (!_session.IsLoggedIn)
        {
            await _navigationService.GoToLoginAsync();
            return;
        }

        IsFavoriteBusy = true;
        try
        {
            if (_favorites.IsFavorite(CurrentStation.Id))
                await _favorites.RemoveAsync(CurrentStation.Id);
            else
                await _favorites.AddAsync(CurrentStation);
        }
        catch (Exception ex) when (ApiException.IsCommunicationError(ex))
        {
            FavoriteErrorMessage = ApiException.ToUserMessage(ex);
        }
        finally
        {
            IsFavoriteBusy = false;
        }
    }

    private void UpdateFavorite()
    {
        IsFavorite = CurrentStation is not null && _favorites.IsFavorite(CurrentStation.Id);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStation), nameof(StationInitial), nameof(StationSubtitle),
        nameof(StationQuality))]
    public partial StationDto? CurrentStation { get; set; }

    public bool HasStation => CurrentStation is not null;

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

    /// <summary>Al tocar el mini reproductor se abre la pantalla completa.</summary>
    [RelayCommand]
    private Task GoToPlayerAsync() => _navigationService.GoToPlayerAsync();

    private void UpdateFromPlayer()
    {
        if (CurrentStation?.Id != _playerService.CurrentStation?.Id)
            FavoriteErrorMessage = null;

        CurrentStation = _playerService.CurrentStation;
        State = _playerService.State;
        ErrorMessage = _playerService.ErrorMessage;
        UpdateFavorite();
        UpdateColors();
    }

    private void UpdateColors()
    {
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        // Sin emisora, el fondo normal de la página.
        PlayerBackground = _playerService.CurrentStation is null ? null : PlayerColors.Background(_accent.Accent, isDark);
        WaveColor = PlayerColors.Waves(_accent.Accent, isDark);
    }
}