using RForgeDocs.Abstractions.DataModels;
using RForgeDocs.Abstractions.Services;
using System.Net.Http.Json;

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
        try
        {
            var httpClient = _httpClientFactory.CreateClient("api");
            return await httpClient.GetFromJsonAsync<List<UserData>>($"api/users/find/?searchText={searchText}&returnCount={returnCount}");
        }
        catch (Exception ex)
        {

        }

        return null;
    }
}
