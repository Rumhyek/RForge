using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RForgeBlazor;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BunitTestContext = Bunit.TestContext;

namespace RForge.Blazor.UnitTest.BugSweeps;

/// <summary>
/// Repro tests from the #68 bug sweep for RfDropDownBase. Every test is red-by-construction
/// and must pass once its follow-up fix issue lands (un-ignore as the TDD red step).
/// </summary>
[TestClass]
public class RfDropDownBase_BugSweep
{
    private BunitTestContext ctx;

    [TestInitialize]
    public void Setup() => ctx = new BunitTestContext();

    [TestCleanup]
    public void Cleanup() => ctx.Dispose();

    // BUG #70 — canary-1: OpenDropDown sets IsLoading=true, awaits OnLoad, clears with NO
    // try/finally; the entry guard (if (IsLoading) return) makes a thrown OnLoad permanently
    // unopenable. Verdict table row: Models/RfDropDownBase.cs:307-313, stuck, high.
    [TestMethod]
    [Ignore("pending #70 — #68 bug-sweep repro, red-by-construction")]
    public void OpenDropDown_ThrowingOnLoad_DoesNotLatchForever()
    {
        var cut = ctx.RenderComponent<RfDropDown<string>>(
            ComponentParameter.CreateParameter("OnLoad", EventCallback.Factory.Create<string>(this,
                (Func<string, Task>)(filter => throw new InvalidOperationException("consumer OnLoad boom")))));

        var trigger = cut.Find(".dropdown-trigger button");

        // First click: OnLoad throws. The component must not keep the latch after the throw.
        _ = Assert.ThrowsException<InvalidOperationException>(() => trigger.Click());

        // Second click must re-open (is-active). Today IsLoading is still true, the guard
        // returns, and the spinner stays: dropdown dead forever.
        trigger.Click();

        Assert.IsTrue(cut.Find(".dropdown").ClassList.Contains("is-active"),
            "Dropdown must be openable after a failed load attempt; today the IsLoading latch never clears (no try/finally).");
    }

    // BUG #71 — filter pump latch: handleFilters sets IsLoading=true, awaits
    // FilterChanged/OnLoad, clears with NO try/finally; a throwing filter load latches
    // IsLoading forever AND the fire-and-forget task exception is unobserved.
    // (Red-run note: the claimed "keystroke loss via Clear after awaits" did NOT reproduce —
    // _filterChanges.Clear() runs at POP time, before the awaits; see correction on #71.)
    // Verdict table row: Models/RfDropDownBase.cs:257-262 (start 277), stuck, high.
    [TestMethod]
    [Ignore("pending #71 — #68 bug-sweep repro, red-by-construction")]
    public void FilterLoad_ThrowingOnLoad_DoesNotLatch()
    {
        var cut = ctx.RenderComponent<RfDropDown<string>>(
            ComponentParameter.CreateParameter("FilterRateLimit", 0),
            ComponentParameter.CreateParameter("Items", new List<string> { "a", "b" }),
            ComponentParameter.CreateParameter("ChildContent",
                (RenderFragment<string>)(item => builder => builder.AddContent(0, item))),
            ComponentParameter.CreateParameter("OnLoad", EventCallback.Factory.Create<string>(this,
                (Func<string, Task>)(filter =>
                {
                    if (string.IsNullOrEmpty(filter) == true)
                    {
                        return Task.CompletedTask; // initial open load succeeds
                    }

                    throw new InvalidOperationException("filter load boom");
                }))));

        var trigger = cut.Find(".dropdown-trigger button");
        trigger.Click(); // opens, OnLoad(null) OK

        // Typing "a" pumps synchronously into the awaited OnLoad which throws — the exception
        // lands in the fire-and-forget _filterHandler task and is NEVER rethrown (part of the
        // finding: unobserved fault). Observable effect: the IsLoading latch.
        cut.Find("input[type=search]").Input("a");

        // User closes and tries again — must be able to reopen.
        trigger.Click(); // close (no guard on close)
        trigger.Click(); // reopen attempt — blocked by the IsLoading guard

        Assert.IsTrue(cut.Find(".dropdown").ClassList.Contains("is-active"),
            "A thrown filter load must not latch IsLoading forever; today the dropdown can never be opened again.");
    }
}
