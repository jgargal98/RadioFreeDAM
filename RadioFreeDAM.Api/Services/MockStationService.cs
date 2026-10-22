using RadioFreeDAM.Shared.Dtos;

namespace RadioFreeDAM.Api.Services;

/// <summary>
/// Catálogo de prueba para la Fase 1. Las emisoras son ficticias y sus StreamUrl no suenan:
/// todavía no se reproduce audio real.
/// </summary>
public class MockStationService : IStationService
{
    private static readonly IReadOnlyList<StationDto> Stations =
    [
        new() { Id = "mock-01", Name = "DAM FM", Country = "España", Tags = "pop,éxitos", StreamUrl = "https://stream.example.com/dam-fm" },
        new() { Id = "mock-02", Name = "Radio Compilador", Country = "España", Tags = "rock,clásicos", StreamUrl = "https://stream.example.com/compilador" },
        new() { Id = "mock-03", Name = "Onda Binaria", Country = "España", Tags = "electrónica", StreamUrl = "https://stream.example.com/onda-binaria" },
        new() { Id = "mock-04", Name = "Jazz en la Nube", Country = "Francia", Tags = "jazz,lounge", StreamUrl = "https://stream.example.com/jazz-nube" },
        new() { Id = "mock-05", Name = "Clásica Async", Country = "Austria", Tags = "clásica", StreamUrl = "https://stream.example.com/clasica-async" },
        new() { Id = "mock-06", Name = "Noticias 24/7", Country = "España", Tags = "noticias,actualidad", StreamUrl = "https://stream.example.com/noticias" },
        new() { Id = "mock-07", Name = "Indie Stack", Country = "Reino Unido", Tags = "indie,alternativa", StreamUrl = "https://stream.example.com/indie-stack" },
        new() { Id = "mock-08", Name = "Latino Loop", Country = "México", Tags = "latina,reguetón", StreamUrl = "https://stream.example.com/latino-loop" },
        new() { Id = "mock-09", Name = "Lo-Fi Debug", Country = "Japón", Tags = "lofi,chill", StreamUrl = "https://stream.example.com/lofi-debug" },
        new() { Id = "mock-10", Name = "Deportes Directo", Country = "España", Tags = "deportes", StreamUrl = "https://stream.example.com/deportes" },
        new() { Id = "mock-11", Name = "Flamenco Null", Country = "España", Tags = "flamenco", StreamUrl = "https://stream.example.com/flamenco-null" },
        new() { Id = "mock-12", Name = "Metal Runtime", Country = "Alemania", Tags = "metal", StreamUrl = "https://stream.example.com/metal-runtime" },
    ];

    public Task<IReadOnlyList<StationDto>> GetStationsAsync() => Task.FromResult(Stations);
}
