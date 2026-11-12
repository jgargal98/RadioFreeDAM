using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RadioFreeDAM.Shared.Dtos;

namespace RadioFreeDAM.Api.Services;

/// <summary>
/// Caché en memoria delante del catálogo real (patrón decorador): implementa la misma interfaz
/// y envuelve a otro IStationService, así que el controlador no sabe que existe.
///
/// - Si hay un resultado reciente, lo devuelve sin llamar a Radio Browser.
/// - Si ha caducado, vuelve a pedirlo.
/// - Si Radio Browser falla y hay un resultado antiguo guardado, devuelve ese en lugar del error.
///
/// Además guarda cada emisora por su Id, para que las favoritas que ya aparecieron
/// en Inicio o en una búsqueda no necesiten otra llamada.
/// </summary>
public class CachedStationService(
    IStationService inner,
    IMemoryCache cache,
    IOptions<StationCacheOptions> options,
    TimeProvider timeProvider,
    ILogger<CachedStationService> logger) : IStationService
{
    private readonly StationCacheOptions _options = options.Value;

    public Task<IReadOnlyList<StationDto>> GetTopStationsAsync(CancellationToken cancellationToken = default) =>
        GetOrFetchAsync(
            "stations:top",
            _options.TopStationsDuration,
            () => inner.GetTopStationsAsync(cancellationToken));

    public Task<IReadOnlyList<StationDto>> SearchByNameAsync(string name, CancellationToken cancellationToken = default) =>
        GetOrFetchAsync(
            // "Jazz" y "jazz " son la misma búsqueda.
            $"stations:search:{name.Trim().ToLowerInvariant()}",
            _options.SearchDuration,
            () => inner.SearchByNameAsync(name, cancellationToken));

    public Task<IReadOnlyList<StationDto>> QueryAsync(StationQuery query, CancellationToken cancellationToken = default) =>
        GetOrFetchAsync(
            $"stations:query:{query.Name?.Trim().ToLowerInvariant()}|{query.CountryOnly}|{string.Join(',', query.Tags)}|{query.Sort}|{query.Limit}",
            // Name searches change more often than the plain top lists.
            string.IsNullOrWhiteSpace(query.Name) ? _options.TopStationsDuration : _options.SearchDuration,
            () => inner.QueryAsync(query, cancellationToken));

    public async Task<IReadOnlyList<StationDto>> GetByIdsAsync(
        IReadOnlyCollection<string> ids,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var found = new Dictionary<string, StationDto>();
        var stale = new Dictionary<string, StationDto>();
        var missing = new List<string>();

        foreach (var id in ids.Distinct())
        {
            if (cache.TryGetValue(StationKey(id), out StationEntry? saved) && saved is not null)
            {
                if (now - saved.FetchedAt < _options.TopStationsDuration)
                {
                    found[id] = saved.Station;
                    continue;
                }
                stale[id] = saved.Station;
            }
            missing.Add(id);
        }

        if (missing.Count > 0)
        {
            try
            {
                var fetched = await inner.GetByIdsAsync(missing, cancellationToken);
                StoreStations(fetched, now);
                foreach (var station in fetched)
                    found[station.Id] = station;
            }
            catch (StationCatalogUnavailableException) when (missing.All(stale.ContainsKey))
            {
                logger.LogWarning("Radio Browser no responde; se devuelven copias guardadas de {Count} emisoras", missing.Count);
                foreach (var id in missing)
                    found[id] = stale[id];
            }
        }

        // Mismo orden en que se pidieron.
        return ids.Distinct().Where(found.ContainsKey).Select(id => found[id]).ToList();
    }

    private async Task<IReadOnlyList<StationDto>> GetOrFetchAsync(
        string key,
        TimeSpan freshDuration,
        Func<Task<IReadOnlyList<StationDto>>> fetch)
    {
        var now = timeProvider.GetUtcNow();
        cache.TryGetValue(key, out ListEntry? saved);

        if (saved is not null && now - saved.FetchedAt < freshDuration)
        {
            logger.LogDebug("Emisoras servidas desde caché ({Key})", key);
            return saved.Stations;
        }

        try
        {
            var stations = await fetch();
            cache.Set(key, new ListEntry(stations, now), _options.FallbackDuration);
            StoreStations(stations, now);
            return stations;
        }
        catch (StationCatalogUnavailableException) when (saved is not null)
        {
            logger.LogWarning(
                "Radio Browser no responde; se devuelve la copia guardada de {Key} ({Minutes:F0} min de antigüedad)",
                key, (now - saved.FetchedAt).TotalMinutes);
            return saved.Stations;
        }
    }

    private void StoreStations(IEnumerable<StationDto> stations, DateTimeOffset fetchedAt)
    {
        foreach (var station in stations)
            cache.Set(StationKey(station.Id), new StationEntry(station, fetchedAt), _options.FallbackDuration);
    }

    private static string StationKey(string id) => $"station:{id}";

    /// <summary>Resultado de una consulta (populares o búsqueda) y cuándo se obtuvo.</summary>
    private sealed record ListEntry(IReadOnlyList<StationDto> Stations, DateTimeOffset FetchedAt);

    /// <summary>Una emisora suelta y cuándo se obtuvo.</summary>
    private sealed record StationEntry(StationDto Station, DateTimeOffset FetchedAt);
}
