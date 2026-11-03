using Bunit;
using Microsoft.Extensions.DependencyInjection;
using RForgeDocs.Abstractions.DataModels;
using RForgeDocs.Abstractions.Services;
using RForgeDocs.Client.Implementation;
using RForgeDocs.Client.Pages.Examples.DataGrid;
using RForgeDocsLibrary.Implementation;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BunitTestContext = Bunit.TestContext;

namespace RForge.Blazor.UnitTest.BugSweeps;

/// <summary>
/// Repro tests from the #68 bug sweep for the docs app (RForgeDocs) surface: the
/// double-click DataGrid example page and the paged user processor behind it.
/// Docs findings had no landing repro tests in #68's test-project PR, so these are
/// written fresh against the same convention (see docs/bug-sweeps.md): active tests
/// asserting the correct behavior, tagged "// BUG #&lt;fix-issue#&gt;".
/// </summary>
[TestClass]
public class Docs_BugSweep
{
    private BunitTestContext ctx;

    [TestInitialize]
    public void Setup()
    {
        ctx = new BunitTestContext();
        ctx.Services.AddSingleton<IUserDataGridPageProcessor>(
            new UserDataGridPageProcessor(new MockUserRepository()));
    }

    [TestCleanup]
    public void Cleanup() => ctx.Dispose();

    // BUG #99 — RForgeDocs.Client/Pages/Examples/DataGrid/DoubleClickExamplePage.razor:134:
    // the detail modal renders ShownUser.Bio.Length unguarded, but MockUserRepository.cs:99
    // (user Id=90, Ariel Gonzales) has Bio=null. Double-clicking that row throws NRE during
    // the modal render and kills the interactive circuit. crash, high.
    [TestMethod]
    public void DoubleClick_NullBio_DoesNotThrow()
    {
        var cut = ctx.RenderComponent<DoubleClickExamplePage>();

        // The exact row the sweep repro used: page 1 of Id=90 from the mock repository.
        var user90 = new UserDataGridPageProcessor(new MockUserRepository())
            .GetPage(new UserDataGridGetPageData { Filters = new UserDataGridFilterData { Id = 90 } })
            .GetAwaiter().GetResult().Data.Single();
        Assert.IsNull(user90.Bio,
            "the sweep fixture row (Id=90) must keep Bio unset; the page has to survive it.");

        Exception thrown = null;
        try
        {
            // Drive the page's public double-click handler (the callback OnRowDoubleClick raises).
            cut.InvokeAsync(() => cut.Instance.OnShowUserDetails(user90)).GetAwaiter().GetResult();
            // Force the modal render (shown-user branch) to run again and surface any render error.
            cut.SetParametersAndRender();
        }
        catch (Exception ex)
        {
            thrown = ex;
        }

        Assert.IsNull(thrown,
            "double-clicking the null-Bio user (Id=90) must render the detail modal, "
            + "not throw during the render (today ShownUser.Bio.Length NREs and kills the circuit). "
            + (thrown == null ? "" : $"Caught {thrown.GetType().Name}: {thrown.Message}"));

        StringAssert.Contains(cut.Markup, "Ariel",
            "after the fix the modal must show the double-clicked user, Bio or no Bio.");
    }

    // BUG #99 — RForgeDocsLibrary/Implementation/UserDataGridPageProcessor.cs:62:
    // the Bio filter calls u.Bio.Contains(...) directly over data containing Bio=null rows
    // (MockUserRepository Id=90) → in-memory LINQ NRE → POST /api/users/page 500s for any
    // Bio filter whose result set touches that row. crash, med.
    [TestMethod]
    public void BioFilter_NullBio_DoesNotThrow()
    {
        var repo = new MockUserRepository();
        var processor = new UserDataGridPageProcessor(repo);
        var expected = repo.GetAllUsers().Count(u => u.Bio != null && u.Bio.Contains("a"));
        Assert.IsTrue(expected > 0, "fixture sanity: some bios contain the filter term");

        GridPageResults<UserData> results = null;
        Exception thrown = null;
        try
        {
            results = processor.GetPage(new UserDataGridGetPageData
            {
                Filters = new UserDataGridFilterData { Bio = "a" },
                PageIndex = 0,
                PageSize = 200,
            }).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            thrown = ex;
        }

        Assert.IsNull(thrown,
            "a Bio filter over data containing a null Bio must skip the null rows, not throw "
            + "(today u.Bio.Contains(...) NREs and the paged API 500s). "
            + (thrown == null ? "" : $"Caught {thrown.GetType().Name}: {thrown.Message}"));

        Assert.AreEqual(expected, results.TotalCount,
            "null-Bio rows must be filtered out while every matching Bio is returned.");
        CollectionAssert.AllItemsAreNotNull(results.Data.Select(d => d.Bio).ToList());
    }

    // BUG #100 — RForgeDocs.Client/Implementation/ApiUserDataGridPageProcessor.cs:28-32 (and
    // ApiFindUsersProcessor/ApiSaveUserProcessor): empty catch -> return null on ANY failure.
    // DevTools-blocking the API makes the minimum example grid swallow the error: GetAll()
    // returns null, Data stays null, and the page sits on the RfDataGrid skeleton forever with
    // no error surfaced anywhere; callers that deref the null NRE-kill the circuit. repro F31.
    [TestMethod]
    public void ApiFailure_ShowsErrorNotCrash()
    {
        // The real client processor, pointed at a transport that fails every call the way
        // DevTools request-blocking does (the sweep's repro). Last registration wins.
        ctx.Services.AddSingleton<IUserDataGridPageProcessor>(
            new ApiUserDataGridPageProcessor(new StubApiHandlerFactory(StubApiMode.ConnectionRefused)));

        Exception thrown = null;
        try
        {
            // First render auto-loads through RfDataGrid's OnLoadData; drive OnLoad directly too
            // so a failure surfaces whether the page raises it during render or on reload.
            var cut = ctx.RenderComponent<MinimumExamplePage>();
            cut.InvokeAsync(() => cut.Instance.OnLoad()).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            thrown = ex;
        }

        Assert.IsNotNull(thrown,
            "when the users API fails, the failure must surface to the page as an error naming "
            + "the failing endpoint; today the processor swallows it, the grid never leaves "
            + "skeleton mode, and the user waits forever on data that is never coming.");

        StringAssert.Contains(thrown.ToString(), "api/users/all",
            "the surfaced failure must identify the failing API call so the docs page shows a "
            + "meaningful error, not a mystery crash (stack of thrown exception).");

        Assert.IsFalse(thrown.ToString().Contains("NullReferenceException"),
            "the failure must arrive as a descriptive API error, not later as a null-deref "
            + "crash from callers dereferencing the swallowed null: " + thrown);
    }

    // BUG #100 — ApiUserDataGridPageProcessor.GetPage swallows HTTP failures (a 500 from
    // /api/users/page returns null); the playground derefs it (DataGridPage OnLoad ->
    // response.TotalCount) -> NRE circuit death on "Force Reload".
    [TestMethod]
    public void GetPage_ApiReturns500_SurfacesErrorNotHiddenNull()
    {
        var processor = new ApiUserDataGridPageProcessor(new StubApiHandlerFactory(StubApiMode.ServerError500));

        Exception thrown = null;
        GridPageResults<UserData> results = null;
        try
        {
            results = processor.GetPage(new UserDataGridGetPageData
            {
                Filters = new UserDataGridFilterData(),
                PageIndex = 0,
                PageSize = 20,
            }).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            thrown = ex;
        }

        Assert.IsNull(results,
            "a failed page call must never hand back a null the callers deref (that NRE is the "
            + "circuit death); if a result object is returned it must be a real one.");
        Assert.IsNotNull(thrown,
            "a 500 from api/users/page must surface as an error naming endpoint and status code, "
            + "not be swallowed into null.");
        StringAssert.Contains(thrown.Message, "api/users/page",
            "the surfaced error must name the failing endpoint.");
    }
}

/// <summary>How <see cref="StubApiHandlerFactory"/> makes API calls fail.</summary>
public enum StubApiMode
{
    /// <summary>The request itself never completes (DevTools request blocking / offline).</summary>
    ConnectionRefused,

    /// <summary>The server answers 500 with a plain-text body that is not the expected JSON.</summary>
    ServerError500,
}

/// <summary>
/// Test-only IHttpClientFactory handing out HttpClients whose transport fails every request
/// the selected <see cref="StubApiMode"/> way, so the Api* processors see real API failures.
/// </summary>
public class StubApiHandlerFactory : IHttpClientFactory
{
    private readonly StubApiMode _mode;

    public StubApiHandlerFactory(StubApiMode mode) => _mode = mode;

    // The real client sets BaseAddress from the host environment (Program.cs); the stub matches.
    public HttpClient CreateClient(string name) =>
        new HttpClient(new StubApiHandler(_mode)) { BaseAddress = new Uri("http://localhost/") };

    private class StubApiHandler : HttpMessageHandler
    {
        private readonly StubApiMode _mode;

        public StubApiHandler(StubApiMode mode) => _mode = mode;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_mode == StubApiMode.ConnectionRefused)
            {
                throw new HttpRequestException($"Simulated blocked network call to '{request.RequestUri}'.");
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                RequestMessage = request,
                Content = new StringContent("The server exploded, no JSON here.", System.Text.Encoding.UTF8, "text/plain"),
            });
        }
    }
}
