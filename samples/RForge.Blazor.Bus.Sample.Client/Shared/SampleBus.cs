using System.Text.Json.Serialization;

namespace RForge.Blazor.Bus.Sample.Client.Shared;

/// <summary>Bus configuration shared by the server and WebAssembly hosts, so both containers behave the same.</summary>
public static class SampleBus
{
    public static void Configure(BusOptions options) => options
        // Carry the cart from prerendering into the interactive session (trim-safe JSON).
        .PersistState(SampleJsonContext.Default.CartState)
        // Let plain components read the root theme with [CascadingParameter] ThemeState.
        .CascadeRootState<ThemeState>();
}

[JsonSerializable(typeof(CartState))]
internal sealed partial class SampleJsonContext : JsonSerializerContext;
