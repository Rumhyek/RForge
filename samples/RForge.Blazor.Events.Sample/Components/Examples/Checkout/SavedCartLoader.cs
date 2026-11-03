using System.Collections.Immutable;

namespace RForge.Blazor.Events.Sample.Components.Examples.Checkout;

/// <summary>
/// Loads a "saved cart" once. During prerendering this runs in the prerender's request scope; because CartState is
/// opted into persistence (SampleEventHub.Configure), the interactive circuit restores it instead of loading again.
/// </summary>
public sealed class SavedCartLoader : RfEventComponent
{
    protected override async Task OnInitializedAsync()
    {
        var cart = State<CartState>();
        if (cart.Value.Source is not null)
        {
            return; // Already loaded (or restored from the prerender).
        }

        await Task.Delay(150); // Pretend to call a database.

        cart.Set(new CartState(
            ImmutableList.Create(
                new CartLine(1, "Coffee beans", "☕", 14.50m, 1),
                new CartLine(2, "Croissant", "🥐", 3.25m, 2)),
            $"Saved cart loaded during the \"{RendererInfo.Name}\" render at {DateTime.Now:HH:mm:ss}. "
            + "If this says \"Static\", it was carried over from prerendering by PersistState<CartState>()."));
    }
}
