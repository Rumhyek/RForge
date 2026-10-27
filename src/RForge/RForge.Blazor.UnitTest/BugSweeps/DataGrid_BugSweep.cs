using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RForge.Abstractions.DataGrids;
using RForgeBlazor;
using RForgeBlazor.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BunitTestContext = Bunit.TestContext;

namespace RForge.Blazor.UnitTest.BugSweeps;

/// <summary>
/// Repro tests from the #68 bug sweep for the data grid stack
/// (DataGridContext, RfDataGrid, RfDgHeader). Red-by-construction.
/// EventCallback parameters are built explicitly (ComponentParameter does not coerce delegates).
/// </summary>
[TestClass]
public class DataGrid_BugSweep
{
    private BunitTestContext ctx;

    [TestInitialize]
    public void Setup()
    {
        ctx = new BunitTestContext();
        BugSweepContextSpy.Captured = null;
    }

    [TestCleanup]
    public void Cleanup() => ctx.Dispose();

    // BUG #90 — Models/DataGridContext.cs:61-65,72-76 (+ RfTabListContext.cs:32-38): multicast
    // async events are invoked via delegate.Invoke → ONLY the last subscriber's Task is awaited.
    // Earlier subscribers run fire-and-forget: their exceptions are unobserved and the raiser
    // returns while their work is still in flight. race, high.
    [TestMethod]
    [Ignore("pending #90 — #68 bug-sweep repro, red-by-construction")]
    public void SortChanged_AwaitsAllSubscribers()
    {
        var gridContext = new DataGridContext();
        var gate = new TaskCompletionSource<bool>();
        bool firstSubscriberFinished = false;

        async Task FirstSubscriber(object sender, DataGridSortBy sortBy)
        {
            await gate.Task;
            firstSubscriberFinished = true;
        }

        Task SecondSubscriber(object sender, DataGridSortBy sortBy) => Task.CompletedTask;

        gridContext.OnSortChanged += FirstSubscriber; // subscribed FIRST (like the grid's own callback)
        gridContext.OnSortChanged += SecondSubscriber;

        // Release the first subscriber shortly AFTER this test has made its call:
        // today SortChanged returns immediately (red); once every subscriber is awaited
        // it returns at ~500 ms with the flag set (green).
        Task.Delay(500).ContinueWith(_ => gate.TrySetResult(true));

        gridContext.SortChanged(new DataGridSortBy { SortKey = "name" })
                   .GetAwaiter().GetResult();

        Assert.IsTrue(firstSubscriberFinished,
            "SortChanged must await EVERY async subscriber before returning; today delegate.Invoke returns only the LAST subscriber's task.");
    }

    // BUG #91 — RfDataGrid.razor.cs:628,636: OnNextPageClick Math.Min(TotalPages-1, …) and
    // OnLastPageClick TotalPages-1 both emit -1 when TotalCount == 0 (TotalPages=0), and the
    // disabled guards (CurrentPageIndex+1 == TotalPages → 1==0 false) leave the buttons ENABLED.
    // Consumers get CurrentPageIndexChanged(-1) → negative Skip → ArgumentOutOfRangeException. math, high.
    [TestMethod]
    [Ignore("pending #91 — #68 bug-sweep repro, red-by-construction")]
    public void EmptyGrid_PagerNeverEmitsNegative()
    {
        var emitted = new List<int>();
        var cut = ctx.RenderComponent<RfDataGrid<string>>(
            ComponentParameter.CreateParameter("Data", new List<string>()),
            ComponentParameter.CreateParameter("TotalCount", 0),
            ComponentParameter.CreateParameter("PageSize", 10),
            ComponentParameter.CreateParameter("CurrentPageIndexChanged",
                EventCallback.Factory.Create<int>(this, (Action<int>)(i => emitted.Add(i)))));

        cut.Instance.OnNextPageClick().GetAwaiter().GetResult();
        cut.Instance.OnLastPageClick().GetAwaiter().GetResult();

        CollectionAssert.DoesNotContain(emitted, -1,
            "On an empty grid (TotalCount=0) the pager must not emit page index -1; today Next/Last both fire CurrentPageIndexChanged(-1).");
    }

    // BUG #92 — RfDataGrid.razor.cs:710-718 vs 745-748: OnFilterChangedCallback emits
    // CurrentPageIndexChanged(0) but never sets the LOCAL CurrentPageIndex (the sort path does).
    // If the parent does not echo the parameter back, pager labels and next-click math run on
    // the stale page index. math, med.
    [TestMethod]
    [Ignore("pending #92 — #68 bug-sweep repro, red-by-construction")]
    public void FilterChange_ResetsLocalPageIndex()
    {
        var emitted = new List<int>();

        RenderFragment filters = b =>
        {
            b.OpenComponent<BugSweepContextSpy>(0);
            b.CloseComponent();
        };

        RenderFragment<string> cells = row => b =>
        {
            b.OpenElement(0, "td");
            b.AddContent(1, row);
            b.CloseElement();
        };

        var cut = ctx.RenderComponent<RfDataGrid<string>>(
            ComponentParameter.CreateParameter("Data", new List<string> { "row" }),
            ComponentParameter.CreateParameter("Cells", cells),
            ComponentParameter.CreateParameter("TotalCount", 100),
            ComponentParameter.CreateParameter("PageSize", 10),
            ComponentParameter.CreateParameter("CurrentPageIndex", 2),
            ComponentParameter.CreateParameter("Filters", filters),
            ComponentParameter.CreateParameter("CurrentPageIndexChanged",
                EventCallback.Factory.Create<int>(this, (Action<int>)(i => emitted.Add(i)))));

        var gridContext = BugSweepContextSpy.Captured;
        Assert.IsNotNull(gridContext, "the Filters fragment must receive the cascaded DataGridContext");

        // Consumer pattern: handler receives page 0 but does NOT echo CurrentPageIndex back.
        cut.InvokeAsync(() => gridContext.FilterChanged(new DataGridFilterBy()))
           .GetAwaiter().GetResult();

        // Next-page math must now run from page 0 → emits 1. Today the stale local 2 makes it emit 3.
        emitted.Clear();
        cut.Instance.OnNextPageClick().GetAwaiter().GetResult();

        CollectionAssert.Contains(emitted, 1,
            "After a filter change the next click must emit page 1 (local index reset); today the stale local CurrentPageIndex=2 emits 3.");
    }

    // BUG #93 — RfDgHeader.razor.cs:169-170: HeaderCss interpolates the ACCUMULATOR
    // (`css += $"{css} "`) instead of CssClass → the consumer's header CssClass is silently
    // dropped (a stray space is appended instead). math, med.
    [TestMethod]
    [Ignore("pending #93 — #68 bug-sweep repro, red-by-construction")]
    public void CssClass_AppearsInHeader()
    {
        RenderFragment headers = b =>
        {
            b.OpenComponent<RfDgHeader>(0);
            b.AddAttribute(1, "SortKey", "name");
            b.AddAttribute(2, "CssClass", "my-head");
            b.AddAttribute(3, "ChildContent", (RenderFragment)(x => x.AddContent(0, "Name")));
            b.CloseComponent();
        };

        RenderFragment<string> cells = row => b =>
        {
            b.OpenElement(0, "td");
            b.AddContent(1, row);
            b.CloseElement();
        };

        var cut = ctx.RenderComponent<RfDataGrid<string>>(
            ComponentParameter.CreateParameter("Data", new List<string> { "row" }),
            ComponentParameter.CreateParameter("Cells", cells),
            ComponentParameter.CreateParameter("Headers", headers));

        Assert.IsTrue(cut.Find("th").ClassList.Contains("my-head"),
            "RfDgHeader CssClass must appear on the <th>; today HeaderCss self-interpolates and drops it.");
    }

    // BUG #94 — RfDataGrid.razor:119,125: @Cells(row) is invoked unguarded in the populated-data
    // branch although Cells is an optional RenderFragment<TRowData> (the skeleton branch guards it)
    // → mid-render NRE whenever a page of rows arrives without a Cells template. crash, med.
    [TestMethod]
    [Ignore("pending #94 — #68 bug-sweep repro, red-by-construction")]
    public void PopulatedRows_NoCells_DoesNotThrow()
    {
        var cut = ctx.RenderComponent<RfDataGrid<string>>(
            ComponentParameter.CreateParameter("Data", new List<string> { "a row" }));

        // Today this NREs mid-render on @Cells(row).
        Assert.IsTrue(cut.FindAll("tr").Count > 0,
            "RfDataGrid with data but no Cells template must render without NRE (guard like the skeleton branch already does).");
    }
}
