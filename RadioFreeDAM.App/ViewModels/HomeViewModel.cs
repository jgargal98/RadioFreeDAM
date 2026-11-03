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
    private static readonly TimeSpan SearchDelay = TimeSpan.FromMilliseconds(500);

    private readonly IApiStatusService _apiStatusService;
    private readonly IStationApiService _stationApiService;
    private readonly IPlayerService _playerService;
    private readonly INavigationService _navigationService;
    private readonly IThemeService _themeService;
    private readonly IDialogService _dialogService;
    private bool _isInitialized;

    /// <summary>Texto de la búsqueda cuyos resultados se muestran; null si se muestran las populares.</summary>
    private string? _activeSearch;

    private CancellationTokenSource? _searchDelayCts;

    /// <summary>Número de la última carga pedida; las respuestas de cargas anteriores se ignoran.</summary>
    private int _requestVersion;

    public HomeViewModel(
        IApiStatusService apiStatusService,
        IStationApiService stationApiService,
        IPlayerService playerService,
        INavigationService navigationService,
        IThemeService themeService,
        IDialogService dialogService)
    {
        _apiStatusService = apiStatusService;
        _stationApiService = stationApiService;
        _playerService = playerService;
        _navigationService = navigationService;
        _themeService = themeService;
        _dialogService = dialogService;

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

    /// <summary>Texto cuando la lista queda vacía (por ejemplo, una búsqueda sin resultados).</summary>
    [ObservableProperty]
    public partial string? EmptyMessage { get; set; }

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

    /// <summary>Al pulsar Buscar (Enter): busca ya, sin esperar.</summary>
    [RelayCommand]
    private async Task SearchAsync()
    {
        _searchDelayCts?.Cancel();
        await SearchNowAsync(SearchText.Trim());
    }

    /// <summary>
    /// Búsqueda mientras se escribe: espera medio segundo sin cambios en el texto antes de buscar,
    /// para no lanzar una petición por cada letra.
    /// </summary>
    partial void OnSearchTextChanged(string value)
    {
        _searchDelayCts?.Cancel();
        var cts = _searchDelayCts = new CancellationTokenSource();

        Task.Delay(SearchDelay, cts.Token).ContinueWith(task =>
        {
            if (!task.IsCanceled)
                MainThread.BeginInvokeOnMainThread(async () => await SearchNowAsync(value.Trim()));
        }, TaskScheduler.Default);
    }

    private async Task SearchNowAsync(string text)
    {
        if (text.Length == 0)
        {
            // Búsqueda borrada: vuelven las populares.
            if (_activeSearch is not null || Stations.Count == 0)
                await LoadTopStationsAsync();
            return;
        }

        // Con una sola letra todavía no se busca (la API exige al menos 2).
        if (text.Length < MinSearchLength || text == _activeSearch)
            return;

        await LoadSearchAsync(text);
    }

    [RelayCommand]
    private Task LoadTopStationsAsync() =>
        LoadStationsAsync(_stationApiService.GetTopStationsAsync, search: null, title: "Populares en España");

    private Task LoadSearchAsync(string text) =>
        LoadStationsAsync(() => _stationApiService.SearchAsync(text), search: text, title: $"Resultados de «{text}»");

    /// <summary>
    /// Carga una lista de emisoras. Si mientras tanto empieza otra carga (el usuario sigue escribiendo),
    /// el resultado de esta se descarta: siempre se muestra la última búsqueda.
    /// </summary>
    private async Task LoadStationsAsync(Func<Task<IReadOnlyList<StationDto>>> fetch, string? search, string title)
    {
        var request = ++_requestVersion;
        IsLoading = true;
        ErrorMessage = null;
        EmptyMessage = null;

        try
        {
            var stations = await fetch();
            if (request != _requestVersion)
                return;

            IsServerOnline = true;
            _activeSearch = search;
            ListTitle = title;
            Stations.Clear();
            foreach (var station in stations)
                Stations.Add(new StationItemViewModel(station));

            EmptyMessage = search is null
                ? "La API no ha devuelto ninguna emisora."
                : $"No hay emisoras que coincidan con «{search}». Prueba con otro nombre.";

            RefreshPlaybackMarks();
        }
        catch (Exception ex) when (request == _requestVersion && ApiException.IsCommunicationError(ex))
        {
            ErrorMessage = ex switch
            {
                // No hay respuesta: la API está apagada o no es alcanzable.
                HttpRequestException { StatusCode: null } or Polly.CircuitBreaker.BrokenCircuitException =>
                    $"No se puede conectar con la API en {ApiConfig.BaseUrl}. Comprueba que está arrancada.",
                // La API responde, pero Radio Browser no.
                HttpRequestException { StatusCode: HttpStatusCode.BadGateway } =>
                    "El catálogo de emisoras (Radio Browser) no está disponible ahora mismo. Inténtalo de nuevo en unos minutos.",
                HttpRequestException =>
                    "No se ha podido cargar el catálogo de emisoras. Inténtalo de nuevo.",
                _ => ApiException.ToUserMessage(ex)
            };

            IsServerOnline = ex is HttpRequestException { StatusCode: null } or Polly.CircuitBreaker.BrokenCircuitException
                ? false
                : IsServerOnline;
        }
        finally
        {
            if (request == _requestVersion)
                IsLoading = false;
        }
    }

    /// <summary>Botón de tema de la cabecera: elegir entre el del sistema, claro u oscuro.</summary>
    [RelayCommand]
    private async Task ChangeThemeAsync()
    {
        var options = new Dictionary<string, ThemeChoice>
        {
            ["Usar el del sistema"] = ThemeChoice.System,
            ["Claro"] = ThemeChoice.Light,
            ["Oscuro"] = ThemeChoice.Dark
        };

        var chosen = await _dialogService.ChooseAsync("Tema de la app", [.. options.Keys]);
        if (chosen is not null)
            _themeService.Apply(options[chosen]);
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
