using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using RForgeDocs;
using RForgeDocs.Abstractions.DataModels;
using RForgeDocs.EndPoints;
using RForgeDocsLibrary.Implementation;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace RForge.Blazor.UnitTest.BugSweeps;

/// <summary>
/// Repro tests from the #68 bug sweep for the server side of the docs app: the user API
/// routes (<see cref="UserEndpoints"/>, hosted in-process over loopback) and the processors
/// behind them. Written fresh against the Docs_BugSweep convention: active tests asserting
/// the correct behavior, tagged "// BUG #&lt;fix-issue#&gt;".
/// </summary>
[TestClass]
public class Docs_ServerBugSweep
{
    private WebApplication app;
    private HttpClient client;

    [TestInitialize]
    public async Task Setup()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        IocConfig.Register(builder.Configuration, builder.Services);

        app = builder.Build();
        app.MapUserApi();
        await app.StartAsync();

        client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        client.Dispose();
        await app.StopAsync();
        await app.DisposeAsync();
    }

    // BUG #101 — RForgeDocsLibrary/Implementation/UserDataGridPageProcessor.cs:152:
    // Skip(PageSize * PageIndex) is unguarded; the playground exposes CurrentPageIndex as a
    // free InputNumber, so -1 reaches Skip(-N) → ArgumentOutOfRangeException → 500 → null →
    // NRE chain in the page. crash, med.
    [TestMethod]
    public async Task NegativePageIndex_Rejected()
    {
        var processor = new UserDataGridPageProcessor(new MockUserRepository());

        GridPageResults<UserData> results = null;
        Exception thrown = null;
        try
        {
            results = await processor.GetPage(new UserDataGridGetPageData
            {
                Filters = new UserDataGridFilterData(),
                PageIndex = -1,
                PageSize = 10,
            });
        }
        catch (Exception ex)
        {
            thrown = ex;
        }

        Assert.IsNull(thrown,
            "a negative PageIndex must not blow up the paged API "
            + (thrown == null ? "" : $"(caught {thrown.GetType().Name}: {thrown.Message})"));
        Assert.AreEqual(10, results.Data.Count, "a negative page index falls back to the first page.");
        Assert.AreEqual(1, results.Data.First().Id);
    }

    // BUG #102 — UserDataGridPageProcessor.cs:99-101: the FullName descending branch calls
    // OrderByDescending twice, so the second replaces the first and LastName is ignored (the
    // ascending branch correctly uses ThenBy). math, med.
    [TestMethod]
    public async Task DescNameSort_ThenByDescending()
    {
        var repo = new MockUserRepository();
        var processor = new UserDataGridPageProcessor(repo);

        var results = await processor.GetPage(new UserDataGridGetPageData
        {
            Filters = new UserDataGridFilterData(),
            SortKey = nameof(UserDataGridFilterData.FullName),
            SortOrder = RForge.Blazor.Abstractions.RfSortOrder.Descending,
            PageIndex = 0,
            PageSize = 200,
        });

        var expected = repo.GetAllUsers()
            .OrderByDescending(u => u.LastName)
            .ThenByDescending(u => u.FirstName)
            .Select(u => u.Id)
            .ToList();

        CollectionAssert.AreEqual(expected, results.Data.Select(d => d.Id).ToList(),
            "descending 'Name' must order by LastName then FirstName, both descending.");
    }

    // BUG #107 — RForgeDocs/EndPoints/UserEndpoints.cs:12-13: the catch-all
    // "/api/users/{*userId}" bound to an int shadows other routes and answers a non-numeric
    // suffix with a raw 400 binding failure; an unknown id comes back 200 with a null body, which
    // the clients render as a permanent skeleton. crash, low.
    [TestMethod]
    public async Task UnknownUser_GracefulNotFound()
    {
        using var unknown = await client.GetAsync("/api/users/99999");
        Assert.AreEqual(HttpStatusCode.NotFound, unknown.StatusCode,
            "an unknown user id must answer 404, not 200 with a null body.");

        using var garbage = await client.GetAsync("/api/users/abc");
        Assert.AreEqual(HttpStatusCode.NotFound, garbage.StatusCode,
            "a non-numeric id is just not a user route: 404, not a raw binding 400.");

        using var known = await client.GetAsync("/api/users/1");
        Assert.AreEqual(HttpStatusCode.OK, known.StatusCode);
        var user = await known.Content.ReadFromJsonAsync<UserData>();
        Assert.AreEqual(1, user.Id);
    }

    // BUG #107 — same catch-all: "/api/users/find" without the trailing slash must reach the
    // find route, not be swallowed by the user-id catch-all.
    [TestMethod]
    public async Task FindRoute_NotShadowedByUserCatchAll()
    {
        using var response = await client.GetAsync("/api/users/find?searchText=a");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
            "/api/users/find must be served by the find route, not shadowed by /api/users/{*userId}.");
    }

    // BUG #108 — RForgeDocsLibrary/Implementation/SaveUserProcessor.cs:10: AddUser always throws
    // NotImplementedException, so PUT /api/users is always a 500 and the client processor swallows
    // it to null — the documented "add user" flow can never work, silently. crash, low.
    [TestMethod]
    public async Task AddUser_WiredOrDocumented()
    {
        using var response = await client.PutAsJsonAsync("/api/users", new UserAddSaveData
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Username = "ALovelace",
            Email = "ada.lovelace@example.org",
            Bio = "First programmer.",
        });

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
            "PUT /api/users with a valid body must succeed instead of 500ing on NotImplementedException.");
        var id = await response.Content.ReadFromJsonAsync<int?>();
        Assert.IsNotNull(id, "AddUser must return the id of the added user.");
    }
}
