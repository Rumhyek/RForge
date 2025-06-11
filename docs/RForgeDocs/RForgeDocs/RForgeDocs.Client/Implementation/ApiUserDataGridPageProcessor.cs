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
        try
        {
            var httpClient = _httpClientFactory.CreateClient("api");

            var response = await httpClient.GetAsync("api/users/all");

            return await response.Content.ReadFromJsonAsync<List<UserData>>();
        }
        catch (Exception e)
        {
        }

        return null;
    }

    public async Task<GridPageResults<UserData>> GetPage(UserDataGridGetPageData options)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("api");

            var response = await httpClient.PostAsJsonAsync<UserDataGridGetPageData>("api/users/page", options);

            return await response.Content.ReadFromJsonAsync<GridPageResults<UserData>>();
        }
        catch (Exception e)
        {
        }

        return null;
    }
}
