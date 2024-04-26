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
        try
        {
            var httpClient = _httpClientFactory.CreateClient("api");
            var response = await httpClient.PutAsJsonAsync<UserAddSaveData>("api/users", userData);
            var idString = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<int?>(idString);
        }
        catch { }

        return null;
    }

    public async Task<bool> SaveUser(UserSaveData userData)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("api");
            var response = await httpClient.PostAsJsonAsync<UserSaveData>("api/users", userData);
            var idString = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<bool>(idString);
        }
        catch (Exception e)
        {
        }

        return false;
    }
}

public class ApiUserDataGridPageProcessor : IUserDataGridPageProcessor
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ApiUserDataGridPageProcessor(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<GridPageResults<UserData>> GetPage(UserDataGridGetPageData options)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("api");
            var response = await httpClient.PostAsJsonAsync<UserDataGridGetPageData>("api/users/page", options);
            var idString = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<GridPageResults<UserData>>(idString);
        }
        catch (Exception e)
        {
        }

        return null;
    }
}
