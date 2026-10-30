using RForgeDocs.Abstractions.DataModels;
using RForgeDocs.Abstractions.Services;
using System.Net.Http.Json;
using System.Text.Json;

namespace RForgeDocs.Client.Implementation;

public class ApiFindUsersProcessor : IFindUsersProcessor
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ApiFindUsersProcessor(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<List<UserData>> Find(string searchText, int returnCount = 10)
    {
        var httpClient = _httpClientFactory.CreateClient("api");
        var response = await httpClient.GetAsync($"api/users/find/?searchText={searchText}&returnCount={returnCount}");

        return await ReadApiResult<List<UserData>>(response, "api/users/find");
    }

    /// <summary>
    /// Reads the deserialized payload of an API response, surfacing every failure (transport,
    /// HTTP status, or unreadable/empty payload) as an error naming the endpoint. Failures are
    /// never swallowed into null: a hidden null here left dropdown examples silently showing
    /// "No Items Found" when the API was actually down (bug #100).
    /// </summary>
    private static async Task<T> ReadApiResult<T>(HttpResponseMessage response, string endpoint)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"API call '{endpoint}' failed with status code {(int)response.StatusCode}"
                + (string.IsNullOrEmpty(response.ReasonPhrase) ? "." : $" ({response.ReasonPhrase})."));
        }

        try
        {
            return await response.Content.ReadFromJsonAsync<T>()
                ?? throw new InvalidDataException($"API call '{endpoint}' returned no content.");
        }
        catch (JsonException e)
        {
            throw new InvalidDataException(
                $"API call '{endpoint}' returned a payload that could not be read: {e.Message}", e);
        }
    }
}
