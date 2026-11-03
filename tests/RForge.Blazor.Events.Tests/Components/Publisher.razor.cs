using Microsoft.AspNetCore.Components;

namespace RForge.Blazor.Events.Tests.Components;

/// <summary>Publishes from its own position in the tree.</summary>
public partial class Publisher : RfEventComponent
{
    [Parameter]
    public string Id { get; set; } = "publish";

    [Parameter]
    public Route Route { get; set; }

    [Parameter]
    public int Value { get; set; } = 1;

    private Task OnClickAsync() => PublishAsync(new Ping(Value), Route).AsTask();

    public ValueTask Publish(int value, Route route = default) => PublishAsync(new Ping(value), route);

    public void Increment() => State<CounterState>().Update(s => s with { Count = s.Count + 1 });

    public IRfEventHubScope CurrentScope => Scope;
}
