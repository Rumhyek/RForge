namespace RForge.Blazor.Bus.Sample.Components.Examples.Checkout;

public partial class PaymentStep
{
    private string _card = "4242 4242 4242 4242";

    [UseState(Select = nameof(SelectIsActive))]
    private partial bool IsActive { get; }

    private static bool SelectIsActive(CheckoutForm form) => form.Status == CheckoutStatus.Editing && form.Step == 2;

    private void Back() => State<CheckoutForm>().Update(f => f with { Step = 1 });

    private void Continue() => State<CheckoutForm>().Update(f => f with { Card = _card, Step = 3 });
}
