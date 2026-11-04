#nullable enable

using Microsoft.AspNetCore.Components;

namespace RForgeDocs.Client.EventsExamples.Islands;

/// <summary>
/// The same component, hosted in different islands. Islands that share a DI container (all Server islands, or all
/// WebAssembly islands) share a hub; islands in different containers don't.
/// </summary>
public partial class IslandPanel
{
    private string? _lastPing;
    private bool _interactive;

    [Parameter, EditorRequired]
    public string Name { get; set; } = "";

    /// <summary>The render mode this island is hosted in, "Server" or "WebAssembly". The hosting page knows it.</summary>
    [Parameter, EditorRequired]
    public string Mode { get; set; } = "";

    // OnAfterRender never runs while prerendering, so reaching it means the island is interactive.
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _interactive = true;
            StateHasChanged();
        }
    }

    [UseState]
    private partial IslandState Island { get; }

    private void Increment() => State<IslandState>().Update(s => s with { Count = s.Count + 1 });

    private Task PingAsync() => PublishAsync(new IslandPinged(Name)).AsTask();

    [Subscribe]
    private void OnPinged(IslandPinged ping) => _lastPing = ping.From == Name ? $"{ping.From} (self)" : ping.From;
}
