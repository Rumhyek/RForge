using System.Text.Json.Serialization;

namespace RForgeDocs.Client.EventsExamples;

/// <summary>Hub configuration shared by the server and WebAssembly hosts, so both containers behave the same.</summary>
public static class EventsDemoHub
{
    public static void Configure(RfEventOptions options) => options
        // Carry the cart from prerendering into the interactive session (trim-safe JSON).
        .PersistState(EventsDemoJsonContext.Default.CartState)
        // Let plain components read the root theme with [CascadingParameter] ThemeState.
        .CascadeRootState<ThemeState>();
}

[JsonSerializable(typeof(CartState))]
internal sealed partial class EventsDemoJsonContext : JsonSerializerContext;
