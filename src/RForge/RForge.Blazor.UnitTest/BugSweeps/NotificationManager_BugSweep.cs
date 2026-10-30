using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RForge.Abstractions.Notifications;
using RForgeBlazor;
using RForgeBlazor.Services;
using System;
using System.Threading.Tasks;
using BunitTestContext = Bunit.TestContext;

namespace RForge.Blazor.UnitTest.BugSweeps;

/// <summary>
/// Repro tests from the #68 bug sweep for the notification stack
/// (NotificationManager service + RfNotificationManager component). Red-by-construction.
/// Service calls that can trigger rendering go through cut.InvokeAsync (bUnit dispatcher).
/// </summary>
[TestClass]
public class NotificationManager_BugSweep
{
    private BunitTestContext ctx;
    private NotificationManager manager;

    [TestInitialize]
    public void Setup()
    {
        ctx = new BunitTestContext();
        manager = new NotificationManager();
        ctx.Services.AddSingleton<INotificationManager>(manager);
    }

    [TestCleanup]
    public void Cleanup() => ctx.Dispose();

    private static RenderFragment Content(string text) => builder => builder.AddContent(0, text);

    // BUG #78 — RfNotificationManager.razor.cs:131-133: the OnClearAll handler body IS
    // throw new NotImplementedException() → every ClearAll() call throws into callers and
    // aborts the multicast to later subscribers. crash, high.
    [TestMethod]
    public void ClearAll_ClearsAllWithoutThrowing()
    {
        var cut = ctx.RenderComponent<RfNotificationManager>();

        cut.InvokeAsync(() =>
        {
            manager.AddError(Content("boom"));
            manager.AddInfo(Content("note"));
            manager.ClearAll(); // today: NotImplementedException straight out of the event
        }).GetAwaiter().GetResult();

        Assert.IsFalse(cut.Markup.Contains("boom") || cut.Markup.Contains("note"),
            "ClearAll must clear every notification; today the RfNotificationManager handler throws NotImplementedException.");
    }

    // BUG #79 — Services/NotificationManager.cs:22,25: ClearAllByPostion/ClearAllBySeverity
    // call OnClearBy*.Invoke(...) WITHOUT the null-conditional the siblings use (ClearAll:19,
    // OnShow:42) → NullReferenceException when no RfNotificationManager is subscribed. crash, med.
    [TestMethod]
    public void ClearBy_NoSubscribers_NoThrow()
    {
        var bare = new NotificationManager(); // no component ever subscribed — no dispatcher needed

        bare.ClearAllError();  // today: NRE on OnClearBySeverity.Invoke
        bare.ClearAllInfo();   // today: NRE
        bare.ClearAllByPostion(RfNotificationPosition.TopCenter); // today: NRE

        // Passing == "no exception escaped" — that is the fixed behavior.
    }


    // BUG #85 — RfNotificationManager.razor.cs:165-174: the severity-default merge overwrites the
    // caller's baseOptions.Position/ShowFor/ShowCloseButton BEFORE the configuration lambda runs,
    // and AddNotification(content, options) passes configuration=null → a caller-set Position is
    // silently clobbered (contradicts NotificationOptions.cs:30 docs). math, med.
    [TestMethod]
    [Ignore("pending #85 — #68 bug-sweep repro, red-by-construction")]
    public void CallerPosition_SurvivesSeverityDefaults()
    {
        var cut = ctx.RenderComponent<RfNotificationManager>();

        cut.InvokeAsync(() => manager.AddNotification(Content("mine"), new NotificationOptions
        {
            Position = RfNotificationPosition.BottomLeft,
        })).GetAwaiter().GetResult();

        Assert.IsTrue(cut.Markup.Contains("notification-container bottom-left"),
            "Caller-set Position=BottomLeft must be honored; today the severity-default merge clobbers it to TopCenter.");
    }
}
