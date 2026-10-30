using Microsoft.Extensions.Options;
using RForgeDocs.Abstractions.DataModels;
using RForgeDocs.Abstractions.Services;
using System.Net.Http.Json;
using System.Text.Json;

namespace RForgeDocs.Client.Implementation;

public class ApiUserDataGridPageProcessor : IUserDataGridPageProcessor
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ApiUserDataGridPageProcessor(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<List<UserData>> GetAll()
    {
        var httpClient = _httpClientFactory.CreateClient("api");

        var response = await httpClient.GetAsync("api/users/all");

        return await ReadApiResult<List<UserData>>(response, "api/users/all");
    }

    public async Task<GridPageResults<UserData>> GetPage(UserDataGridGetPageData options)
    {
        var httpClient = _httpClientFactory.CreateClient("api");

        var response = await httpClient.PostAsJsonAsync<UserDataGridGetPageData>("api/users/page", options);

        return await ReadApiResult<GridPageResults<UserData>>(response, "api/users/page");
    }

    /// <summary>
    /// Reads the deserialized payload of an API response, surfacing every failure (transport,
    /// HTTP status, or unreadable/empty payload) as an error naming the endpoint. Failures are
    /// never swallowed into null: callers dereference these results, and the hidden null meant
    /// either an NRE circuit death or a data grid stuck in skeleton mode forever (bug #100).
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
