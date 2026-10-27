using System.Net.Http.Json;

namespace RadioFreeDAM.Services;

public class StationApiService(HttpClient httpClient) : IStationApiService
{
    public Task<IReadOnlyList<StationDto>> GetTopStationsAsync() => GetAsync("api/stations");

    public Task<IReadOnlyList<StationDto>> SearchAsync(string name) =>
        GetAsync($"api/stations?name={Uri.EscapeDataString(name)}");

    private async Task<IReadOnlyList<StationDto>> GetAsync(string url)
    {
        var stations = await httpClient.GetFromJsonAsync<List<StationDto>>(url);
        return stations ?? [];
    }
}
