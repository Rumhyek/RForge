#nullable enable

namespace RForgeDocs.EventsExamples.Checkout;

public partial class OrderLog
{
    private readonly List<OrderConfirmed> _orders = [];
    private int _submittedSeen;

    [Subscribe]
    private void OnConfirmed(OrderConfirmed order) => _orders.Insert(0, order);

    // Stays at 0: OrderSubmitted uses Route.Named("checkout", Broadcast), which only travels down from the checkout scope.
    [Subscribe]
    private void OnSubmitted(OrderSubmitted _) => _submittedSeen++;
}
