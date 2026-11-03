using System.Net.Http.Json;

namespace RadioFreeDAM.Services;

public class StationApiService(HttpClient httpClient) : IStationApiService
{
    public Task<IReadOnlyList<CatalogEntryDto>> GetTopStationsAsync() => GetAsync<CatalogEntryDto>("api/catalog");

    public Task<IReadOnlyList<CatalogEntryDto>> SearchAsync(string name) =>
        GetAsync<CatalogEntryDto>($"api/catalog?name={Uri.EscapeDataString(name)}");

    public Task<IReadOnlyList<StationDto>> GetNetworkStationsAsync(string networkId) =>
        GetAsync<StationDto>($"api/networks/{Uri.EscapeDataString(networkId)}/stations");

    private async Task<IReadOnlyList<T>> GetAsync<T>(string url)
    {
        var items = await httpClient.GetFromJsonAsync<List<T>>(url);
        return items ?? [];
    }
}
