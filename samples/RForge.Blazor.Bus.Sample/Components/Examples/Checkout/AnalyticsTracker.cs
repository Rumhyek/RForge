using Microsoft.AspNetCore.Components.Rendering;

namespace RForge.Blazor.Bus.Sample.Components.Examples.Checkout;

/// <summary>
/// A listener that fails for some inputs. Its exception goes to the nearest ErrorBoundary via
/// DispatchExceptionAsync; the publisher and the other handlers (PaymentProcessor) are unaffected.
/// </summary>
public sealed partial class AnalyticsTracker : BusComponentBase
{
    private int _tracked;

    [Subscribe]
    private void OnSubmitted(OrderSubmitted order)
    {
        if (order.Email.Contains("crash", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"analytics endpoint rejected order {order.OrderId.ToString()[..8]}");
        }

        _tracked++;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder) =>
        builder.AddContent(0, $"📈 Analytics: {_tracked} order(s) tracked");
}
