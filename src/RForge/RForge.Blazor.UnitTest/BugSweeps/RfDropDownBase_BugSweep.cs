using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RForgeBlazor;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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

    // Poll a predicate directly: the pump writes these test-side histories from a continuation
    // after its last render, and bUnit's WaitForState re-checks only on renderer notifications.
    // Named apart from other sweep helpers so fix PRs touching this file stay mergeable.
    private static bool RateLimitPollUntil(Func<bool> condition, int timeoutMs = 5000)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            if (condition() == true) return true;
            Thread.Sleep(5);
        }
        return condition() == true;
    }

    // BUG #72 — CANARY-3: the rate-limit loop sleeps Task.Delay((int)elapsed + 1), the ALREADY-
    // ELAPSED milliseconds, instead of FilterRateLimit minus elapsed: the pump wakes and re-sleeps
    // roughly the elapsed window again (a geometric re-peek loop, ~8 wakes for a 250ms config)
    // instead of waiting the REMAINING window once. The same math reads the wall clock
    // (DateTime.Now), so a backward clock jump can hand Task.Delay a negative argument and kill
    // the pump; that half is non-reproducible without a time seam (spec note) and the fix uses a
    // monotonic Stopwatch timestamp plus a clamp instead.
    // Deterministic seam: park the pump inside a gated OnLoad("a"); type "b" and hold the gate
    // ~200 ms past b's keystroke, then release. When the pump next checks "b", elapsed(b) is
    // ~200 of the 250 ms window. Correct math waits the ~50 ms remaining and loads "b" ~250 ms
    // after its keystroke. The elapsed-sleep waits ~201 ms, re-peeks at ~401 ms, and loads "b"
    // there: a 2x+ overshoot that cannot land below the asserted band.
    // Verdict table row: Models/RfDropDownBase.cs:239 (see also 226, 233), math, med.
    [TestMethod]
    public void RateLimit_WaitsRemainingNotElapsed()
    {
        var history = new ConcurrentQueue<string>();
        var gates = new ConcurrentDictionary<string, TaskCompletionSource<bool>>();

        Func<string, Task> onLoad = async filter =>
        {
            var key = filter ?? "<null>";
            var gate = gates.GetOrAdd(key, _ =>
                new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously));
            history.Enqueue("start:" + key);
            await gate.Task;
            history.Enqueue("end:" + key);
        };

        var cut = ctx.RenderComponent<RfDropDown<string>>(
            ComponentParameter.CreateParameter("FilterRateLimit", 250),
            ComponentParameter.CreateParameter("IsOpen", true),
            ComponentParameter.CreateParameter("Items", new List<string> { "a", "b" }),
            ComponentParameter.CreateParameter("ChildContent",
                (RenderFragment<string>)(item => builder => builder.AddContent(0, item))),
            ComponentParameter.CreateParameter("OnLoad", EventCallback.Factory.Create<string>(this, onLoad)));

        cut.Find("input[type=search]").Input("a"); // pump waits, pops "a", parks in gated OnLoad("a")
        Assert.IsTrue(RateLimitPollUntil(() => history.ToArray().Contains("start:a")),
            "OnLoad('a') never started; history: " + string.Join(", ", history.ToArray()));

        var sinceKeystrokeB = Stopwatch.StartNew();
        cut.Find("input[type=search]").Input("b"); // queued while "a" is still awaited

        Thread.Sleep(200);           // "b" is ~200 ms old when the pump next rate-checks it
        gates["a"].SetResult(true);  // release: the pump finishes "a", then rate-limits "b"

        Assert.IsTrue(RateLimitPollUntil(() => history.ToArray().Contains("start:b")),
            "OnLoad('b') never started; history: " + string.Join(", ", history.ToArray()));
        var latencyBMs = sinceKeystrokeB.ElapsedMilliseconds;

        gates["b"].SetResult(true);
        Assert.IsTrue(RateLimitPollUntil(() => history.ToArray().Contains("end:b")),
            "OnLoad('b') never completed; history: " + string.Join(", ", history.ToArray()));

        // The wait for "b" must be its REMAINING window (250 - ~200), so "b" loads about
        // FilterRateLimit after its keystroke. Sleeping the ELAPSED window (~201 ms) and
        // re-peeking lands "b" at ~401 ms: the loop then only exits once elapsed >= 250 again,
        // which cannot happen below 2*elapsed on this schedule.
        Assert.IsTrue(latencyBMs >= 200,
            $"'b' loaded after only {latencyBMs} ms: the rate limit window was not honored.");
        Assert.IsTrue(latencyBMs <= 330,
            $"'b' loaded {latencyBMs} ms after its keystroke, expected ~250 ms: the pump slept the "
            + "already-elapsed window and re-peeked instead of waiting FilterRateLimit minus elapsed "
            + "(CANARY-3, busy re-peek loop with 2x+ overshoot).");
    }
}
