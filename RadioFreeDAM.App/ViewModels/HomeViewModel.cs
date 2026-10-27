using System.Collections.ObjectModel;
using System.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RadioFreeDAM.ViewModels;

/// <summary>
/// Pantalla de Inicio: comprueba el servidor, carga las emisoras populares, busca por nombre
/// y permite elegir una emisora.
/// </summary>
public partial class HomeViewModel : ObservableObject
{
    private const int MinSearchLength = 2;

    private readonly IApiStatusService _apiStatusService;
    private readonly IStationApiService _stationApiService;
    private readonly IPlayerService _playerService;
    private readonly INavigationService _navigationService;
    private bool _isInitialized;

    /// <summary>Texto de la búsqueda cuyos resultados se muestran; null si se muestran las populares.</summary>
    private string? _activeSearch;

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
    public partial string ListTitle { get; set; } = "Populares en España";

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

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
        await RefreshAsync();
    }

    /// <summary>Botón Recargar: vuelve a comprobar el servidor y repite la última carga.</summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsLoading)
            return;

        IsServerOnline = null;
        IsServerOnline = await _apiStatusService.IsServerAvailableAsync();
        if (IsServerOnline != true)
        {
            ErrorMessage = $"No se puede conectar con la API en {ApiConfig.BaseUrl}. Comprueba que está arrancada.";
            return;
        }

        if (_activeSearch is null)
            await LoadTopStationsAsync();
        else
            await LoadSearchAsync(_activeSearch);
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        var text = SearchText.Trim();

        if (text.Length == 0)
        {
            await LoadTopStationsAsync();
            return;
        }

        if (text.Length < MinSearchLength)
        {
            ErrorMessage = $"Escribe al menos {MinSearchLength} caracteres para buscar.";
            return;
        }

        await LoadSearchAsync(text);
    }

    /// <summary>Al borrar la búsqueda se vuelve a las emisoras populares.</summary>
    partial void OnSearchTextChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value) && _activeSearch is not null)
            LoadTopStationsCommand.Execute(null);
    }

    [RelayCommand]
    private Task LoadTopStationsAsync() =>
        LoadStationsAsync(_stationApiService.GetTopStationsAsync, search: null, title: "Populares en España");

    private Task LoadSearchAsync(string text) =>
        LoadStationsAsync(() => _stationApiService.SearchAsync(text), search: text, title: $"Resultados de «{text}»");

    private async Task LoadStationsAsync(Func<Task<IReadOnlyList<StationDto>>> fetch, string? search, string title)
    {
        if (IsLoading)
            return;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var stations = await fetch();
            IsServerOnline = true;

            _activeSearch = search;
            ListTitle = title;
            Stations.Clear();
            foreach (var station in stations)
                Stations.Add(new StationItemViewModel(station));

            RefreshPlaybackMarks();
        }
        catch (HttpRequestException ex) when (ex.StatusCode is null)
        {
            // No hay respuesta: la API está apagada o no es alcanzable.
            IsServerOnline = false;
            ErrorMessage = $"No se puede conectar con la API en {ApiConfig.BaseUrl}. Comprueba que está arrancada.";
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.BadGateway)
        {
            // La API responde, pero Radio Browser no.
            IsServerOnline = true;
            ErrorMessage = "El catálogo de emisoras (Radio Browser) no está disponible ahora mismo. Inténtalo de nuevo en unos minutos.";
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "No se ha podido cargar el catálogo de emisoras. Inténtalo de nuevo.";
        }
        catch (TaskCanceledException)
        {
            ErrorMessage = "El servidor está tardando demasiado en responder. Inténtalo de nuevo.";
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
