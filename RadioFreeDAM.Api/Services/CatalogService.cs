using RadioFreeDAM.Shared.Dtos;

namespace RadioFreeDAM.Api.Services;

/// <summary>
/// Catálogo tal y como lo enseña la app: las emisoras de una misma cadena (COPE Málaga, COPE Sevilla...)
/// se juntan en una sola fila, y desde ella se elige la ciudad.
/// </summary>
public class CatalogService(IStationService stations)
{
    /// <summary>Filas que muestra Inicio.</summary>
    public const int TopEntries = 20;

    public async Task<IReadOnlyList<CatalogEntryDto>> GetTopAsync(CancellationToken cancellationToken = default) =>
        Group(await stations.GetTopStationsAsync(cancellationToken), TopEntries);

    public async Task<IReadOnlyList<CatalogEntryDto>> SearchAsync(string name, CancellationToken cancellationToken = default) =>
        Group(await stations.SearchByNameAsync(name, cancellationToken), int.MaxValue);

    /// <summary>Todas las emisoras de una cadena en el país configurado, de la más a la menos escuchada.</summary>
    /// <exception cref="NotFoundAppException">Si la cadena no existe.</exception>
    public async Task<IReadOnlyList<StationDto>> GetNetworkStationsAsync(string networkId, CancellationToken cancellationToken = default)
    {
        var network = NetworkCatalog.FindById(networkId)
            ?? throw new NotFoundAppException("Esa cadena no existe.");

        // Una búsqueda por cada forma de escribir el nombre ("esradio" y "es radio"); luego se juntan.
        var found = new List<StationDto>();
        foreach (var term in network.SearchTerms)
            found.AddRange(await stations.SearchInCountryAsync(term, cancellationToken));

        return found
            .Where(s => network.Contains(s.Name))
            .DistinctBy(s => s.Id)
            .OrderByDescending(s => s.ClickCount)
            .ToList();
    }

    /// <summary>
    /// Junta en una fila las emisoras de una misma cadena. Una cadena solo se agrupa si aparece más de una vez:
    /// si en la lista solo hay una COPE, se muestra tal cual. Se respeta el orden (la fila de la cadena va
    /// donde estaba su emisora más popular).
    /// </summary>
    public static IReadOnlyList<CatalogEntryDto> Group(IReadOnlyList<StationDto> stations, int limit)
    {
        string? NetworkOf(StationDto station) => station.NetworkId ?? NetworkCatalog.FindFor(station.Name)?.Id;

        var byNetwork = stations
            .Where(s => NetworkOf(s) is not null)
            .GroupBy(s => NetworkOf(s)!)
            .ToDictionary(g => g.Key, g => g.ToList());

        var entries = new List<CatalogEntryDto>();
        var added = new HashSet<string>();

        foreach (var station in stations)
        {
            if (entries.Count >= limit)
                break;

            var networkId = NetworkOf(station);
            if (networkId is null || byNetwork[networkId].Count < 2)
            {
                entries.Add(new CatalogEntryDto { Station = station });
                continue;
            }

            if (!added.Add(networkId))
                continue; // ya está la fila de esta cadena

            entries.Add(new CatalogEntryDto
            {
                Network = new NetworkDto
                {
                    Id = networkId,
                    Name = NetworkCatalog.FindById(networkId)!.Name,
                    FaviconUrl = byNetwork[networkId].FirstOrDefault(s => !string.IsNullOrWhiteSpace(s.FaviconUrl))?.FaviconUrl
                }
            });
        }

        return entries;
    }
}
