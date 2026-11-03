using RForgeDocs.Abstractions.DataModels;
using RForgeDocs.Abstractions.Services;
using System.Net.Http.Json;
using System.Text.Json;

namespace RForgeDocs.Client.Implementation;

public class ApiSaveUserProcessor : ISaveUserProcessor
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ApiSaveUserProcessor(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<int?> AddUser(UserAddSaveData userData)
    {
        var httpClient = _httpClientFactory.CreateClient("api");
        var response = await httpClient.PutAsJsonAsync<UserAddSaveData>("api/users", userData);
        var idString = await ReadApiText(response, "api/users (PUT)");

        try
        {
            return JsonSerializer.Deserialize<int?>(idString);
        }
        catch (JsonException e)
        {
            throw new InvalidDataException(
                $"API call 'api/users (PUT)' returned an id payload that could not be read: {e.Message}", e);
        }
    }

    public async Task<bool> SaveUser(UserSaveData userData)
    {
        var httpClient = _httpClientFactory.CreateClient("api");
        var response = await httpClient.PostAsJsonAsync<UserSaveData>("api/users", userData);
        var resultString = await ReadApiText(response, "api/users (POST)");

        try
        {
            return JsonSerializer.Deserialize<bool>(resultString);
        }
        catch (JsonException e)
        {
            throw new InvalidDataException(
                $"API call 'api/users (POST)' returned a save-result payload that could not be read: {e.Message}", e);
        }
    }

    /// <summary>
    /// Returns the response body, surfacing transport/HTTP failures as an error naming the
    /// endpoint instead of swallowing them into null/false, which hid save failures from the
    /// docs pages (bug #100).
    /// </summary>
    private static async Task<string> ReadApiText(HttpResponseMessage response, string endpoint)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"API call '{endpoint}' failed with status code {(int)response.StatusCode}"
                + (string.IsNullOrEmpty(response.ReasonPhrase) ? "." : $" ({response.ReasonPhrase})."));
        }

        return await response.Content.ReadAsStringAsync();
    }
}
