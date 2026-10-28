using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RForgeBlazor;
using System;
using System.Diagnostics;
using System.Threading;
using BunitTestContext = Bunit.TestContext;

namespace RForge.Blazor.UnitTest;

/// <summary>
/// Good-path coverage for the #76 acceptance criteria: the single-click lifetime
/// guard must only suppress post-dispose callbacks, never the live behavior.
/// Companion of BugSweeps/RfDoubleClick_BugSweep.cs (which covers the dispose race
/// and the empty/null Element fallback).
/// </summary>
[TestClass]
public class RfDoubleClick_Lifecycle
{
    private BunitTestContext ctx;

    [TestInitialize]
    public void Setup() => ctx = new BunitTestContext();

    [TestCleanup]
    public void Cleanup() => ctx.Dispose();

    // #76 acceptance (good path): a single click on a LIVE component still invokes
    // OnSingleClick exactly once after the click-delay window closes.
    [TestMethod]
    public void SingleClick_FiresExactlyOnceWhileAlive()
    {
        int fired = 0;
        var cut = ctx.RenderComponent<RfDoubleClick>(
            ComponentParameter.CreateParameter("ClickDelay", 50),
            ComponentParameter.CreateParameter("OnDoubleClick",
                EventCallback.Factory.Create<MouseEventArgs>(this, (Action)(() => { }))), // enables the async path
            ComponentParameter.CreateParameter("OnSingleClick",
                EventCallback.Factory.Create<MouseEventArgs>(this, (Action)(() => Interlocked.Increment(ref fired)))));

        cut.Find("button").Click(); // default Element renders as "button"

        // bUnit 1.x WaitForState re-checks on renderer notifications, not on a timer
        // (repo pitfall): poll externally with a stopwatch for the callback instead.
        var sw = Stopwatch.StartNew();
        while (Volatile.Read(ref fired) == 0 && sw.ElapsedMilliseconds < 1000)
        {
            Thread.Sleep(5);
        }

        Assert.IsTrue(Volatile.Read(ref fired) == 1,
            $"OnSingleClick must fire exactly once for a live single click (fired={Volatile.Read(ref fired)}).");

        Thread.Sleep(150); // well past the 50 ms window: no extra invocations
        Assert.IsTrue(Volatile.Read(ref fired) == 1,
            $"OnSingleClick fired more than once for a single live click (fired={Volatile.Read(ref fired)}).");
    }
}
