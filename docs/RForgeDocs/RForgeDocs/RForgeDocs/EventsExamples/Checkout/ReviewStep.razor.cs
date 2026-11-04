#nullable enable

using Route = RForge.Blazor.Events.Route;
using RouteDirection = RForge.Blazor.Events.RouteDirection;

namespace RForgeDocs.EventsExamples.Checkout;

public partial class ReviewStep
{
    // Resolved from the checkout's StateScope.
    [UseState]
    private partial CheckoutForm Form { get; }

    // No StateScope provides CartState, so this falls back to the root store shared with the catalog.
    [UseState]
    private partial CartState Cart { get; }

    private static string Last4(string card) => card.Length >= 4 ? card[^4..] : card;

    private void Back() => State<CheckoutForm>().Update(f => f with { Step = 2 });

    private async Task PlaceOrderAsync()
    {
        var orderId = Guid.NewGuid();
        State<CheckoutForm>().Update(f => f with { Status = CheckoutStatus.Processing, OrderId = orderId });

        // Start at the nearest scope named "checkout" and broadcast down through it.
        // Nothing outside the checkout (e.g. the page's OrderLog) receives this.
        await PublishAsync(
            new OrderSubmitted(orderId, Form.Email, Cart.Total),
            Route.Named("checkout", RouteDirection.Broadcast));
    }
}
