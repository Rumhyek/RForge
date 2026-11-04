using Bunit;
using BunitTestContext = Bunit.TestContext;

namespace RForge.Blazor.UnitTest;

/// <summary>
/// bUnit's strict JS interop rejects any call it wasn't told about, and open dropdowns and visible modals
/// register a hot key. This handles only the hot key module, so any other JS call still fails the test.
/// </summary>
internal static class HotKeyTestExtensions
{
    public const string ModulePath = "./_content/RForge.Blazor.Components/rf-hotkeys.js";

    public static BunitJSModuleInterop SetupHotKeys(this BunitTestContext ctx)
    {
        var module = ctx.JSInterop.SetupModule(ModulePath);

        module.Setup<int>("register", _ => true).SetResult(1);
        module.SetupVoid("update", _ => true).SetVoidResult();
        module.SetupVoid("unregister", _ => true).SetVoidResult();

        return module;
    }
}
