namespace ContentForge.IntegrationTests.Api;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using ContentForge.Application.Auth.Models;

internal static class ApiTestHelper
{
    internal static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<LoginResultDto>();
        return payload!.AccessToken;
    }

    internal static HttpRequestMessage CreateAuthenticatedRequest(
        HttpMethod method,
        string url,
        string accessToken,
        HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, url) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }
}
