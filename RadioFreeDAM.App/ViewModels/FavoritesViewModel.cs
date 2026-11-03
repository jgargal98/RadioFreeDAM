using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RadioFreeDAM.ViewModels;

/// <summary>
/// Pantalla Favoritos. Sin sesión invita a iniciarla; con sesión muestra las favoritas guardadas
/// en la base de datos y permite reproducirlas y quitarlas.
/// </summary>
public partial class FavoritesViewModel : ObservableObject
{
    private readonly ISessionService _session;
    private readonly IFavoritesService _favorites;
    private readonly IPlayerService _player;
    private readonly INavigationService _navigation;

    public FavoritesViewModel(
        ISessionService session,
        IFavoritesService favorites,
        IPlayerService player,
        INavigationService navigation)
    {
        _session = session;
        _favorites = favorites;
        _player = player;
        _navigation = navigation;

        _session.SessionChanged += (_, _) => UpdateSession();
        _favorites.FavoritesChanged += (_, _) => RebuildItems();
        _player.StateChanged += (_, _) => RefreshPlaybackMarks();

        UpdateSession();
        RebuildItems();
    }

    public ObservableCollection<FavoriteItemViewModel> Items { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLoggedOut))]
    public partial bool IsLoggedIn { get; set; }

    public bool IsLoggedOut => !IsLoggedIn;

    [ObservableProperty]
    public partial string UserText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>Al mostrar la pantalla: si hay sesión y aún no se han cargado, se cargan.</summary>
    public async Task InitializeAsync()
    {
        if (_session.IsLoggedIn && !_favorites.IsLoaded)
            await LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsLoading || !_session.IsLoggedIn)
            return;

        IsLoading = true;
        ErrorMessage = null;
        try
        {
            await _favorites.LoadAsync();
        }
        catch (Exception ex) when (ApiException.IsCommunicationError(ex))
        {
            ErrorMessage = ApiException.ToUserMessage(ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task PlayAsync(FavoriteItemViewModel item)
    {
        if (item.Favorite.Station is null)
            return;

        _player.Select(item.Favorite.Station);
        await _navigation.GoToPlayerAsync();
    }

    [RelayCommand]
    private async Task RemoveAsync(FavoriteItemViewModel item)
    {
        ErrorMessage = null;
        try
        {
            await _favorites.RemoveAsync(item.StationId);
        }
        catch (Exception ex) when (ApiException.IsCommunicationError(ex))
        {
            ErrorMessage = ApiException.ToUserMessage(ex);
        }
    }

    [RelayCommand]
    private Task LoginAsync() => _navigation.GoToLoginAsync();

    /// <summary>Engranaje de la cabecera (allí se cierra la sesión).</summary>
    [RelayCommand]
    private Task OpenSettingsAsync() => _navigation.GoToSettingsAsync();

    private void UpdateSession()
    {
        IsLoggedIn = _session.IsLoggedIn;
        UserText = _session.CurrentUser is { } user ? $"{user.Name} · {user.Email}" : string.Empty;
        ErrorMessage = null;
    }

    private void RebuildItems()
    {
        Items.Clear();
        foreach (var favorite in _favorites.Favorites)
            Items.Add(new FavoriteItemViewModel(favorite));

        RefreshPlaybackMarks();
    }

    private void RefreshPlaybackMarks()
    {
        foreach (var item in Items)
        {
            item.IsPlaying = item.StationId == _player.CurrentStation?.Id && _player.State == PlaybackState.Playing;
        }
    }
}
