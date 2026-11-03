using Bunit;
using Microsoft.Extensions.DependencyInjection;
using RForgeDocs.Abstractions.DataModels;
using RForgeDocs.Abstractions.Services;
using RForgeDocs.Client.Implementation;
using RForgeDocs.Client.Pages.Examples.DataGrid;
using RForgeDocsLibrary.Implementation;
using RForge.Blazor.Services;
using RForgeDocs.Client.Implementation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
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

    private sealed class StubFindUsersProcessor : IFindUsersProcessor
    {
        public Func<string, Task<List<UserData>>> OnFind { get; set; }

        public Task<List<UserData>> Find(string searchText, int returnCount = 10) => OnFind(searchText);
    }

    private static List<UserData> Users(string firstName) =>
        new() { new UserData { Id = 1, FirstName = firstName, LastName = "User", Username = firstName } };

    // BUG #103 — Pages/Playgrounds/DropDownMultiPage.razor:169-174 (pattern repeated across the
    // dropdown example pages): OnLoad awaits Find(filter) then unconditionally overwrites Items,
    // so a slow response for an earlier filter lands after, and clobbers, the result for the
    // filter the user ended on. race, med. Exercised on the single-dropdown complete example,
    // which shares the handler shape with the other nine sites.
    [TestMethod]
    public async Task DropDownFilter_StaleResponseIgnored()
    {
        var slowGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        ctx.Services.AddSingleton<IFindUsersProcessor>(new StubFindUsersProcessor
        {
            OnFind = async filter =>
            {
                if (filter == "slow")
                {
                    await slowGate.Task;
                    return Users("Stale");
                }

                return Users("Fresh");
            },
        });

        var cut = ctx.RenderComponent<RForgeDocs.Client.Pages.Examples.SingleDropDown.CompleteExamplePage>();
        cut.Find(".dropdown-trigger button").Click(); // open: items render

        var slow = cut.InvokeAsync(() => cut.Instance.OnLoad("slow"));  // earlier filter, still in flight
        await cut.InvokeAsync(() => cut.Instance.OnLoad("fast"));        // newer filter finishes first
        slowGate.SetResult(true);
        await slow;                                                      // stale response lands last
        cut.Render();

        StringAssert.Contains(cut.Markup, "Fresh", "the newest filter's result must be shown.");
        Assert.IsFalse(cut.Markup.Contains("Stale"),
            "a slow response for an earlier filter must not overwrite the result of the newer filter.");
    }

    // BUG #105 — Pages/Examples/ContextMenu/BasicExamplePage.razor:63-76: IsLoading true -> await
    // -> false with no finally; an exception between leaves the trigger button permanently
    // "is-loading" - an example teaching the exact library bug class. stuck, low.
    [TestMethod]
    public void ExampleLatch_UsesFinally()
    {
        var manager = new NotificationManager();
        manager.OnShow += (content, options, configure) => throw new InvalidOperationException("notification boom");
        ctx.Services.AddSingleton<INotificationManager>(manager);

        var cut = ctx.RenderComponent<RForgeDocs.Client.Pages.Examples.ContextMenu.BasicExamplePage>();
        cut.Find(".dropdown-trigger button").Click();
        var save = cut.FindAll("a").First(a => a.TextContent.Trim() == "Save");

        try { save.Click(); } catch (Exception) { /* the handler's own failure is expected */ }
        Thread.Sleep(1500); // the example simulates 1 s of work before notifying (and throwing)

        string markup;
        try { markup = cut.Markup; }
        catch (Exception) { markup = cut.Markup; } // first access rethrows the handler's unhandled exception

        Assert.IsFalse(cut.Find(".dropdown-trigger button").ClassList.Contains("is-loading"),
            "a throwing save must not leave the trigger button latched in is-loading.");
    }

    // BUG #106 — Client/Implementation/ApiFindUsersProcessor.cs:21: searchText was interpolated
    // into the query string unescaped, so '#' truncates the search and '&' injects parameters
    // ('a&returnCount=1' rewrites the request). math, low.
    [TestMethod]
    public async Task SearchText_UrlEscaped()
    {
        Uri requested = null;
        var handler = new CapturingHandler(request => requested = request.RequestUri);
        var processor = new ApiFindUsersProcessor(new StubHttpClientFactory(handler));

        const string hostile = "a&returnCount=1#frag";
        await processor.Find(hostile, 10);

        var query = HttpUtility.ParseQueryString(requested.Query);
        Assert.AreEqual(hostile, query["searchText"], "the search text must arrive intact.");
        Assert.AreEqual("10", query["returnCount"], "search text must not be able to rewrite other parameters.");
        Assert.AreEqual(string.Empty, requested.Fragment, "a '#' in the search text must not start a fragment.");
    }

    // BUG #109 — Pages/Examples/CardModal/FormExampleCompetePage.razor:184: ran.Next(1, 100)
    // excludes 100 although the repository has ids 1..100, so user 100 was unreachable via the
    // random redirect. math, low.
    [TestMethod]
    public void RandomId_CoversFullRange()
    {
        Assert.AreEqual(100, RForgeDocs.Client.Pages.Examples.CardModal.FormExampleCompetePage.RandomUserId(new HighestRandom()),
            "the largest random draw must map to user id 100.");
        Assert.AreEqual(1, RForgeDocs.Client.Pages.Examples.CardModal.FormExampleCompetePage.RandomUserId(new LowestRandom()),
            "the smallest random draw must map to user id 1.");
    }

    private sealed class HighestRandom : Random
    {
        public override int Next(int minValue, int maxValue) => maxValue - 1;
    }

    private sealed class LowestRandom : Random
    {
        public override int Next(int minValue, int maxValue) => minValue;
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly Action<HttpRequestMessage> _capture;

        public CapturingHandler(Action<HttpRequestMessage> capture) => _capture = capture;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _capture(request);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public StubHttpClientFactory(HttpMessageHandler handler) => _handler = handler;

        public HttpClient CreateClient(string name) =>
            new(_handler, disposeHandler: false) { BaseAddress = new Uri("http://localhost/") };
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
