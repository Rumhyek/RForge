using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RForgeBlazor;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
    public void OpenDropDown_ThrowingOnLoad_DoesNotLatchForever()
    {
        var cut = ctx.RenderComponent<RfDropDown<string>>(
            ComponentParameter.CreateParameter("OnLoad", EventCallback.Factory.Create<string>(this,
                (Func<string, Task>)(filter => throw new InvalidOperationException("consumer OnLoad boom")))));

        var trigger = cut.Find(".dropdown-trigger button");

        // First click: OnLoad throws. The consumer's exception must propagate (contract),
        // and the component must not keep the latch after the throw.
        _ = Assert.ThrowsException<InvalidOperationException>(() => trigger.Click());

        // GREEN-RUN CORRECTION (#71 red-run-note precedent): the fake OnLoad throws on EVERY
        // call, so the original bare second click could never reach is-active — a retry with
        // a still-throwing load legitimately THROWS again. Pre-fix this click proves the bug
        // too: the latch is set, the guard returns silently, no exception.
        _ = Assert.ThrowsException<InvalidOperationException>(() => trigger.Click());

        // Re-render with a load that succeeds: one click must now open (is-active).
        // Today IsLoading is still true from click 1, the guard returns, the spinner stays:
        // dropdown dead forever.
        cut.SetParametersAndRender(ComponentParameter.CreateParameter("OnLoad",
            EventCallback.Factory.Create<string>(this,
                (Func<string, Task>)(filter => Task.CompletedTask))));

        trigger = cut.Find(".dropdown-trigger button");
        trigger.Click();

        Assert.IsTrue(cut.Find(".dropdown").ClassList.Contains("is-active"),
            "Dropdown must be openable after a failed load attempt; today the IsLoading latch never clears (no try/finally).");
    }

    // BUG #71 — filter pump latch: handleFilters sets IsLoading=true, awaits
    // FilterChanged/OnLoad, clears with NO try/finally; a throwing filter load latches
    // IsLoading forever AND the fire-and-forget task exception is unobserved.
    // (Red-run note: the claimed "keystroke loss via Clear after awaits" did NOT reproduce —
    // _filterChanges.Clear() runs at POP time, before the awaits; the FilterTypedDuringLoad_NotLost
    // test below pins that acceptance behavior as a characterization test.)
    // Verdict table row: Models/RfDropDownBase.cs:257-262 (start 277), stuck, high.
    // Un-ignored #71 as the TDD red step. Pre-fix, the sync throw from the consumer OnLoad
    // escapes the fire-and-forget pump right through the keystroke dispatch and fails this
    // test at the Input() line; the IsLoading latch below is the same missing finally.
    [TestMethod]
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

    private static TaskCompletionSource<bool> PendingGate() =>
        new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

    // bUnit's WaitForState re-checks its predicate on component renders. The pump writes these
    // test-side histories from a continuation that has already completed its last render, so
    // render-coupled waits can miss the write; poll the predicate directly instead.
    private static bool WaitUntil(Func<bool> condition, int timeoutMs = 5000)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            if (condition() == true) return true;
            Thread.Sleep(5);
        }
        return condition() == true;
    }

    // BUG #71 — CANARY-2: Dispose() only clears the filter stack and calls Task.Dispose() on
    // _filterHandler, but Task.Dispose cancels nothing (the pump had no CTS), so an in-flight
    // handleFilters continues through its consumer callbacks after the component is disposed.
    // Deterministic seam: the pump parks INSIDE the awaited FilterChanged callback (gated),
    // Dispose runs, the callback then completes — the pending OnLoad fires into the disposed
    // component. (The bare "dispose inside the 10ms rate-limit wake window" variant of the
    // sweep note is a µs race, not a test: pre-fix Dispose() itself clears the stack, so the
    // pump there survives only as an invisible spin.) Post-fix Dispose cancels the pump and
    // the invoke sequence unwinds before the next callback.
    // Verdict table row: Models/RfDropDownBase.cs:326-332 vs 259-262, race, high.
    [TestMethod]
    public void Dispose_InFlightFilterHandler_DoesNotInvokeAfterDispose()
    {
        var postDisposeOnLoad = 0;
        var disposed = false;
        var filterChangedGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        Func<string, Task> spy = filter =>
        {
            if (disposed == true)
                Interlocked.Increment(ref postDisposeOnLoad);
            return Task.CompletedTask;
        };

        Func<string, Task> gatedFilterChanged = async filter =>
        {
            await filterChangedGate.Task; // park the pump inside the load sequence
        };

        var cut = ctx.RenderComponent<RfDropDown<string>>(
            ComponentParameter.CreateParameter("FilterRateLimit", 10),
            ComponentParameter.CreateParameter("IsOpen", true),
            ComponentParameter.CreateParameter("Items", new List<string> { "x", "xy" }),
            ComponentParameter.CreateParameter("ChildContent",
                (RenderFragment<string>)(item => builder => builder.AddContent(0, item))),
            ComponentParameter.CreateParameter("FilterChanged", EventCallback.Factory.Create<string>(this, gatedFilterChanged)),
            ComponentParameter.CreateParameter("OnLoad", EventCallback.Factory.Create<string>(this, spy)));

        cut.Find("input[type=search]").Input("x"); // pump pops "x" and parks inside FilterChanged
        Assert.IsTrue(WaitUntil(() => cut.Markup.Contains("is-loading")),
            "the pump never entered the load; markup never showed the loading state");

        disposed = true;
        ctx.Dispose(); // dispose while the pump is mid-sequence, after its pop
        filterChangedGate.SetResult(true); // the awaited callback completes post-dispose

        Thread.Sleep(100); // a surviving pump resumes and fires OnLoad into the disposed component

        Assert.AreEqual(0, postDisposeOnLoad,
            "OnLoad must never be invoked after Dispose; Task.Dispose() cancels nothing, so today "
            + "the pump resumes inside the disposed component and fires the consumer callback (CANARY-2).");
    }

    // BUG #71 — keystroke loss: the sweep finding claimed keystrokes typed during an awaited
    // OnLoad are wiped by _filterChanges.Clear() running "after the awaits". Red-run correction
    // (already noted on the latch test above): Clear() runs at POP time, BEFORE the awaits, and
    // the while-loop re-checks the stack after the load, so the latest filter does reach OnLoad.
    // This test pins that acceptance behavior (Given a load in flight, When a new filter is
    // typed, Then OnLoad eventually runs with the latest text and nothing else is loaded).
    // It is green before AND after the #71 fix by design: a characterization guard, not a red
    // repro; the pump rework (CTS/finally/catch) must not regress it.
    [TestMethod]
    public void FilterTypedDuringLoad_NotLost()
    {
        var history = new ConcurrentQueue<string>();
        var gates = new ConcurrentDictionary<string, TaskCompletionSource<bool>>();

        Func<string, Task> onLoad = async filter =>
        {
            var key = filter ?? "<null>";
            var gate = gates.GetOrAdd(key, _ => PendingGate());
            history.Enqueue("start:" + key);
            await gate.Task;
            history.Enqueue("end:" + key);
        };

        var cut = ctx.RenderComponent<RfDropDown<string>>(
            ComponentParameter.CreateParameter("FilterRateLimit", 0),
            ComponentParameter.CreateParameter("IsOpen", true),
            ComponentParameter.CreateParameter("Items", new List<string> { "a", "ab" }),
            ComponentParameter.CreateParameter("ChildContent",
                (RenderFragment<string>)(item => builder => builder.AddContent(0, item))),
            ComponentParameter.CreateParameter("OnLoad", EventCallback.Factory.Create<string>(this, onLoad)));

        cut.Find("input[type=search]").Input("a");  // pump pops "a" and suspends inside OnLoad("a")
        Assert.IsTrue(WaitUntil(() => history.ToArray().Contains("start:a")),
            "OnLoad('a') never started; history: " + string.Join(", ", history.ToArray()));

        cut.Find("input[type=search]").Input("ab"); // typed while the awaited load is still in flight

        gates["a"].SetResult(true);                 // the load for "a" finally completes
        Assert.IsTrue(WaitUntil(() => history.ToArray().Contains("start:ab")),
            "the filter typed during the awaited load never reached OnLoad; history: " + string.Join(", ", history.ToArray()));
        gates["ab"].SetResult(true);
        Assert.IsTrue(WaitUntil(() => history.ToArray().Contains("end:ab")),
            "OnLoad('ab') never completed; history: " + string.Join(", ", history.ToArray()));

        Thread.Sleep(50); // nothing else should ever load

        var snapshot = history.ToArray();
        Assert.AreEqual("end:ab", snapshot[snapshot.Length - 1],
            "The latest filter must survive an awaited load and reach OnLoad; history: " + string.Join(", ", snapshot));
        Assert.AreEqual(1, snapshot.Count(h => h == "start:ab"),
            "The latest filter must load exactly once; history: " + string.Join(", ", snapshot));
    }

    // BUG #71 — double pump on close/reopen: CloseDropDown and OpenDropDown null _filterHandler
    // without cancelling it, so the next keystroke can start a SECOND concurrent pump over the
    // same shared stack while the first is still alive. Deterministic seam: pump 1 parks
    // rate-waiting (IsLoading not set yet, so the reopen click is not guarded), close+reopen
    // orphan it without cancelling, a keystroke starts pump 2 whose OnLoad("b") is gated, then a
    // third keystroke ("c") must load exactly once and only after "b" completes. Pre-fix either
    // the surviving orphan steals "c" out of order while "b" is still awaited, or the orphan
    // dies at the empty-stack pop check and "c" is never loaded at all (pump 2 is busy, so no
    // new pump starts) — both histories violate this acceptance bullet. Post-fix close/reopen
    // cancels the orphan, pump 2 stays the single owner, and loads b then c serially.
    // Verdict table row: Models/RfDropDownBase.cs:290-291,315-316, race, med.
    [TestMethod]
    public void CloseMidLoad_DoesNotStartSecondPump()
    {
        var history = new ConcurrentQueue<string>();
        var gates = new ConcurrentDictionary<string, TaskCompletionSource<bool>>();

        Func<string, Task> onLoad = async filter =>
        {
            var key = filter ?? "<null>";
            var gate = gates.GetOrAdd(key, _ => PendingGate());
            history.Enqueue("start:" + key);
            await gate.Task;
            history.Enqueue("end:" + key);
        };

        var nullGate = PendingGate(); // the reopen's open-load (Filter=null) must not block
        nullGate.SetResult(true);
        gates["<null>"] = nullGate;

        var cut = ctx.RenderComponent<RfDropDown<string>>(
            ComponentParameter.CreateParameter("FilterRateLimit", 20),
            ComponentParameter.CreateParameter("IsOpen", true),
            ComponentParameter.CreateParameter("Items", new List<string> { "b", "c" }),
            ComponentParameter.CreateParameter("ChildContent",
                (RenderFragment<string>)(item => builder => builder.AddContent(0, item))),
            ComponentParameter.CreateParameter("OnLoad", EventCallback.Factory.Create<string>(this, onLoad)));

        var trigger = cut.Find(".dropdown-trigger button");

        cut.Find("input[type=search]").Input("a"); // pump 1 parks in the rate-limit wait for "a"
        Thread.Sleep(5);                            // pump 1 still waiting (20ms gate not reached)

        trigger.Click(); // close: clears the stack and orphans pump 1 without cancelling it
        trigger.Click(); // reopen: succeeds, pump 1 never set IsLoading (still rate-waiting)

        cut.Find("input[type=search]").Input("b"); // keystroke starts pump 2 while pump 1 is alive
        Assert.IsTrue(WaitUntil(() => history.ToArray().Contains("start:b")),
            "OnLoad('b') never started; history: " + string.Join(", ", history.ToArray()));

        cut.Find("input[type=search]").Input("c"); // arrives while "b" is still gated
        Thread.Sleep(80); // enough for the surviving orphan to steal "c" while "b" is still gated

        gates["b"].SetResult(true);
        Assert.IsTrue(WaitUntil(() => history.ToArray().Contains("start:c")),
            "the 'c' keystroke was never loaded; history: " + string.Join(", ", history.ToArray()));
        gates["c"].SetResult(true);
        Assert.IsTrue(WaitUntil(() => Array.IndexOf(history.ToArray(), "end:c") >= 0),
            "OnLoad('c') never completed; history: " + string.Join(", ", history.ToArray()));
        var snapshot = history.ToArray();

        Assert.AreEqual(1, snapshot.Count(h => h == "start:b"),
            "The filter must be loaded exactly once per keystroke; history: " + string.Join(", ", snapshot));
        Assert.AreEqual(1, snapshot.Count(h => h == "start:c"),
            "The filter must be loaded exactly once per keystroke; history: " + string.Join(", ", snapshot));
        Assert.IsTrue(Array.IndexOf(snapshot, "start:c") > Array.IndexOf(snapshot, "end:b"),
            "OnLoad('c') must not start while OnLoad('b') is still awaiting; history: " + string.Join(", ", snapshot));
    }
}
