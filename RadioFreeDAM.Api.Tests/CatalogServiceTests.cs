using RadioFreeDAM.Api.Services;
using RadioFreeDAM.Shared.Dtos;

namespace RadioFreeDAM.Api.Tests;

public class NetworkCatalogTests
{
    [Theory]
    [InlineData("COPE + Sevilla", "cope")]
    [InlineData("Cope Madrid FM", "cope")]
    [InlineData("COPE A CORUÑA", "cope")]
    [InlineData("Cadena SER - Ràdio Barcelona", "ser")]
    [InlineData("Onda Cero Malaga", "ondacero")]
    [InlineData("es radio", "esradio")]
    [InlineData("Radio 5 RNE Almería", "rne")]
    [InlineData("LOS 40 Principales España", "los40")]
    [InlineData("KISS FM", "kissfm")]
    public void ReconoceLaCadena(string stationName, string expectedNetwork) =>
        Assert.Equal(expectedNetwork, NetworkCatalog.FindFor(stationName)?.Id);

    [Theory]
    [InlineData("nordic lodge copenhagen")] // empieza por "cope" pero no es COPE
    [InlineData("LOS40 Classic")]           // emisora temática distinta, no una local de LOS40
    [InlineData("Los 40 Urban")]
    [InlineData("Cadena Ser + Madrid")]     // SER+ es otra emisora
    [InlineData("Café del Mar")]
    public void NoConfundeOtrasEmisoras(string stationName) =>
        Assert.Null(NetworkCatalog.FindFor(stationName));
}

public class CatalogServiceTests
{
    private static StationDto Station(string id, string name, int clicks = 0, string? favicon = null) =>
        new() { Id = id, Name = name, ClickCount = clicks, FaviconUrl = favicon, StreamUrl = "https://stream.example.com" };

    [Fact]
    public void JuntaLasLocalesDeUnaCadenaEnUnaFila_EnLaPosicionDeLaMasPopular()
    {
        var stations = new[]
        {
            Station("1", "esRadio"),
            Station("2", "COPE + Sevilla", favicon: "https://logo/cope.png"),
            Station("3", "Cadena 100"),
            Station("4", "COPE Málaga"),
            Station("5", "Cope Madrid FM")
        };

        var entries = CatalogService.Group(stations, limit: 20);

        Assert.Equal(3, entries.Count);
        Assert.Equal("1", entries[0].Station?.Id);
        Assert.Equal("cope", entries[1].Network?.Id);
        Assert.Equal("COPE", entries[1].Network?.Name);
        Assert.Equal("https://logo/cope.png", entries[1].Network?.FaviconUrl);
        Assert.Equal("3", entries[2].Station?.Id);
    }

    [Fact]
    public void UnaCadenaQueSoloApareceUnaVez_SeMuestraComoEmisora()
    {
        var entries = CatalogService.Group([Station("1", "COPE Málaga"), Station("2", "Café del Mar")], limit: 20);

        Assert.All(entries, e => Assert.NotNull(e.Station));
        Assert.Equal("COPE Málaga", entries[0].Station?.Name);
    }

    [Fact]
    public void RespetaElLimiteDeFilas()
    {
        var stations = Enumerable.Range(1, 50).Select(i => Station(i.ToString(), $"Emisora {i}")).ToList();

        Assert.Equal(20, CatalogService.Group(stations, limit: 20).Count);
    }

    [Fact]
    public async Task EmisorasDeUnaCadena_SinOtrasParecidas_YDeMasAMenosEscuchada()
    {
        var catalog = new FakeStationService(
            Station("a", "COPE Málaga", clicks: 10),
            Station("b", "COPE + Sevilla", clicks: 50),
            Station("c", "nordic lodge copenhagen", clicks: 99),
            Station("d", "Cadena 100", clicks: 80));

        var stations = await new CatalogService(catalog).GetNetworkStationsAsync("cope");

        Assert.Equal(["b", "a"], stations.Select(s => s.Id));
    }

    [Fact]
    public async Task CadenaQueNoExiste_Da404()
    {
        await Assert.ThrowsAsync<NotFoundAppException>(
            () => new CatalogService(new FakeStationService()).GetNetworkStationsAsync("no-existe"));
    }
}
