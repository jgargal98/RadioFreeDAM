using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RadioFreeDAM.ViewModels;

/// <summary>
/// Home screen: checks the server, loads the station list (filtered by scope, category and sort order),
/// searches by name and lets the user pick a station or a network.
/// </summary>
public partial class HomeViewModel : ObservableObject
{
    private const int MinSearchLength = 2;
    private static readonly TimeSpan SearchDelay = TimeSpan.FromMilliseconds(500);

    private readonly IApiStatusService _apiStatusService;
    private readonly IStationApiService _stationApiService;
    private readonly IPlayerService _playerService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private bool _isInitialized;

    /// <summary>Search text of the list being shown (at least 2 characters), or null when there is no search.</summary>
    private string? _query;

    private string _scope = CatalogFilters.ScopeSpain;
    private string _category = CatalogFilters.AllCategories;
    private string _sort = CatalogFilters.SortClicks;

    private CancellationTokenSource? _searchDelayCts;

    /// <summary>Number of the last load requested; answers of older loads are ignored.</summary>
    private int _requestVersion;

    public HomeViewModel(
        IApiStatusService apiStatusService,
        IStationApiService stationApiService,
        IPlayerService playerService,
        INavigationService navigationService,
        IDialogService dialogService)
    {
        _apiStatusService = apiStatusService;
        _stationApiService = stationApiService;
        _playerService = playerService;
        _navigationService = navigationService;
        _dialogService = dialogService;

        UpdateFilterTexts();
        _playerService.StateChanged += (_, _) => RefreshPlaybackMarks();
    }

    public ObservableCollection<StationItemViewModel> Stations { get; } = [];

    // ---------- Filters: three drop-down menus (scope, category, sort) ----------

    /// <summary>Text of each drop-down button: the option that is currently chosen.</summary>
    [ObservableProperty]
    public partial string ScopeText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CategoryText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SortText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasActiveFilters { get; set; }

    [RelayCommand]
    private Task ChooseScopeAsync() => ChooseAsync("Ámbito", CatalogFilters.Scopes, _scope, id => _scope = id);

    [RelayCommand]
    private Task ChooseCategoryAsync() => ChooseAsync("Categoría", CatalogFilters.Categories, _category, id => _category = id);

    [RelayCommand]
    private Task ChooseSortAsync() => ChooseAsync("Ordenar por", CatalogFilters.Sorts, _sort, id => _sort = id);

    /// <summary>Shows the options of a filter; if the user picks a different one, stores it and reloads the list.</summary>
    private async Task ChooseAsync(string title, IReadOnlyList<FilterOption> options, string current, Action<string> apply)
    {
        var chosen = await _dialogService.ChooseAsync(title, [.. options.Select(o => o.Name)]);
        var option = options.FirstOrDefault(o => o.Name == chosen);
        if (option is null || option.Id == current)
            return;

        apply(option.Id);
        UpdateFilterTexts();
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ResetFiltersAsync()
    {
        _scope = CatalogFilters.ScopeSpain;
        _category = CatalogFilters.AllCategories;
        _sort = CatalogFilters.SortClicks;
        UpdateFilterTexts();
        await LoadAsync();
    }

    private void UpdateFilterTexts()
    {
        ScopeText = CatalogFilters.Scopes.First(o => o.Id == _scope).Name;
        CategoryText = CatalogFilters.Categories.First(o => o.Id == _category).Name;
        SortText = CatalogFilters.Sorts.First(o => o.Id == _sort).Name;
        HasActiveFilters = CurrentFilter.HasActiveFilters;
    }

    private CatalogFilter CurrentFilter => new(_query, _scope, _category, _sort);
    // ---------- State ----------

    /// <summary>The single line above the list, for example "Top España · Música · 20 resultados".</summary>
    [ObservableProperty]
    public partial string ListSummary { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>Text shown when the list is empty (for example a search without results).</summary>
    [ObservableProperty]
    public partial string? EmptyMessage { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    /// <summary>null = not checked yet; true/false = result of the last check.</summary>
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

    /// <summary>Called when the screen appears; only loads the first time.</summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        _isInitialized = true;
        await RefreshAsync();
    }

    /// <summary>Reload button: checks the server again and repeats the last load.</summary>
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

        await LoadAsync();
    }

    // ---------- Search ----------

    /// <summary>On Search (Enter): searches right away, without waiting.</summary>
    [RelayCommand]
    private async Task SearchAsync()
    {
        _searchDelayCts?.Cancel();
        await SearchNowAsync(SearchText.Trim());
    }

    /// <summary>
    /// Search while typing: waits half a second without changes in the text before searching,
    /// so a request is not sent for every letter.
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
        // With a single letter it does not search yet (the API requires at least 2).
        if (text.Length == 1)
            return;

        var query = text.Length >= MinSearchLength ? text : null;
        if (query == _query && Stations.Count > 0)
            return;

        _query = query;
        await LoadAsync();
    }

    // ---------- Loading ----------

    /// <summary>
    /// Loads the list for the current search and filters. If another load starts meanwhile
    /// (the user keeps typing or changes a filter), the result of this one is discarded.
    /// </summary>
    private async Task LoadAsync()
    {
        var request = ++_requestVersion;
        var filter = CurrentFilter;
        IsLoading = true;
        ErrorMessage = null;
        EmptyMessage = null;

        try
        {
            var entries = await _stationApiService.GetCatalogAsync(filter);
            if (request != _requestVersion)
                return;

            IsServerOnline = true;
            Stations.Clear();
            foreach (var entry in entries)
                Stations.Add(new StationItemViewModel(entry));

            ListSummary = BuildSummary(filter, entries.Count);
            EmptyMessage = filter.Name is not null
                ? $"No hay emisoras que coincidan con «{filter.Name}». Prueba con otro nombre o cambia los filtros."
                : "No hay emisoras con estos filtros. Prueba con otra categoría o con Mundo.";

            RefreshPlaybackMarks();
        }
        catch (Exception ex) when (request == _requestVersion && ApiException.IsCommunicationError(ex))
        {
            ErrorMessage = ApiException.ToCatalogMessage(ex);
            if (ApiException.IsConnectionFailure(ex))
                IsServerOnline = false;
        }
        finally
        {
            if (request == _requestVersion)
                IsLoading = false;
        }
    }

    private static string BuildSummary(CatalogFilter filter, int rows)
    {
        var scope = filter.Scope == CatalogFilters.ScopeSpain ? "España" : "Mundo";
        var title = filter.Name is null ? $"Top {scope}" : $"«{filter.Name}»";

        if (filter.Category != CatalogFilters.AllCategories)
            title += $" · {CatalogFilters.Categories.First(c => c.Id == filter.Category).Name}";

        return rows == 0 ? title : $"{title} · {rows} resultados";
    }

    // ---------- Navigation ----------

    /// <summary>Gear of the header.</summary>
    [RelayCommand]
    private Task OpenSettingsAsync() => _navigationService.GoToSettingsAsync();

    [RelayCommand]
    private async Task SelectStationAsync(StationItemViewModel item)
    {
        // A network opens the list of its stations to choose a city; a station goes to the Player.
        if (item.Network is { } network)
        {
            await _navigationService.GoToNetworkAsync(network);
            return;
        }

        _playerService.Select(item.Station!);
        await _navigationService.GoToPlayerAsync();
    }

    private void RefreshPlaybackMarks()
    {
        var current = _playerService.CurrentStation;
        foreach (var item in Stations)
        {
            item.IsSelected = item.Represents(current);
            item.IsPlaying = item.IsSelected && _playerService.State == PlaybackState.Playing;
        }
    }
}
