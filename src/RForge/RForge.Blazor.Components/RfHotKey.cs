using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace RForge.Blazor;

/// <summary>
/// Runs an action when a key combo is pressed anywhere on the page. The hot key is active while this component is
/// rendered, so an <c>@if</c> around it controls when it applies. It renders no markup.
/// </summary>
/// <remarks>
/// When several active hot keys use the same combo, only the most recently rendered one fires. When it is removed the
/// previous one applies again. This is the only part of the library that loads JavaScript, and it loads it on first use.
/// </remarks>
/// <example>
/// <code>
/// @if (IsOpen == true)
/// {
///     &lt;RfHotKey Keys="@RfKey.Escape" OnPressed="Close" /&gt;
/// }
/// </code>
/// </example>
public class RfHotKey : ComponentBase, IAsyncDisposable
{
    private const string ModulePath = "./_content/RForge.Blazor.Components/rf-hotkeys.js";

    [Inject]
    private IJSRuntime JsRuntime { get; set; }

    [Inject]
    private ILogger<RfHotKey> Logger { get; set; }

    #region Parameters
    /// <summary>
    /// The combo to listen for, such as <c>mod+s</c>. See <see cref="RfKey"/> for the modifiers and named keys.
    /// </summary>
    /// <exception cref="ArgumentException">The combo isn't valid.</exception>
    [Parameter, EditorRequired]
    public string Keys { get; set; }

    /// <summary>
    /// Called when the combo is pressed.
    /// </summary>
    [Parameter]
    public EventCallback OnPressed { get; set; }

    /// <summary>
    /// If true a key with no Ctrl, Alt or Cmd also fires while typing in an input, textarea, select or contenteditable.
    /// Combos with those modifiers, Esc and the function keys always fire. By default = false
    /// </summary>
    [Parameter]
    public bool AllowWhileTyping { get; set; }
    #endregion

    private string _rawKeys;
    private string _combo;

    private IJSObjectReference _module;
    private DotNetObjectReference<RfHotKey> _reference;
    private Task _registration;
    private int _id;
    private string _sentCombo;
    private bool _sentAllowWhileTyping;
    private bool _disposed;

    /// <summary>
    /// Validates <see cref="Keys"/>, so a bad combo fails where it is written.
    /// </summary>
    protected override void OnParametersSet()
    {
        if (_rawKeys == Keys) return;

        _combo = RfKey.Normalize(Keys);
        _rawKeys = Keys;
    }

    /// <summary>
    /// Registers after the first render, which never runs during prerendering, and passes on later parameter changes.
    /// </summary>
    /// <param name="firstRender">True on the first render.</param>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender == true)
        {
            _registration = RunJsAsync(RegisterAsync);
        }

        await _registration;
        await RunJsAsync(UpdateAsync);
    }

    private async Task RegisterAsync()
    {
        _module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath);

        if (_disposed == true) return;

        _reference = DotNetObjectReference.Create(this);
        _sentCombo = _combo;
        _sentAllowWhileTyping = AllowWhileTyping;
        _id = await _module.InvokeAsync<int>("register", _reference, _sentCombo, _sentAllowWhileTyping);
    }

    private async Task UpdateAsync()
    {
        if (_disposed == true || _id == 0) return;
        if (_sentCombo == _combo && _sentAllowWhileTyping == AllowWhileTyping) return;

        _sentCombo = _combo;
        _sentAllowWhileTyping = AllowWhileTyping;
        await _module.InvokeVoidAsync("update", _id, _sentCombo, _sentAllowWhileTyping);
    }

    private async Task UnregisterAsync()
    {
        if (_id != 0)
        {
            await _module.InvokeVoidAsync("unregister", _id);
        }

        if (_module != null)
        {
            await _module.DisposeAsync();
        }
    }

    /// <summary>
    /// Runs a JS call without letting a failure reach the renderer. An unhandled exception in
    /// <see cref="OnAfterRenderAsync"/> would end a Blazor Server circuit, and a hot key that can't load isn't worth that.
    /// </summary>
    private async Task RunJsAsync(Func<Task> call)
    {
        try
        {
            await call();
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the page with it.
        }
        catch (OperationCanceledException)
        {
            // The JS call timed out or was cancelled, usually during a disconnect.
        }
        catch (JSException ex)
        {
            Logger.LogWarning(ex, "The hot key '{Keys}' is not available because a JavaScript call failed.", Keys);
        }
    }

    /// <summary>
    /// Called by JavaScript when the combo is pressed. A failing handler goes to the renderer, the same as one on an
    /// <c>@onclick</c>, so an error boundary can catch it.
    /// </summary>
    [JSInvokable]
    public async Task OnKeyPressed()
    {
        await InvokeAsync(async () =>
        {
            try
            {
                await OnPressed.InvokeAsync();
            }
            catch (Exception ex)
            {
                await DispatchExceptionAsync(ex);
            }
        });
    }

    /// <summary>
    /// Unregisters the hot key.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed == true) return;

        _disposed = true;

        if (_registration != null)
        {
            await _registration;
        }

        await RunJsAsync(UnregisterAsync);

        _reference?.Dispose();

        GC.SuppressFinalize(this);
    }
}
