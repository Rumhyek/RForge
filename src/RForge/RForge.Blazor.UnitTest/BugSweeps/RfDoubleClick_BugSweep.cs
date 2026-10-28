using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RForgeBlazor;
using System;
using System.Threading;
using BunitTestContext = Bunit.TestContext;

namespace RForge.Blazor.UnitTest.BugSweeps;

/// <summary>
/// Repro tests from the #68 bug sweep for RfDoubleClick. Red-by-construction.
/// </summary>
[TestClass]
public class RfDoubleClick_BugSweep
{
    private BunitTestContext ctx;

    [TestInitialize]
    public void Setup() => ctx = new BunitTestContext();

    [TestCleanup]
    public void Cleanup() => ctx.Dispose();

    // BUG #76 — RfDoubleClick.razor.cs:102-109: OnClickAndDoubleClick awaits Task.Delay(ClickDelay)
    // and invokes OnSingleClick with no lifetime tracking (no CTS/disposed-check) → the parent
    // callback fires after the component is removed (await-after-dispose). race, med.
    // Semi-deterministic by nature (timing window); honest per spec §6.
    // Un-ignored as the RED step of #76 (TDD contract in docs/bug-sweeps.md).
    // Repair per docs/bug-sweeps.md ("a failure for any other reason means the repro drifted"):
    // the cut.Dispose() variant never reached the component's IDisposable in-suite (instrumented:
    // OnSingleClick invoked ~270 ms BEFORE any component Dispose call, fixed and unfixed alike), so
    // it failed for a dispose-propagation artifact, not the spec'd bug. ctx.DisposeComponents() is
    // the repro the #76 body specifies ("click, DisposeComponents within window, wait") and it
    // disposes synchronously: unfixed goes fired 0 -> 1 (real post-dispose callback), fixed stays 0.
    [TestMethod]
    public void SingleClick_FiresOnlyWhileAlive()
    {
        int fired = 0;
        var cut = ctx.RenderComponent<RfDoubleClick>(
            ComponentParameter.CreateParameter("Element", "div"),
            ComponentParameter.CreateParameter("ClickDelay", 50),
            ComponentParameter.CreateParameter("OnDoubleClick",
                EventCallback.Factory.Create<MouseEventArgs>(this, (Action)(() => { }))), // enables the async path
            ComponentParameter.CreateParameter("OnSingleClick",
                EventCallback.Factory.Create<MouseEventArgs>(this, (Action)(() => Interlocked.Increment(ref fired)))));

        cut.Find("div").Click();  // schedules the delayed single-click continuation
        ctx.DisposeComponents();  // renderer-level dispose inside the 50 ms window (spec repro, synchronous)

        // Wait past the 50 ms window without any dispatcher pumping: unfixed code runs the
        // continuation on the timer thread and invokes the parent callback on the disposed component.
        Thread.Sleep(250);

        Assert.IsTrue(Volatile.Read(ref fired) == 0,
            "OnSingleClick fired after the component was disposed (no lifetime guard on the Task.Delay continuation).");
    }

    // BUG #76 — RfDoubleClick.razor.cs:73: builder.OpenElement(0, Element) with Element explicitly
    // null/empty throws ArgumentNullException every render. crash, low.
    // Planned test from the issue body, written to this convention because the #68 supporting PR
    // landed only the SingleClick repro (docs/bug-sweeps.md: write a new test when none exists).
    // Documented behavior chosen by the #76 fix: null/empty Element falls back to the parameter's
    // default element ("button") instead of throwing every render.
    [TestMethod]
    public void EmptyElement_HandledPredictably()
    {
        // Null first: this is the spec's ArgumentNullException path (OpenElement(null)).
        var missing = ctx.RenderComponent<RfDoubleClick>(
            ComponentParameter.CreateParameter("Element", null));
        Assert.IsNotNull(missing.Find("button"),
            "A null Element must fall back to the default \"button\" element instead of throwing.");

        var empty = ctx.RenderComponent<RfDoubleClick>(
            ComponentParameter.CreateParameter("Element", ""));
        Assert.IsNotNull(empty.Find("button"),
            "An empty Element must fall back to the default \"button\" element instead of rendering no queryable element.");
    }

}
