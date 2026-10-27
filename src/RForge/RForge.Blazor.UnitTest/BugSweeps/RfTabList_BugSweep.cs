using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RForgeBlazor;
using System;
using System.Threading;
using System.Threading.Tasks;
using BunitTestContext = Bunit.TestContext;

namespace RForge.Blazor.UnitTest.BugSweeps;

/// <summary>
/// Repro tests from the #68 bug sweep for RfTab / RfTabList. Red-by-construction.
/// </summary>
[TestClass]
public class RfTabList_BugSweep
{
    private BunitTestContext ctx;

    [TestInitialize]
    public void Setup() => ctx = new BunitTestContext();

    [TestCleanup]
    public void Cleanup() => ctx.Dispose();

    private static RenderFragment TabLabel(string text) => b => b.AddContent(0, text);

    // BUG #89 — CANARY-1 family (RfTab.razor.cs:126-136): TabListOnLoad sets IsLoading=true,
    // awaits OnLoad, clears + sets IsLoaded — NO try/finally. An exception latches the per-tab
    // spinner forever and the guards block all future loads of that tab. stuck, high.
    [TestMethod]
    [Ignore("pending #89 — #68 bug-sweep repro, red-by-construction")]
    public void TabLoad_ThrowingOnLoad_DoesNotLatch()
    {
        int aLoads = 0;

        RenderFragment tabs = b =>
        {
            b.OpenComponent<RfTab>(0);
            b.AddAttribute(1, "TabId", "A");
            b.AddAttribute(2, "Tab", TabLabel("Tab A"));
            b.AddAttribute(3, "Panel", TabLabel("Panel A"));
            b.AddAttribute(4, "OnLoad", EventCallback.Factory.Create<string>(this,
                (Func<string, Task>)(id =>
                {
                    aLoads++;
                    throw new InvalidOperationException("tab load boom");
                })));
            b.CloseComponent();
        };

        // Start with a non-matching ActiveTabId so registration renders cleanly;
        // the click drives TabListOnLoad.
        var cut = ctx.RenderComponent<RfTabList>(
            ComponentParameter.CreateParameter("ActiveTabId", "Z"),
            ComponentParameter.CreateParameter("ChildContent", tabs));

        Assert.ThrowsException<InvalidOperationException>(() => cut.Find("#tab-A").Click());

        cut.Render(); // fresh render re-reads tab.IsLoading for the header spinner

        Assert.IsTrue(aLoads > 0, "the click did drive the tab load");
        Assert.IsFalse(cut.Find("#tab-A").OuterHtml.Contains("is-loading"),
            "A thrown tab OnLoad must not leave the tab latched in is-loading (no try/finally in TabListOnLoad).");
    }

    // BUG #95 — RfTabList.razor.cs:88-92: OnParametersSet only validates ActiveTabId; a
    // programmatic ActiveTabId change never calls SetActiveTab. The header highlight moves but
    // the panel never loads/shows — OnLoad/OnShow never fire until the user clicks. stuck, med.
    [TestMethod]
    [Ignore("pending #95 — #68 bug-sweep repro, red-by-construction")]
    public void ActiveTabIdChange_LoadsNewTab()
    {
        int bLoads = 0;

        RenderFragment tabs = b =>
        {
            b.OpenComponent<RfTab>(0);
            b.AddAttribute(1, "TabId", "A");
            b.AddAttribute(2, "Tab", TabLabel("Tab A"));
            b.AddAttribute(3, "Panel", TabLabel("Panel A"));
            b.CloseComponent();

            b.OpenComponent<RfTab>(10);
            b.AddAttribute(11, "TabId", "B");
            b.AddAttribute(12, "Tab", TabLabel("Tab B"));
            b.AddAttribute(13, "Panel", TabLabel("Panel B"));
            b.AddAttribute(14, "OnLoad", EventCallback.Factory.Create<string>(this,
                (Action<string>)(_ => bLoads++)));
            b.CloseComponent();
        };

        var cut = ctx.RenderComponent<RfTabList>(
            ComponentParameter.CreateParameter("ActiveTabId", "A"),
            ComponentParameter.CreateParameter("ChildContent", tabs));

        // Parent flips the active tab programmatically.
        cut.SetParametersAndRender(
            ComponentParameter.CreateParameter("ActiveTabId", "B"),
            ComponentParameter.CreateParameter("ChildContent", tabs));

        Assert.IsTrue(bLoads > 0,
            "A programmatic ActiveTabId change must load and show the new tab; today only the header highlight moves.");
    }

    // BUG #96 — RfTabList.razor.cs:124-139: OnTabClick checks its guard BEFORE the awaits and
    // assigns ActiveTab only AFTER await TabListOnHide — a second click during a slow OnHide
    // re-enters: the old tab is hidden twice and two tabs load/show interleaved. race, med.
    [TestMethod]
    [Ignore("pending #96 — #68 bug-sweep repro, red-by-construction")]
    public void DoubleClickTab_LoadsOnce()
    {
        var hideGate = new TaskCompletionSource<bool>();
        int hideCount = 0;

        RenderFragment tabs = b =>
        {
            b.OpenComponent<RfTab>(0);
            b.AddAttribute(1, "TabId", "A");
            b.AddAttribute(2, "Tab", TabLabel("Tab A"));
            b.AddAttribute(3, "Panel", TabLabel("Panel A"));
            b.AddAttribute(4, "OnHide", EventCallback.Factory.Create<string>(this,
                (Func<string, Task>)(async id =>
                {
                    Interlocked.Increment(ref hideCount);
                    await hideGate.Task;
                })));
            b.CloseComponent();

            b.OpenComponent<RfTab>(10);
            b.AddAttribute(11, "TabId", "B");
            b.AddAttribute(12, "Tab", TabLabel("Tab B"));
            b.AddAttribute(13, "Panel", TabLabel("Panel B"));
            b.CloseComponent();
        };

        var cut = ctx.RenderComponent<RfTabList>(
            ComponentParameter.CreateParameter("ActiveTabId", "A"),
            ComponentParameter.CreateParameter("ChildContent", tabs));

        cut.Find("#tab-B").Click(); // first click suspends inside OnHide(A)
        cut.Find("#tab-B").Click(); // guard already passed, ActiveTab still A → re-entry

        hideGate.TrySetResult(true);
        try
        {
            cut.WaitForState(() => Volatile.Read(ref hideCount) >= 2, TimeSpan.FromMilliseconds(500));
        }
        catch (Exception)
        {
            // wait never satisfied == the second click was guarded and OnHide never ran twice.
        }

        Assert.IsTrue(Volatile.Read(ref hideCount) == 1,
            "Two fast clicks on the same tab must hide the old tab / load the new one once; today the guard is checked before the awaits and never re-checked.");
    }
}
