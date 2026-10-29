using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RForge.Abstractions.DropDowns;
using RForgeBlazor;
using System;
using System.Collections.Generic;
using BunitTestContext = Bunit.TestContext;

namespace RForge.Blazor.UnitTest.BugSweeps;

/// <summary>
/// Repro tests from the #68 bug sweep for RfDropDown / RfDropDownMulti.
/// Red-by-construction; un-ignore when the fix issue lands.
/// </summary>
[TestClass]
public class RfDropDown_BugSweep
{
    private BunitTestContext ctx;

    [TestInitialize]
    public void Setup() => ctx = new BunitTestContext();

    [TestCleanup]
    public void Cleanup() => ctx.Dispose();

    private static RenderFragment<string> TextTemplate
        => item => builder => builder.AddContent(0, item);

    // BUG #73 — canary-4: RfDropDown.razor:23 invokes @ChildContent(SelectedItem) with no null
    // guard; with a selection and no templates the render NREs. Same unguarded invoked form
    // repeats at :84 (show-at-top row) and :121 (item loop). crash, med.
    [TestMethod]
    [Ignore("pending #73 — #68 bug-sweep repro, red-by-construction")]
    public void Render_NoTemplatesWithSelection_DoesNotThrow()
    {
        var cut = ctx.RenderComponent<RfDropDown<string>>(
            ComponentParameter.CreateParameter("Items", new List<string> { "a", "b" }),
            ComponentParameter.CreateParameter("SelectedItem", "a"));

        // Today this NREs during render (trigger @ChildContent(SelectedItem)); after the fix
        // the component must render rows without crashing.
        Assert.IsTrue(cut.Markup.Contains("dropdown-item"),
            "RfDropDown must render safely when ChildContent/RowItemTemplate/SelectedItemTemplate are all null.");
    }

    // BUG #73 — same bug class in RfDropDownMulti.razor:30: @ChildContent(selectedItem) is
    // invoked for each selected tag with no null guard. crash, med.
    [TestMethod]
    [Ignore("pending #73 — #68 bug-sweep repro, red-by-construction")]
    public void RenderMulti_TagsWithNoChildContent_DoesNotThrow()
    {
        var cut = ctx.RenderComponent<RfDropDownMulti<string>>(
            ComponentParameter.CreateParameter("Items", new List<string> { "a", "b" }),
            ComponentParameter.CreateParameter("SelectedItems", new List<string> { "a" }));

        // Today the tag chip render invokes @ChildContent(selectedItem) → NRE.
        Assert.IsTrue(cut.Markup.Contains("tag"),
            "RfDropDownMulti must render selected tags safely when ChildContent is null.");
    }

    // BUG #74 — RfDropDownMulti.razor.cs:74: selecting the FIRST item when the consumer binds
    // only SelectedItemsChanged (SelectedItems left null) executes SelectedItems.Add(item) → NRE.
    // IsSelected tolerates null; the add path does not. crash, high.
    [TestMethod]
    public void ItemClick_NullSelectedItems_DoesNotThrow()
    {
        List<string> emitted = null;
        var cut = ctx.RenderComponent<RfDropDownMulti<string>>(
            ComponentParameter.CreateParameter("Items", new List<string> { "a", "b" }),
            ComponentParameter.CreateParameter("ChildContent", TextTemplate),
            ComponentParameter.CreateParameter("SelectedItemsChanged",
                EventCallback.Factory.Create<List<string>>(this, (Action<List<string>>)(items => emitted = items))));

        cut.Find(".dropdown-trigger button").Click(); // open

        cut.Find("a.dropdown-item").Click(); // pick "a" — today: NullReferenceException on SelectedItems.Add

        Assert.IsNotNull(emitted,
            "Clicking the first item with SelectedItems unset must emit the new selection, not NRE (SelectedItems.Add on null).");
    }

}
