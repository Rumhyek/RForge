using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RForgeBlazor;
using RForgeBlazor.Models;
using RForgeBlazor.Services;
using System;
using System.Threading.Tasks;
using BunitTestContext = Bunit.TestContext;

namespace RForge.Blazor.UnitTest.BugSweeps;

/// <summary>
/// Repro tests from the #68 bug sweep for the dialog stack (DialogManager + RfDialogManager).
/// Red-by-construction; un-ignore when the fix issue lands.
/// </summary>
[TestClass]
public class DialogManager_BugSweep
{
    private BunitTestContext ctx;
    private DialogManager dialogManager;

    [TestInitialize]
    public void Setup()
    {
        ctx = new BunitTestContext();
        dialogManager = new DialogManager();
        // Pre-created instance shared by both rendered managers and the test body.
        ctx.Services.AddSingleton(dialogManager);
        ctx.Services.AddSingleton<IDialogManager>(dialogManager);
    }

    [TestCleanup]
    public void Cleanup() => ctx.Dispose();

    // BUG #86 — RfDialogManager.razor.cs:223 (DialogManager.cs:228-231): Dispose → Unregister()
    // unconditionally nulls _dialogManager with no registrant-identity check. Disposing the OLD
    // manager after a swap unregisters the NEW one: every later Alert/Confirm/Prompt queues
    // forever and no dialog ever renders again. stuck, med.
    [TestMethod]
    public void OlderInstanceDisposed_DialogsStillShow()
    {
        var first = ctx.RenderComponent<RfDialogManager>();
        var second = ctx.RenderComponent<RfDialogManager>(); // swap: singleton now points here

        // Invoke the component's OWN Dispose logic (what Blazor calls when the hosting page is
        // removed) directly — bUnit's cut.Dispose() detaches other roots and would muddy the repro.
        ((IDisposable)first.Instance).Dispose(); // OLD instance disposed → today this unregisters the NEW one

        second.InvokeAsync(() => dialogManager.Alert("after-swap alert")).GetAwaiter().GetResult();

        // The still-alive second manager must render the modal.
        // The still-alive second manager must render the modal.
        Assert.IsTrue(second.Markup.Contains("modal"),
            "Disposing a replaced RfDialogManager must not unregister the active one; today every later dialog queues forever.");
    }

    // BUG #87 — RfDialogManager.razor.cs:99,137,165: catch{} swallows EVERY user-callback failure,
    // including a guaranteed NRE when OnAlert/OnConfirm/OnPrompt are null. The dialog closes as if
    // the action SUCCEEDED and the operation is lost with zero diagnostics. crash, low.
    [TestMethod]
    public void FailingCallback_IsNotSilentlyEaten()
    {
        var cut = ctx.RenderComponent<RfDialogManager>();

        cut.InvokeAsync(() => dialogManager.Confirm(new RfDialogOptionConfirm
        {
            ConfirmText = "Go",
            CancelText = "No",
            OnConfirm = null, // consumer forgot the callback → deref inside is a guaranteed NRE
        })).GetAwaiter().GetResult();

        var confirmButton = cut.Find("button.is-primary");

        try
        {
            confirmButton.Click(); // today: NRE inside is eaten by catch{}, dialog closes "as success"
            Assert.Fail("A null/failing user callback was swallowed by catch{} — the dialog closed as if the action succeeded.");
        }
        catch (NullReferenceException)
        {
            // Fixed behavior: the failure surfaces to the caller/diagnostics instead of vanishing.
        }
    }

    // BUG #88 — RfDialogManager.razor.cs:71 (and RfNotificationManager.razor.cs:193): Show()
    // mutates state and calls StateHasChanged directly from service events, but
    // IDialogManagerBackend is documented for server-side PUSH callers — a thread-pool caller
    // (SignalR hub, timer) throws "not associated with the Dispatcher" / renders off-context. race, med.
    [TestMethod]
    public void BackendPushFromBackgroundThread_Works()
    {
        var cut = ctx.RenderComponent<RfDialogManager>();

        IDialogManager backend = dialogManager;
        Exception pushed = null;

        try
        {
            // Documented scenario: backend pushes a dialog from a background thread.
            Task.Run(() => backend.Alert("pushed from pool")).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            pushed = ex;
        }

        Assert.IsNull(pushed,
            "Backend push from a thread-pool thread must work (dispatch internally); today Show()/StateHasChanged runs off-context and throws.");
        cut.WaitForState(() => cut.Markup.Contains("pushed from pool"));
    }
}
