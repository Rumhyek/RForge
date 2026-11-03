using RForgeDocs.Abstractions.DataModels;
using RForgeDocs.Abstractions.Services;
using System.Net.Http.Json;
using System.Web;

namespace RForgeDocs.Client.Implementation;

public class ApiGetUserProcessor : IGetUserProcessor
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ApiGetUserProcessor(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<UserData> GetUser(int userId)
    {
        var httpClient = _httpClientFactory.CreateClient("api");
        using var response = await httpClient.GetAsync($"api/users/{userId}");

        // An unknown id is a 404 from the API; callers get null instead of an exception.
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserData>();
    }
}
