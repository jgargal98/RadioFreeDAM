using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RadioFreeDAM.ViewModels;

/// <summary>
/// Pantalla de una cadena (por ejemplo COPE): todas sus emisoras para elegir la de tu ciudad.
/// La cadena llega como parámetro de navegación desde Inicio.
/// </summary>
public partial class NetworkViewModel : ObservableObject, IQueryAttributable
{
    private readonly IStationApiService _stationApi;
    private readonly IPlayerService _player;
    private readonly INavigationService _navigation;
    private readonly IStationColorService _stationColors;
    private NetworkDto? _network;
    private int _requestVersion; // drops logo colors that arrive after the list was reloaded

    public NetworkViewModel(IStationApiService stationApi, IPlayerService player, INavigationService navigation,
        IStationColorService stationColors, IAccentService accent)
    {
        _stationApi = stationApi;
        _player = player;
        _navigation = navigation;
        _stationColors = stationColors;

        _player.StateChanged += (_, _) => RefreshPlaybackMarks();
        accent.AccentChanged += (_, _) => { foreach (var item in Stations) item.RefreshColors(); };
    }

    public ObservableCollection<StationItemViewModel> Stations { get; } = [];

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("network", out var value) && value is NetworkDto network)
        {
            _network = network;
            Title = network.Name;
            Stations.Clear(); // por si quedaban las de la cadena anterior
            LoadCommand.Execute(null);
        }
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (_network is null || IsLoading)
            return;

        IsLoading = true;
        ErrorMessage = null;
        var request = ++_requestVersion;
        try
        {
            var stations = await _stationApi.GetNetworkStationsAsync(_network.Id);

            Stations.Clear();
            foreach (var station in stations)
                Stations.Add(new StationItemViewModel(station));

            RefreshPlaybackMarks();
            _ = LoadCardColorsAsync(request);
        }
        catch (Exception ex) when (ApiException.IsCommunicationError(ex))
        {
            ErrorMessage = ApiException.ToCatalogMessage(ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SelectStationAsync(StationItemViewModel item)
    {
        _player.Select(item.Station!);
        await _navigation.GoToPlayerAsync();
    }

    /// <summary>Takes the color of each logo in the background, a few at a time, to tint the cards.</summary>
    private async Task LoadCardColorsAsync(int request)
    {
        using var limit = new SemaphoreSlim(4);

        await Task.WhenAll(Stations.ToList().Select(async item =>
        {
            await limit.WaitAsync();
            try
            {
                var color = await _stationColors.GetAccentColorAsync(item.Station!);
                if (color is not null && request == _requestVersion)
                    item.SetLogoColor(color);
            }
            finally
            {
                limit.Release();
            }
        }));
    }

    private void RefreshPlaybackMarks()
    {
        foreach (var item in Stations)
        {
            item.IsSelected = item.Represents(_player.CurrentStation);
            item.IsPlaying = item.IsSelected && _player.State == PlaybackState.Playing;
        }
    }
}
