namespace RForge.Blazor.Events.Sample.Components.Examples.Checkout;

public partial class AddressStep
{
    private string _name = "";
    private string _email = "";
    private string _address = "";
    private string? _error;

    [UseState(Select = nameof(SelectIsActive))]
    private partial bool IsActive { get; }

    private static bool SelectIsActive(CheckoutForm form) => form.Status == CheckoutStatus.Editing && form.Step == 1;

    protected override void OnInitialized()
    {
        // [UseState] values are already readable here: registration runs before OnInitialized.
        var form = State<CheckoutForm>().Value;
        (_name, _email, _address) = (form.Name, form.Email, form.Address);
    }

    private void Continue()
    {
        if (string.IsNullOrWhiteSpace(_name) || string.IsNullOrWhiteSpace(_email) || string.IsNullOrWhiteSpace(_address))
        {
            _error = "Please fill in every field.";
            return;
        }

        _error = null;
        State<CheckoutForm>().Update(f => f with { Name = _name, Email = _email, Address = _address, Step = 2 });
    }
}
