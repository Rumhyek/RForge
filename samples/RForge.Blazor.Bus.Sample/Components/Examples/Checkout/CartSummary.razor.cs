namespace RForge.Blazor.Bus.Sample.Components.Examples.Checkout;

public partial class CartSummary
{
    [UseState]
    private partial CartState Cart { get; }

    private void Empty() => State<CartState>().Set(CartState.Initial with { Source = Cart.Source });
}
