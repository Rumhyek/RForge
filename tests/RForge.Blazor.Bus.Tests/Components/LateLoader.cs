namespace RForge.Blazor.Bus.Tests.Components;

/// <summary>Loads state asynchronously, like a component fetching data during prerendering.</summary>
public sealed class LateLoader : BusComponentBase
{
    protected override async Task OnInitializedAsync()
    {
        await Task.Delay(20);
        State<CounterState>().Set(new CounterState(9, "loaded"));
    }
}
