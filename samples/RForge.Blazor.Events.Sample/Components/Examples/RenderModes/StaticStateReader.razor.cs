namespace RForge.Blazor.Events.Sample.Components.Examples.RenderModes;

/// <summary>[UseState] in static SSR: readable, never reactive (no subscription is registered).</summary>
public partial class StaticStateReader
{
    [UseState]
    private partial IslandState Island { get; }
}
