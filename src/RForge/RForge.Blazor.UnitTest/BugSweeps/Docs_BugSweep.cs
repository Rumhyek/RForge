using Bunit;
using Microsoft.Extensions.DependencyInjection;
using RForgeDocs.Abstractions.DataModels;
using RForgeDocs.Abstractions.Services;
using RForgeDocs.Client.Pages.Examples.DataGrid;
using RForgeDocsLibrary.Implementation;
using System;
using System.Linq;
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
}
