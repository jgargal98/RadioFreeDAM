using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RadioFreeDAM.Api.Services;
using RadioFreeDAM.Shared.Dtos;

namespace RadioFreeDAM.Api.Tests;

public class CachedStationServiceTests
{
    private readonly FakeClock _clock = new();
    private readonly CountingCatalog _catalog = new();
    private readonly CachedStationService _cache;

    public CachedStationServiceTests()
    {
        _cache = new CachedStationService(
            _catalog,
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new StationCacheOptions()),
            _clock,
            NullLogger<CachedStationService>.Instance);
    }

    [Fact]
    public async Task SegundaLlamadaReciente_NoVaARadioBrowser()
    {
        await _cache.GetTopStationsAsync();
        await _cache.GetTopStationsAsync();

        Assert.Equal(1, _catalog.Calls);
    }

    [Fact]
    public async Task BusquedasIguales_IgnoranMayusculasYEspacios()
    {
        await _cache.SearchByNameAsync("Jazz");
        await _cache.SearchByNameAsync(" jazz ");

        Assert.Equal(1, _catalog.Calls);
    }

    [Fact]
    public async Task AlCaducar_VuelveAPedirlo()
    {
        await _cache.GetTopStationsAsync();
        _clock.Advance(TimeSpan.FromMinutes(11));
        _catalog.Version = 2;

        var stations = await _cache.GetTopStationsAsync();

        Assert.Equal(2, _catalog.Calls);
        Assert.Equal("v2", stations[0].Name);
    }

    [Fact]
    public async Task SiRadioBrowserFalla_DevuelveLaUltimaCopia()
    {
        await _cache.GetTopStationsAsync();
        _clock.Advance(TimeSpan.FromMinutes(11));
        _catalog.Fail = true;

        var stations = await _cache.GetTopStationsAsync();

        Assert.Equal("v1", stations[0].Name);
    }

    [Fact]
    public async Task SiRadioBrowserFalla_YNoHayCopia_DaElError()
    {
        _catalog.Fail = true;

        await Assert.ThrowsAsync<StationCatalogUnavailableException>(() => _cache.SearchByNameAsync("jazz"));
    }

    [Fact]
    public async Task EmisorasYaVistas_NoNecesitanOtraLlamadaParaBuscarlasPorId()
    {
        await _cache.GetTopStationsAsync();

        var stations = await _cache.GetByIdsAsync(["top-1"]);

        Assert.Equal(1, _catalog.Calls);
        Assert.Equal("top-1", Assert.Single(stations).Id);
    }

    private sealed class FakeClock : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.UtcNow;

        public void Advance(TimeSpan time) => _now += time;

        public override DateTimeOffset GetUtcNow() => _now;
    }

    private sealed class CountingCatalog : IStationService
    {
        public int Calls { get; private set; }

        public int Version { get; set; } = 1;

        public bool Fail { get; set; }

        private IReadOnlyList<StationDto> Next()
        {
            Calls++;
            if (Fail)
                throw new StationCatalogUnavailableException("Radio Browser caído");
            return [new StationDto { Id = "top-1", Name = $"v{Version}" }];
        }

        public Task<IReadOnlyList<StationDto>> GetTopStationsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Next());

        public Task<IReadOnlyList<StationDto>> SearchByNameAsync(string name, CancellationToken cancellationToken = default) =>
            Task.FromResult(Next());

        public Task<IReadOnlyList<StationDto>> QueryAsync(StationQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(Next());

        public Task<IReadOnlyList<StationDto>> GetByIdsAsync(IReadOnlyCollection<string> ids, CancellationToken cancellationToken = default) =>
            Task.FromResult(Next());
    }
}
