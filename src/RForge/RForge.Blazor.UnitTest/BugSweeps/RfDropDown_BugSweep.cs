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
    public void RenderMulti_TagsWithNoChildContent_DoesNotThrow()
    {
        var cut = ctx.RenderComponent<RfDropDownMulti<string>>(
            ComponentParameter.CreateParameter("Items", new List<string> { "a", "b" }),
            ComponentParameter.CreateParameter("SelectedItems", new List<string> { "a" }));

        // Today the tag chip render invokes @ChildContent(selectedItem) → NRE.
        Assert.IsTrue(cut.Markup.Contains("tag"),
            "RfDropDownMulti must render selected tags safely when ChildContent is null.");
    }

    // BUG #73: same bug class at RfDropDown.razor:84. With ShowSelectedItemInDropDown=Always and
    // no RowItemTemplate, the show-at-top row falls through to the invoked
    // @ChildContent(SelectedItem) with no null guard. SelectedItemTemplate is set here so the
    // trigger site (:23) stays safe and the failure lands on :84. crash, med.
    [TestMethod]
    public void Render_ShowAtTopRowWithNoTemplates_DoesNotThrow()
    {
        var cut = ctx.RenderComponent<RfDropDown<string>>(
            ComponentParameter.CreateParameter("Items", new List<string> { "a", "b" }),
            ComponentParameter.CreateParameter("SelectedItem", "a"),
            ComponentParameter.CreateParameter("SelectedItemTemplate", TextTemplate),
            ComponentParameter.CreateParameter("ShowSelectedItemInDropDown", RfShowSelectionInDropDown.Always));

        // Today the show-at-top row invokes @ChildContent(SelectedItem) and NREs; after the fix the
        // row must render (empty content is fine, the selected anchor must exist).
        Assert.IsTrue(cut.FindAll("a.dropdown-item.is-selected").Count > 0,
            "RfDropDown must render the show-at-top row safely when RowItemTemplate and ChildContent are null.");
    }

    // BUG #73: same bug class at RfDropDown.razor:121. The item loop's else branch invokes
    // @ChildContent(item) with no null guard. No selection here, so the trigger and show-at-top
    // paths stay safe and the failure lands on :121. crash, med.
    [TestMethod]
    public void Render_ItemRowsWithNoTemplates_DoesNotThrow()
    {
        var cut = ctx.RenderComponent<RfDropDown<string>>(
            ComponentParameter.CreateParameter("Items", new List<string> { "a", "b" }));

        // Today the first row invokes @ChildContent(item) and NREs; after the fix every item
        // renders as a (contentless) row.
        Assert.AreEqual(2, cut.FindAll("a.dropdown-item").Count,
            "RfDropDown must render all item rows safely when RowItemTemplate and ChildContent are null.");
    }

    // BUG #73: same bug class at RfDropDownMulti.razor:105. With ShowSelectedItemInDropDown=Always
    // and no RowItemTemplate, the show-at-top row invokes @ChildContent(selectedItem) unguarded.
    // SelectedItemTemplate is set so the tag chips (:30) stay safe and the failure lands on :105.
    // crash, med.
    [TestMethod]
    public void RenderMulti_ShowAtTopRowWithNoTemplates_DoesNotThrow()
    {
        var cut = ctx.RenderComponent<RfDropDownMulti<string>>(
            ComponentParameter.CreateParameter("Items", new List<string> { "a", "b" }),
            ComponentParameter.CreateParameter("SelectedItems", new List<string> { "a" }),
            ComponentParameter.CreateParameter("SelectedItemTemplate", TextTemplate),
            ComponentParameter.CreateParameter("ShowSelectedItemInDropDown", RfShowSelectionInDropDown.Always));

        // Today the show-at-top row NREs; after the fix the selected row must render.
        Assert.IsTrue(cut.FindAll("a.dropdown-item.is-selected").Count > 0,
            "RfDropDownMulti must render the show-at-top row safely when RowItemTemplate and ChildContent are null.");
    }

    // BUG #73: same bug class at RfDropDownMulti.razor:143. The item loop's else branch invokes
    // @ChildContent(item) unguarded. No selection here, so the trigger and show-at-top paths stay
    // safe and the failure lands on :143. crash, med.
    [TestMethod]
    public void RenderMulti_ItemRowsWithNoTemplates_DoesNotThrow()
    {
        var cut = ctx.RenderComponent<RfDropDownMulti<string>>(
            ComponentParameter.CreateParameter("Items", new List<string> { "a", "b" }));

        // Today the first row invokes @ChildContent(item) and NREs; after the fix every item
        // renders as a (contentless) row.
        Assert.AreEqual(2, cut.FindAll("a.dropdown-item").Count,
            "RfDropDownMulti must render all item rows safely when RowItemTemplate and ChildContent are null.");
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
