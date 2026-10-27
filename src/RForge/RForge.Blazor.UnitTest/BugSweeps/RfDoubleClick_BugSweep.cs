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
    [TestMethod]
    [Ignore("pending #76 — #68 bug-sweep repro, red-by-construction")]
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

        cut.Find("div").Click(); // schedules the delayed single-click continuation
        cut.Dispose();           // component gone — the pending callback must be dropped

        try
        {
            // Pump the test dispatcher past the 50 ms window: today the continuation runs and
            // invokes the parent callback on a disposed component.
            cut.WaitForState(() => Volatile.Read(ref fired) > 0, TimeSpan.FromMilliseconds(300));
        }
        catch (Exception)
        {
            // WaitForState failure (wait never satisfied) == callback never fired post-dispose.
        }

        Assert.IsTrue(Volatile.Read(ref fired) == 0,
            "OnSingleClick fired after the component was disposed (no lifetime guard on the Task.Delay continuation).");
    }

}
