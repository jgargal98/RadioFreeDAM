using System.Net;

namespace RadioFreeDAM.Services;

public class FavoritesService : IFavoritesService
{
    private readonly IFavoritesApiService _api;
    private readonly ISessionService _session;
    private List<FavoriteDto> _favorites = [];

    public FavoritesService(IFavoritesApiService api, ISessionService session)
    {
        _api = api;
        _session = session;

        // Al cerrar sesión se olvidan las favoritas del usuario anterior.
        _session.SessionChanged += (_, _) =>
        {
            if (!_session.IsLoggedIn)
                SetFavorites([], isLoaded: false);
        };
    }

    public IReadOnlyList<FavoriteDto> Favorites => _favorites;

    public bool IsLoaded { get; private set; }

    public event EventHandler? FavoritesChanged;

    public bool IsFavorite(string stationId) => _favorites.Any(f => f.StationId == stationId);

    public async Task LoadAsync()
    {
        if (!_session.IsLoggedIn)
            return;

        var favorites = await CallApiAsync(_api.GetAllAsync);
        SetFavorites(favorites.ToList(), isLoaded: true);
    }

    public async Task AddAsync(StationDto station)
    {
        var added = await CallApiAsync(() => _api.AddAsync(station.Id));

        // La API devuelve la favorita con los datos de la emisora; se pone la primera (más reciente).
        added.Station ??= station;
        SetFavorites([added, .. _favorites.Where(f => f.StationId != station.Id)], IsLoaded);
    }

    public async Task RemoveAsync(string stationId)
    {
        try
        {
            await CallApiAsync(async () => { await _api.RemoveAsync(stationId); return true; });
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Ya no estaba en la base de datos: el resultado es el mismo que se quería.
        }

        SetFavorites(_favorites.Where(f => f.StationId != stationId).ToList(), IsLoaded);
    }

    /// <summary>Si la API dice 401, el token ya no vale: se cierra la sesión.</summary>
    private async Task<T> CallApiAsync<T>(Func<Task<T>> call)
    {
        try
        {
            return await call();
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            _session.SignOut();
            throw;
        }
    }

    private void SetFavorites(List<FavoriteDto> favorites, bool isLoaded)
    {
        _favorites = favorites;
        IsLoaded = isLoaded;
        FavoritesChanged?.Invoke(this, EventArgs.Empty);
    }
}
