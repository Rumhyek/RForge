using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RForgeBlazor;
using System.Collections.Generic;
using System.Linq;
using BunitTestContext = Bunit.TestContext;

namespace RForge.Blazor.UnitTest.BugSweeps;

/// <summary>
/// Repro tests from the #68 bug sweep for RfContextMenu.
/// Authored for the fix issue itself (#77): finding 30 had no pre-tagged repro
/// ("test optional (F8)"), so this follows the same convention per docs/bug-sweeps.md.
/// </summary>
[TestClass]
public class RfContextMenu_BugSweep
{
    private BunitTestContext ctx;

    [TestInitialize]
    public void Setup() => ctx = new BunitTestContext();

    [TestCleanup]
    public void Cleanup() => ctx.Dispose();

    // BUG #77 — RfContextMenu.razor.cs:83: DropDownId = new Random().Next(100000, 999999) per
    // instance -> birthday collision in the ~900k space with many menus -> duplicate DOM ids,
    // broken aria-controls/JS targeting. race, low.
    // Honest non-determinism (spec #68 §6, #77 body): against unfixed code this is red with
    // probability ~1 - 1.4e-4 (4000 draws over 899999 values, P(no collision) ~ exp(-8.89)).
    // The fix is a process-wide counter, so once green this test is deterministic and can never
    // flake in CI.
    [TestMethod]
    public void MenuIds_AcrossManyInstances_AreUnique()
    {
        const int instances = 4000;
        var cut = ctx.RenderComponent<ContextMenuCountHost>(
            ComponentParameter.CreateParameter("Count", instances));

        var menus = cut.FindAll("div.dropdown-menu");
        var triggers = cut.FindAll(".dropdown-trigger button");
        Assert.AreEqual(instances, menus.Count, "each RfContextMenu must render its dropdown-menu div.");
        Assert.AreEqual(instances, triggers.Count, "each RfContextMenu must render its trigger button.");

        List<string> ids = menus.Select(m => m.GetAttribute("id")).ToList();
        List<string> controls = triggers.Select(t => t.GetAttribute("aria-controls")).ToList();

        int distinctIds = ids.Distinct().Count();
        Assert.AreEqual(instances, distinctIds,
            $"RfContextMenu DOM ids collide: {instances - distinctIds} duplicate id(s) among " +
            $"{instances} menus; duplicate ids break aria-controls/JS targeting.");

        CollectionAssert.AreEqual(ids, controls,
            "every trigger's aria-controls must name its own menu's id (1:1 pairing).");
    }
}

/// <summary>Test-only host that renders a parameterized number of standalone RfContextMenu instances.</summary>
public class ContextMenuCountHost : ComponentBase
{
    /// <summary>Number of RfContextMenu instances to render.</summary>
    [Parameter]
    public int Count { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        for (int i = 0; i < Count; i++)
        {
            builder.OpenComponent<RfContextMenu>(i * 2);
            builder.CloseComponent();
        }
    }
}
