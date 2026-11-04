#nullable enable

namespace RForgeDocs.EventsExamples.Checkout;

public partial class CartSummary
{
    [UseState]
    private partial CartState Cart { get; }

    private void Empty() => State<CartState>().Set(CartState.Initial with { Source = Cart.Source });
}
