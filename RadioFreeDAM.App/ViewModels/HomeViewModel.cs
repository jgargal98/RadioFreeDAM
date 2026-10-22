using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RadioFreeDAM.ViewModels;

/// <summary>
/// Pantalla de Inicio: comprueba el servidor, carga las emisoras y permite elegir una.
/// </summary>
public partial class HomeViewModel : ObservableObject
{
    private readonly IApiStatusService _apiStatusService;
    private readonly IStationApiService _stationApiService;
    private readonly IPlayerService _playerService;
    private readonly INavigationService _navigationService;
    private bool _isInitialized;

    public HomeViewModel(
        IApiStatusService apiStatusService,
        IStationApiService stationApiService,
        IPlayerService playerService,
        INavigationService navigationService)
    {
        _apiStatusService = apiStatusService;
        _stationApiService = stationApiService;
        _playerService = playerService;
        _navigationService = navigationService;

        _playerService.StateChanged += (_, _) => RefreshPlaybackMarks();
    }

    public ObservableCollection<StationItemViewModel> Stations { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    /// <summary>null = aún no comprobado; true/false = resultado de la última comprobación.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ServerStatusText), nameof(IsServerOk), nameof(IsServerDown))]
    public partial bool? IsServerOnline { get; set; }

    public bool IsServerOk => IsServerOnline == true;

    public bool IsServerDown => IsServerOnline == false;

    public string ServerStatusText => IsServerOnline switch
    {
        true => "Servidor conectado",
        false => "Sin conexión con el servidor",
        null => "Comprobando servidor…"
    };

    /// <summary>Se llama al mostrar la pantalla; solo carga la primera vez.</summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        _isInitialized = true;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsLoading)
            return;

        IsLoading = true;
        ErrorMessage = null;
        IsServerOnline = null;

        try
        {
            IsServerOnline = await _apiStatusService.IsServerAvailableAsync();
            if (IsServerOnline != true)
            {
                ErrorMessage = $"No se puede conectar con la API en {ApiConfig.BaseUrl}. Comprueba que está arrancada.";
                return;
            }

            var stations = await _stationApiService.GetStationsAsync();

            Stations.Clear();
            foreach (var station in stations)
                Stations.Add(new StationItemViewModel(station));

            RefreshPlaybackMarks();

            if (Stations.Count == 0)
                ErrorMessage = "La API no ha devuelto ninguna emisora.";
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            ErrorMessage = "No se ha podido cargar el catálogo de emisoras. Inténtalo de nuevo.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SelectStationAsync(StationItemViewModel item)
    {
        _playerService.Select(item.Station);
        await _navigationService.GoToPlayerAsync();
    }

    private void RefreshPlaybackMarks()
    {
        var current = _playerService.CurrentStation;
        foreach (var item in Stations)
        {
            item.IsSelected = item.Station.Id == current?.Id;
            item.IsPlaying = item.IsSelected && _playerService.State == PlaybackState.Playing;
        }
    }
}
