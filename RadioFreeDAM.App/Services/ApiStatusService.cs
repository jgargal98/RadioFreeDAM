using System.Net.Http.Json;

namespace RadioFreeDAM.Services;

public class ApiStatusService(HttpClient httpClient) : IApiStatusService
{
    public async Task<bool> IsServerAvailableAsync()
    {
        try
        {
            var health = await httpClient.GetFromJsonAsync<HealthDto>("api/health");
            return health?.Status == "ok";
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // Sin red, API apagada o tiempo de espera agotado.
            return false;
        }
    }
}
