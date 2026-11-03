using Microsoft.AspNetCore.Components;

namespace RForge.Blazor.Bus.Sample.Client.Examples.Islands;

/// <summary>
/// The same component, hosted in different islands. Islands that share a DI container (all Server islands, or all
/// WebAssembly islands) share a bus; islands in different containers don't.
/// </summary>
public partial class IslandPanel
{
    private string? _lastPing;

    [Parameter, EditorRequired]
    public string Name { get; set; } = "";

    [UseState]
    private partial IslandState Island { get; }

    private void Increment() => State<IslandState>().Update(s => s with { Count = s.Count + 1 });

    private Task PingAsync() => PublishAsync(new IslandPinged(Name)).AsTask();

    [Subscribe]
    private void OnPinged(IslandPinged ping) => _lastPing = ping.From == Name ? $"{ping.From} (self)" : ping.From;
}
