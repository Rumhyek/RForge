using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RForge.Abstractions;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace RForge.Blazor.UnitTest.BugSweeps;

/// <summary>
/// Repro tests from the #68 bug sweep for RForge.Abstractions CountdownTimer.
/// Red-by-construction; un-ignore when the fix issue lands.
/// </summary>
[TestClass]
public class CountdownTimer_BugSweep
{
    // BUG #82 — CountdownTimer.cs:47: OnElapsed() sets Status=Completed AT SUBSCRIPTION (before
    // any run); the real completion path never sets Completed → Status wrong in both directions
    // for anything keyed off it. stuck, low. (timeout 200ms keeps the ctor legal — see #84.)
    [TestMethod]
    [Ignore("pending #82 — #68 bug-sweep repro, red-by-construction")]
    public void Status_LifecycleContract()
    {
        using var timer = new CountdownTimer(200);

        timer.OnElapsed(() => { });

        Assert.AreNotEqual(TimerStatus.Completed, timer.Status,
            "Subscribing an elapsed handler must not mark the timer Completed before it ever ran.");
    }

    // BUG #83 — CountdownTimer.cs:55: StartAsync has no Status==Running guard → a concurrent
    // double-start spawns a SECOND DoWorkAsync loop. Observed mechanism (red-run correction):
    // PeriodicTimer rejects the concurrent WaitForNextTickAsync claim, so the second StartAsync
    // task silently FAULTS (fire-and-forget callers never see it) — worse than the originally
    // claimed "~2x speed / elapsed twice": it is another silent-fault path, same family as #81.
    // race, med.
    [TestMethod]
    [Ignore("pending #83 — #68 bug-sweep repro, red-by-construction")]
    public void DoubleStart_ElapsedFiresOnce()
    {
        using var cts = new CancellationTokenSource();
        int elapsedFires = 0;
        var timer = new CountdownTimer(200, cts.Token)
            .OnElapsed(() => Interlocked.Increment(ref elapsedFires));

        // Real double-tap: the first call cannot be awaited before the second starts.
        var first = timer.StartAsync();
        var second = timer.StartAsync();

        var sw = Stopwatch.StartNew();
        while ((!first.IsCompleted || !second.IsCompleted) && sw.Elapsed < TimeSpan.FromSeconds(5))
        {
            Thread.Sleep(25);
        }

        cts.Cancel();

        Assert.IsFalse(first.IsFaulted, $"first StartAsync faulted silently: {first.Exception?.GetBaseException().Message}");
        Assert.IsFalse(second.IsFaulted,
            "A concurrent second StartAsync faulted the second task silently (no Status==Running guard; PeriodicTimer rejects the concurrent claim).");
        Assert.IsTrue(Volatile.Read(ref elapsedFires) <= 1, "elapsed must fire at most once per countdown");
    }

    // BUG #84 — CountdownTimer.cs:28: fixed _ticksToTimeout=100 → interval = ShowFor/100.
    // Red-run finding (harder than the originally-reported drift): for any ShowFor < 100 ms the
    // computed sub-millisecond period makes the PeriodicTimer CONSTRUCTOR throw
    // ArgumentOutOfRangeException → RfNotificationManager.OnShow (:186) throws straight into the
    // Add* caller (see NotificationManager_BugSweep / #80 interaction). The arithmetic model
    // must validate/derive ticks from the duration instead. math→crash, med.
    [TestMethod]
    [Ignore("pending #84 — #68 bug-sweep repro, red-by-construction")]
    public void ShortDuration_IsNotCrashOnCtor()
    {
        using var timer = new CountdownTimer(60); // nominal 60 ms toast lifetime

        // Today: ArgumentOutOfRangeException('period') — 60/100 = 0.6 ms is below the OS
        // timer quantum and the ctor refuses. A fix validates or re-derives the tick model.
        Assert.IsTrue(true, "reaching here means sub-100ms durations construct");
    }
}
