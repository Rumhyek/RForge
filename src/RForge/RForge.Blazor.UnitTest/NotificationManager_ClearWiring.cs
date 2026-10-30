using Microsoft.VisualStudio.TestTools.UnitTesting;
using RForge.Abstractions.Notifications;
using RForgeBlazor.Services;
using System.Collections.Generic;

namespace RForge.Blazor.UnitTest;

/// <summary>
/// Regression tests for the #79 fix (NotificationManager ClearAllByPostion /
/// ClearAllBySeverity NRE with no subscribers). Derived from the spec's public
/// API contract, not from the implementation: the clear calls are a no-op with
/// no subscriber AND must still deliver the exact argument to subscribers.
/// Covers the members the #79 bug-sweep repro (BugSweeps/NotificationManager_BugSweep)
/// does not exercise. Pure service tests: no render, no mocks.
/// </summary>
[TestClass]
public class NotificationManager_ClearWiring
{
    // Bad path for #79: null-conditional must not silence wired subscribers.
    [TestMethod]
    public void Given_Subscriber_When_ClearAllError_Then_SubscriberReceivesErrorSeverity()
    {
        var manager = new NotificationManager();
        var seen = new List<RfNotificationSeverity>();
        manager.OnClearBySeverity += s => seen.Add(s);

        manager.ClearAllError();

        CollectionAssert.AreEqual(new[] { RfNotificationSeverity.Error }, seen,
            "A subscribed handler must receive the exact severity; the call must not become a silent no-op.");
    }

    // Bad path for #79, position overload.
    [TestMethod]
    public void Given_Subscriber_When_ClearAllByPostion_Then_SubscriberReceivesExactPosition()
    {
        var manager = new NotificationManager();
        var seen = new List<RfNotificationPosition>();
        manager.OnClearByPosition += p => seen.Add(p);

        manager.ClearAllByPostion(RfNotificationPosition.BottomRight);

        CollectionAssert.AreEqual(new[] { RfNotificationPosition.BottomRight }, seen,
            "A subscribed handler must receive the exact position; the call must not become a silent no-op.");
    }

    // Boundary for #79: every public clear overload is a no-op with no subscriber.
    // The sweep repro covers ClearAllError/ClearAllInfo/ClearAllByPostion; this
    // covers the remaining public overloads (ClearAll, ClearAllSuccess,
    // ClearAllWarning, ClearAllBySeverity direct).
    [TestMethod]
    public void Given_NoSubscriber_When_EveryClearOverload_Then_NoThrow()
    {
        var bare = new NotificationManager();

        bare.ClearAll();
        bare.ClearAllSuccess();
        bare.ClearAllWarning();
        bare.ClearAllBySeverity(RfNotificationSeverity.Info);
        bare.ClearAllByPostion(RfNotificationPosition.TopRight);

        // Passing == no exception escaped on any clear overload without a subscriber.
    }
}
