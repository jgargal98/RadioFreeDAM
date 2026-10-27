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

    private async Task<IReadOnlyList<StationDto>> GetOrFetchAsync(
        string key,
        TimeSpan freshDuration,
        Func<Task<IReadOnlyList<StationDto>>> fetch)
    {
        var now = timeProvider.GetUtcNow();
        cache.TryGetValue(key, out CacheEntry? saved);

        if (saved is not null && now - saved.FetchedAt < freshDuration)
        {
            logger.LogDebug("Emisoras servidas desde caché ({Key})", key);
            return saved.Stations;
        }

        try
        {
            var stations = await fetch();
            cache.Set(key, new CacheEntry(stations, now), _options.FallbackDuration);
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

    /// <summary>Lo que se guarda en caché: las emisoras y cuándo se obtuvieron.</summary>
    private sealed record CacheEntry(IReadOnlyList<StationDto> Stations, DateTimeOffset FetchedAt);
}
