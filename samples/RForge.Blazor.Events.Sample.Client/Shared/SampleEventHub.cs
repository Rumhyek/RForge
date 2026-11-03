using System.Text.Json.Serialization;

namespace RForge.Blazor.Events.Sample.Client.Shared;

/// <summary>Hub configuration shared by the server and WebAssembly hosts, so both containers behave the same.</summary>
public static class SampleEventHub
{
    public static void Configure(RfEventOptions options) => options
        // Carry the cart from prerendering into the interactive session (trim-safe JSON).
        .PersistState(SampleJsonContext.Default.CartState)
        // Let plain components read the root theme with [CascadingParameter] ThemeState.
        .CascadeRootState<ThemeState>();
}

[JsonSerializable(typeof(CartState))]
internal sealed partial class SampleJsonContext : JsonSerializerContext;
