#nullable enable

using Microsoft.AspNetCore.Components;

namespace RForgeDocs.EventsExamples.Checkout;

public partial class ProductCard
{
    [Parameter, EditorRequired]
    public Product Product { get; set; } = default!;

    private async Task AddAsync()
    {
        State<CartState>().Update(cart => cart.Add(Product));
        await PublishAsync(new ItemAdded(Product));
    }
}
