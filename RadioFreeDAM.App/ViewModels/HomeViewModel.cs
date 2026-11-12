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
    private readonly ISessionService _session;
    private readonly IDialogService _dialogService;
    private bool _isInitialized;

    /// <summary>Search text of the list being shown (at least 2 characters), or null when there is no search.</summary>
    private string? _query;

    private string _scope = CatalogFilters.ScopeSpain;
    private string _category = CatalogFilters.AllCategories;
    private string _sort = CatalogFilters.SortClicks;

    /// <summary>True while ResetFilters changes the sort itself, so the change does not start its own load.</summary>
    private bool _resetting;

    private CancellationTokenSource? _searchDelayCts;

    /// <summary>Number of the last load requested; answers of older loads are ignored.</summary>
    private int _requestVersion;

    public HomeViewModel(
        IApiStatusService apiStatusService,
        IStationApiService stationApiService,
        IPlayerService playerService,
        INavigationService navigationService,
        ISessionService session,
        IDialogService dialogService)
    {
        _apiStatusService = apiStatusService;
        _stationApiService = stationApiService;
        _playerService = playerService;
        _navigationService = navigationService;
        _session = session;
        _dialogService = dialogService;

        Scopes = [.. CatalogFilters.Scopes.Select(o => new FilterOptionViewModel(o))];
        Categories = [.. CatalogFilters.Categories.Select(o => new FilterOptionViewModel(o))];
        SortOptions = [.. CatalogFilters.Sorts];
        SelectedSort = SortOptions[0];
        UpdateSelections();

        _playerService.StateChanged += (_, _) => RefreshPlaybackMarks();
        _session.SessionChanged += (_, _) => OnPropertyChanged(nameof(Greeting));
    }

    public ObservableCollection<StationItemViewModel> Stations { get; } = [];

    // ---------- Greeting ----------

    /// <summary>"Buenas tardes, Juan" (or just the salutation without a session).</summary>
    public string Greeting
    {
        get
        {
            var hour = DateTime.Now.Hour;
            var salutation = hour switch { < 6 => "Buenas noches", < 14 => "Buenos días", < 21 => "Buenas tardes", _ => "Buenas noches" };
            return _session.CurrentUser is { } user ? $"{salutation}, {user.Name.Split(' ')[0]}" : salutation;
        }
    }

    // ---------- Filters ----------

    public IReadOnlyList<FilterOptionViewModel> Scopes { get; }

    public IReadOnlyList<FilterOptionViewModel> Categories { get; }

    // A concrete list: Picker.ItemsSource needs an IList, which IReadOnlyList does not guarantee.
    public List<FilterOption> SortOptions { get; }

    /// <summary>Bound to the sort picker. Changing it reloads the list.</summary>
    // Nullable on purpose: a Picker sets SelectedItem to null while it is being built or rebuilt.
    // The sort actually used is always _sort, which never becomes null.
    [ObservableProperty]
    public partial FilterOption? SelectedSort { get; set; }

    [ObservableProperty]
    public partial bool HasActiveFilters { get; set; }

    [RelayCommand]
    private async Task SelectScopeAsync(FilterOptionViewModel option)
    {
        if (option.Id == _scope)
            return;

        _scope = option.Id;
        await ApplyFiltersAsync();
    }

    [RelayCommand]
    private async Task SelectCategoryAsync(FilterOptionViewModel option)
    {
        if (option.Id == _category)
            return;

        _category = option.Id;
        await ApplyFiltersAsync();
    }

    partial void OnSelectedSortChanged(FilterOption? value)
    {
        // The picker sends null while it is built and a real option when the user chooses one.
        if (value is null || value.Id == _sort)
            return;

        _sort = value.Id;
        if (!_resetting)
            _ = ApplyFiltersAsync();
    }

    /// <summary>Opens the list of sort options.</summary>
    [RelayCommand]
    private async Task ChooseSortAsync()
    {
        var chosen = await _dialogService.ChooseAsync("Ordenar por", [.. SortOptions.Select(o => o.Name)]);
        var option = SortOptions.FirstOrDefault(o => o.Name == chosen);
        if (option is not null)
            SelectedSort = option;
    }

    [RelayCommand]
    private async Task ResetFiltersAsync()
    {
        _scope = CatalogFilters.ScopeSpain;
        _category = CatalogFilters.AllCategories;
        _resetting = true;
        _sort = CatalogFilters.SortClicks;
        SelectedSort = SortOptions[0];
        _resetting = false;
        await ApplyFiltersAsync();
    }

    private Task ApplyFiltersAsync()
    {
        UpdateSelections();
        return LoadAsync();
    }

    private void UpdateSelections()
    {
        foreach (var option in Scopes)
            option.IsSelected = option.Id == _scope;
        foreach (var option in Categories)
            option.IsSelected = option.Id == _category;

        HasActiveFilters = CurrentFilter.HasActiveFilters;
    }

    private CatalogFilter CurrentFilter => new(_query, _scope, _category, _sort);

    // ---------- State ----------

    [ObservableProperty]
    public partial string ListTitle { get; set; } = "Top España";

    /// <summary>Second line of the list header: how it is sorted and how many rows there are.</summary>
    [ObservableProperty]
    public partial string ListSubtitle { get; set; } = string.Empty;

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

            ListTitle = BuildTitle(filter);
            ListSubtitle = BuildSubtitle(filter, entries.Count);
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

    private static string BuildTitle(CatalogFilter filter)
    {
        var scope = filter.Scope == CatalogFilters.ScopeSpain ? "España" : "Mundo";
        var title = filter.Name is null ? $"Top {scope}" : $"Resultados de «{filter.Name}»";

        if (filter.Category != CatalogFilters.AllCategories)
            title += $" · {CatalogFilters.Categories.First(c => c.Id == filter.Category).Name}";

        return title;
    }

    private static string BuildSubtitle(CatalogFilter filter, int rows)
    {
        var sort = CatalogFilters.Sorts.First(s => s.Id == filter.Sort).Name;
        return rows == 0 ? sort : $"{rows} resultados · {sort}";
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
