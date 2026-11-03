namespace RForge.Blazor.Bus.Sample.Components.Examples.Checkout;

public partial class CheckoutSteps
{
    private static readonly (int Number, string Name)[] Steps = [(1, "Address"), (2, "Payment"), (3, "Review")];

    [UseState(Select = nameof(SelectStep))]
    private partial int Step { get; }

    private static int SelectStep(CheckoutForm form) => form.Status == CheckoutStatus.Editing ? form.Step : 4;
}
