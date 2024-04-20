using RForgeDocs.Abstractions.DataModels;
using RForgeDocs.Abstractions.Services;
using System.Net.Http.Json;

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
        return await httpClient.GetFromJsonAsync<UserData>($"api/users/{userId}");
    }
}
