using System.Text.Json;
using Microsoft.Extensions.Options;
using RadioFreeDAM.Api.Models;
using RadioFreeDAM.Shared.Dtos;

namespace RadioFreeDAM.Api.Services;

/// <summary>
/// Obtiene las emisoras de Radio Browser (https://www.radio-browser.info) y las adapta a StationDto.
/// El HttpClient ya viene configurado desde Program.cs con la URL base, el User-Agent, el timeout
/// y los reintentos automáticos ante cortes momentáneos.
/// </summary>
public class RadioBrowserStationService(
    HttpClient httpClient,
    IOptions<RadioBrowserOptions> options,
    ILogger<RadioBrowserStationService> logger) : IStationService
{
    private readonly RadioBrowserOptions _options = options.Value;

    public Task<IReadOnlyList<StationDto>> GetTopStationsAsync(CancellationToken cancellationToken = default)
    {
        var url = "json/stations/search" +
                  $"?countrycode={Uri.EscapeDataString(_options.CountryCode)}" +
                  "&order=clickcount&reverse=true&hidebroken=true" +
                  $"&limit={_options.TopLimit}";

        return GetStationsAsync(url, cancellationToken);
    }

    public Task<IReadOnlyList<StationDto>> SearchByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var url = "json/stations/search" +
                  $"?name={Uri.EscapeDataString(name)}" +
                  "&order=clickcount&reverse=true&hidebroken=true" +
                  $"&limit={_options.SearchLimit}";

        return GetStationsAsync(url, cancellationToken);
    }

    public async Task<IReadOnlyList<StationDto>> GetByIdsAsync(
        IReadOnlyCollection<string> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return [];

        // Una sola llamada para todas: byuuid acepta varios Ids separados por comas.
        var url = $"json/stations/byuuid?uuids={Uri.EscapeDataString(string.Join(',', ids))}";
        return await GetStationsAsync(url, cancellationToken);
    }

    private async Task<IReadOnlyList<StationDto>> GetStationsAsync(string url, CancellationToken cancellationToken)
    {
        List<RadioBrowserStation>? stations;
        try
        {
            stations = await httpClient.GetFromJsonAsync<List<RadioBrowserStation>>(url, cancellationToken);
        }
        // ExecutionRejectedException: la política de reintentos se ha rendido (tiempo agotado o circuito abierto).
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException
                                       or Polly.ExecutionRejectedException)
        {
            // Si quien canceló fue el propio cliente (la app cerró la conexión), no es un fallo de Radio Browser.
            if (cancellationToken.IsCancellationRequested)
                throw;

            logger.LogWarning(ex, "Radio Browser no ha respondido correctamente a {Url}", url);
            throw new StationCatalogUnavailableException("No se ha podido obtener el catálogo de Radio Browser.", ex);
        }

        return (stations ?? [])
            .Select(ToDto)
            .Where(s => !string.IsNullOrWhiteSpace(s.StreamUrl))
            .ToList();
    }

    /// <summary>Convierte el formato de Radio Browser al nuestro.</summary>
    private static StationDto ToDto(RadioBrowserStation station) => new()
    {
        Id = station.StationUuid,
        Name = station.Name.Trim(),
        // url_resolved es el stream directo; url puede ser una lista .pls/.m3u que el reproductor no entiende.
        StreamUrl = string.IsNullOrWhiteSpace(station.UrlResolved) ? station.Url : station.UrlResolved,
        FaviconUrl = string.IsNullOrWhiteSpace(station.Favicon) ? null : station.Favicon,
        Country = station.Country,
        Tags = station.Tags,
        Codec = station.Codec,
        Bitrate = station.Bitrate
    };
}
