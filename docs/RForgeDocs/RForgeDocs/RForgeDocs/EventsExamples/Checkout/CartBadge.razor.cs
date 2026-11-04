#nullable enable

namespace RForgeDocs.EventsExamples.Checkout;

/// <summary>Combines a selector (the count) with an event (a pulse animation when something is added).</summary>
public partial class CartBadge
{
    private int _renders;
    private bool _pulse;

    [UseState(Select = nameof(SelectCount))]
    private partial int Count { get; }

    private static int SelectCount(CartState cart) => cart.Count;

    [Subscribe]
    private void OnItemAdded(ItemAdded added)
    {
        _pulse = true;
        // Delivery is sequential, so don't await the animation here: that would hold up the publisher and later handlers.
        _ = EndPulseAsync();
    }

    private async Task EndPulseAsync()
    {
        try
        {
            await Task.Delay(600, DisposalToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        _pulse = false;
        StateHasChanged();
    }
}
