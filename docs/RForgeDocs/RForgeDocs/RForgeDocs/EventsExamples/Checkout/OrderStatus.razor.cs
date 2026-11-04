#nullable enable

namespace RForgeDocs.EventsExamples.Checkout;

public partial class OrderStatus
{
    [UseState(Select = nameof(SelectStatus))]
    private partial (CheckoutStatus Status, Guid? OrderId) Status { get; }

    private static (CheckoutStatus, Guid?) SelectStatus(CheckoutForm form) => (form.Status, form.OrderId);

    private void StartOver() => State<CheckoutForm>().Update(f => CheckoutForm.Initial with
    {
        Name = f.Name,
        Email = f.Email,
        Address = f.Address,
    });
}
