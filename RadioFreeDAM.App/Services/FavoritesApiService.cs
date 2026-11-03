using System.Net.Http.Json;

namespace RadioFreeDAM.Services;

public class FavoritesApiService(HttpClient httpClient) : IFavoritesApiService
{
    public async Task<IReadOnlyList<FavoriteDto>> GetAllAsync()
    {
        using var response = await httpClient.GetAsync("api/favorites");
        await ApiException.ThrowIfErrorAsync(response);
        return await response.Content.ReadFromJsonAsync<List<FavoriteDto>>() ?? [];
    }

    public async Task<FavoriteDto> AddAsync(string stationId)
    {
        using var response = await httpClient.PostAsJsonAsync("api/favorites", new AddFavoriteRequest { StationId = stationId });
        await ApiException.ThrowIfErrorAsync(response);
        return (await response.Content.ReadFromJsonAsync<FavoriteDto>())!;
    }

    public async Task RemoveAsync(string stationId)
    {
        using var response = await httpClient.DeleteAsync($"api/favorites/{Uri.EscapeDataString(stationId)}");
        await ApiException.ThrowIfErrorAsync(response);
    }
}
