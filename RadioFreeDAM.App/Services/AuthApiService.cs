using System.Net.Http.Json;

namespace RadioFreeDAM.Services;

public class AuthApiService(HttpClient httpClient) : IAuthApiService
{
    public Task<AuthResponse> RegisterAsync(RegisterRequest request) => PostAsync("api/auth/register", request);

    public Task<AuthResponse> LoginAsync(LoginRequest request) => PostAsync("api/auth/login", request);

    public async Task<UserDto> GetCurrentUserAsync()
    {
        using var response = await httpClient.GetAsync("api/auth/me");
        await ApiException.ThrowIfErrorAsync(response);
        return (await response.Content.ReadFromJsonAsync<UserDto>())!;
    }

    private async Task<AuthResponse> PostAsync<T>(string url, T body)
    {
        using var response = await httpClient.PostAsJsonAsync(url, body);
        await ApiException.ThrowIfErrorAsync(response);
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }
}
