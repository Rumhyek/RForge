using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;
using BunitTestContext = Bunit.TestContext;

namespace RForge.Blazor.UnitTest;

[TestClass]
public class RfHotKey_Registration
{
    private BunitTestContext ctx;

    [TestInitialize]
    public void Setup() => ctx = new BunitTestContext();

    [TestCleanup]
    public void Cleanup() => ctx.Dispose();

    [TestMethod]
    public void Given_Rendered_When_FirstRender_Then_RegistersNormalizedCombo()
    {
        var module = ctx.SetupHotKeys();

        ctx.RenderComponent<RfHotKey>(p => p
            .Add(x => x.Keys, "Shift+Mod+K")
            .Add(x => x.AllowWhileTyping, true));

        var register = module.VerifyInvoke("register");
        Assert.AreEqual("shift+mod+k", register.Arguments[1]);
        Assert.AreEqual(true, register.Arguments[2]);
    }

    [TestMethod]
    public void Given_Rendered_When_Rendered_Then_NoMarkup()
    {
        ctx.SetupHotKeys();

        var cut = ctx.RenderComponent<RfHotKey>(p => p.Add(x => x.Keys, "esc"));

        Assert.AreEqual(string.Empty, cut.Markup);
    }

    [TestMethod]
    public void Given_InvalidCombo_When_Rendered_Then_Throws()
    {
        ctx.SetupHotKeys();

        _ = Assert.ThrowsException<ArgumentException>(() =>
            ctx.RenderComponent<RfHotKey>(p => p.Add(x => x.Keys, "ctrl+")));
    }

    [TestMethod]
    public void Given_Registered_When_ParameterChanges_Then_UpdatesInPlace()
    {
        var module = ctx.SetupHotKeys();
        var cut = ctx.RenderComponent<RfHotKey>(p => p.Add(x => x.Keys, "a"));

        cut.SetParametersAndRender(p => p.Add(x => x.Keys, "b"));

        Assert.AreEqual(1, module.Invocations["register"].Count);
        Assert.AreEqual("b", module.VerifyInvoke("update").Arguments[1]);
    }

    [TestMethod]
    public void Given_Registered_When_ParentRerendersUnchanged_Then_NoUpdate()
    {
        var module = ctx.SetupHotKeys();
        var cut = ctx.RenderComponent<RfHotKey>(p => p.Add(x => x.Keys, "a"));

        cut.SetParametersAndRender(p => p.Add(x => x.Keys, "a"));

        module.VerifyNotInvoke("update");
    }

    [TestMethod]
    public void Given_Registered_When_Disposed_Then_Unregisters()
    {
        var module = ctx.SetupHotKeys();
        ctx.RenderComponent<RfHotKey>(p => p.Add(x => x.Keys, "a"));

        ctx.DisposeComponents();

        module.VerifyInvoke("unregister");
    }

    [TestMethod]
    public async Task Given_Registered_When_Pressed_Then_InvokesCallback()
    {
        ctx.SetupHotKeys();
        int pressed = 0;
        var cut = ctx.RenderComponent<RfHotKey>(p => p
            .Add(x => x.Keys, "a")
            .Add(x => x.OnPressed, () => pressed++));

        await cut.Instance.OnKeyPressed();

        Assert.AreEqual(1, pressed);
    }

    [TestMethod]
    public async Task Given_ThrowingHandler_When_Pressed_Then_GoesToRenderer()
    {
        ctx.SetupHotKeys();
        var cut = ctx.RenderComponent<RfHotKey>(p => p
            .Add(x => x.Keys, "a")
            .Add(x => x.OnPressed, (Action)(() => throw new InvalidOperationException("boom"))));

        await cut.Instance.OnKeyPressed();

        var captured = await ctx.Renderer.UnhandledException;
        Assert.IsInstanceOfType<InvalidOperationException>(captured);
    }

    [TestMethod]
    public void Given_ImportFails_When_Rendered_Then_NoThrowAndDisposeIsClean()
    {
        var jsRuntime = new FakeJsRuntime();
        ctx.Services.AddSingleton<IJSRuntime>(jsRuntime);
        jsRuntime.Import.SetException(new JSException("blocked by CSP"));

        ctx.RenderComponent<RfHotKey>(p => p.Add(x => x.Keys, "a"));

        ctx.DisposeComponents();

        Assert.IsFalse(ctx.Renderer.UnhandledException.IsCompleted);
    }

    [TestMethod]
    public void Given_RegisterFails_When_Rendered_Then_NoThrowAndNoUnregister()
    {
        var module = ctx.JSInterop.SetupModule(HotKeyTestExtensions.ModulePath);
        module.Setup<int>("register", _ => true).SetException(new JSException("boom"));

        ctx.RenderComponent<RfHotKey>(p => p.Add(x => x.Keys, "a"));

        ctx.DisposeComponents();

        Assert.IsFalse(ctx.Renderer.UnhandledException.IsCompleted);
        module.VerifyNotInvoke("unregister");
    }

    [TestMethod]
    public void Given_Disconnected_When_Disposed_Then_NoThrow()
    {
        var module = ctx.JSInterop.SetupModule(HotKeyTestExtensions.ModulePath);
        module.Setup<int>("register", _ => true).SetResult(1);
        module.SetupVoid("unregister", _ => true).SetException(new JSDisconnectedException("gone"));

        ctx.RenderComponent<RfHotKey>(p => p.Add(x => x.Keys, "a"));

        ctx.DisposeComponents();

        Assert.IsFalse(ctx.Renderer.UnhandledException.IsCompleted);
    }

    [TestMethod]
    public void Given_DisposedDuringImport_When_ImportCompletes_Then_NeverRegisters()
    {
        var jsRuntime = new FakeJsRuntime();
        ctx.Services.AddSingleton<IJSRuntime>(jsRuntime);
        var module = new FakeModule();

        ctx.RenderComponent<RfHotKey>(p => p.Add(x => x.Keys, "a"));
        ctx.DisposeComponents();

        jsRuntime.Import.SetResult(module);

        Assert.IsTrue(SpinWait.SpinUntil(() => module.Disposed, TimeSpan.FromSeconds(5)), "The module must still be released.");
        Assert.AreEqual(0, module.Calls);
    }

    // bUnit doesn't let a test decide how the import completes.
    private sealed class FakeJsRuntime : IJSRuntime
    {
        public TaskCompletionSource<IJSObjectReference> Import { get; } = new();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args) =>
            new((Task<TValue>)(object)Import.Task);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args) =>
            InvokeAsync<TValue>(identifier, args);
    }

    private sealed class FakeModule : IJSObjectReference
    {
        public int Calls { get; private set; }
        public bool Disposed { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args)
        {
            Calls++;
            return default;
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args)
        {
            Calls++;
            return default;
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return default;
        }
    }
}
