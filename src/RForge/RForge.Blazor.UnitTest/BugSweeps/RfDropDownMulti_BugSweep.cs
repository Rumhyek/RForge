using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RForge.Abstractions.DropDowns;
using RForgeBlazor;
using System;
using System.Collections.Generic;
using System.Linq;
using BunitTestContext = Bunit.TestContext;

namespace RForge.Blazor.UnitTest.BugSweeps;

/// <summary>
/// Repro tests from the #68 bug sweep for RfDropDownMulti.
/// Authored fresh on the #75 fix issue (the sweep PR #115 committed no test for this row);
/// un-ignored by design: it is the TDD red step of #75 and ships passing with the fix.
/// </summary>
[TestClass]
public class RfDropDownMulti_BugSweep
{
    private BunitTestContext ctx;

    [TestInitialize]
    public void Setup() => ctx = new BunitTestContext();

    [TestCleanup]
    public void Cleanup() => ctx.Dispose();

    private static RenderFragment<SweepUser> NameTemplate
        => user => builder => builder.AddContent(0, user.Name);

    // BUG #75 — RfDropDownMulti.razor.cs OnDeselectClick: SelectedItems.Remove(item) uses
    // List's default (reference) equality while IsSelected and OnItemClick honor ItemComparer.
    // A deselect request for an item that is ItemComparer-equal to a SelectedItems entry but not
    // reference-equal makes Remove return false: silent early return, SelectedItemsChanged never
    // fires, the tag stays stuck. The #71 red-run arbitration showed the naive same-instance tag
    // click is green (the tag loop iterates the list's own instances); this repro uses the
    // reachable vehicle for the same-instance mismatch: the consumer swaps the selection IN PLACE
    // (selected.Clear(); selected.Add(equal-but-new instance)). Blazor has no change detection on
    // List<T>, so no re-render happens and the still-visible tag keeps its click closure over the
    // departed instance. Clicking it must deselect by ItemComparer, not by reference. stuck, med.
    [TestMethod]
    public void TagDeselect_HonorsItemComparer()
    {
        List<SweepUser> emitted = null;
        int emitCount = 0;

        var selected = new List<SweepUser> { new SweepUser { Id = 1, Name = "a" } };

        var cut = ctx.RenderComponent<RfDropDownMulti<SweepUser>>(
            ComponentParameter.CreateParameter("Items", new List<SweepUser>
            {
                new SweepUser { Id = 1, Name = "a" },
                new SweepUser { Id = 2, Name = "b" },
            }),
            ComponentParameter.CreateParameter("SelectedItems", selected),
            ComponentParameter.CreateParameter("ItemComparer",
                (Func<SweepUser, SweepUser, bool>)((x, y) =>
                    x == null || y == null ? ReferenceEquals(x, y) : x.Id == y.Id)),
            ComponentParameter.CreateParameter("ChildContent", NameTemplate),
            ComponentParameter.CreateParameter("ShowSelectedItemInDropDown",
                RfShowSelectionInDropDown.Never),
            ComponentParameter.CreateParameter("SelectedItemsChanged",
                EventCallback.Factory.Create<List<SweepUser>>(this, (Action<List<SweepUser>>)(items =>
                {
                    emitted = items;
                    emitCount++;
                }))));

        // Sanity: the selected item renders exactly one delete tag anchor.
        Assert.AreEqual(1, cut.FindAll(".tag.is-delete").Count,
            "one selected item must render one is-delete anchor");

        // Consumer swaps the selection content in place for an equal-but-new instance (Id = 1).
        // No re-render is triggered (List<T> has no change notification), so the visible tag
        // still carries a click closure over the departed instance: comparer-equal, not
        // reference-equal, to what SelectedItems now holds.
        selected.Clear();
        selected.Add(new SweepUser { Id = 1, Name = "a" });

        cut.Find(".tag.is-delete").Click();

        Assert.IsTrue(emitCount > 0,
            "deselecting the tag of an ItemComparer-equal item must fire SelectedItemsChanged "
            + "(Remove's reference-equality miss currently returns false and returns silently).");
        Assert.IsFalse(selected.Any(u => u.Id == 1),
            "the ItemComparer-equal entry must be gone from SelectedItems after tag deselect.");
        Assert.IsFalse(cut.Markup.Contains("is-delete"),
            "the tag must disappear once the selection is empty.");
    }
}
