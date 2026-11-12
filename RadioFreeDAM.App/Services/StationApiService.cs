using System.Net.Http.Json;

namespace RadioFreeDAM.Services;

public class StationApiService(HttpClient httpClient) : IStationApiService
{
    public Task<IReadOnlyList<CatalogEntryDto>> GetCatalogAsync(CatalogFilter filter)
    {
        // Every filter is sent explicitly so the request does not depend on the API's defaults.
        var url = $"api/catalog?scope={Uri.EscapeDataString(filter.Scope)}" +
                  $"&category={Uri.EscapeDataString(filter.Category)}" +
                  $"&sort={Uri.EscapeDataString(filter.Sort)}";

        if (!string.IsNullOrWhiteSpace(filter.Name))
            url += $"&name={Uri.EscapeDataString(filter.Name)}";

        return GetAsync<CatalogEntryDto>(url);
    }

    public Task<IReadOnlyList<StationDto>> GetNetworkStationsAsync(string networkId) =>
        GetAsync<StationDto>($"api/networks/{Uri.EscapeDataString(networkId)}/stations");

    private async Task<IReadOnlyList<T>> GetAsync<T>(string url)
    {
        var items = await httpClient.GetFromJsonAsync<List<T>>(url);
        return items ?? [];
    }
}
