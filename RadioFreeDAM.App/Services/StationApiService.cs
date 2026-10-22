using System.Net.Http.Json;

namespace RadioFreeDAM.Services;

public class StationApiService(HttpClient httpClient) : IStationApiService
{
    public async Task<IReadOnlyList<StationDto>> GetStationsAsync()
    {
        var stations = await httpClient.GetFromJsonAsync<List<StationDto>>("api/stations");
        return stations ?? [];
    }
}
