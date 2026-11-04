#nullable enable

using Microsoft.AspNetCore.Components.Rendering;

namespace RForgeDocs.EventsExamples.Checkout;

/// <summary>
/// An invisible worker component: async handler, cancellation via the component's lifetime, state writes in two
/// different scopes, and a follow-up event that bubbles out of the checkout.
/// </summary>
public sealed partial class PaymentProcessor : RfEventComponent
{
    [Subscribe(Render = false)]
    private async Task OnSubmitted(OrderSubmitted order, CancellationToken cancellationToken)
    {
        // The token is cancelled if the user navigates away mid-payment.
        await Task.Delay(1500, cancellationToken);

        var cart = State<CartState>();               // root store
        var items = cart.Value.Count;
        cart.Set(CartState.Initial with { Source = "Cart emptied after the last order." });

        State<CheckoutForm>().Update(f => f with { Status = CheckoutStatus.Confirmed }); // checkout's own store

        // Default route (Bubble): checkout scopes, then up to the root where the page's OrderLog listens.
        await PublishAsync(new OrderConfirmed(order.OrderId, order.Total, items, DateTime.Now), cancellationToken: cancellationToken);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
    }
}
