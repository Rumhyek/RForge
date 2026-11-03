namespace RForge.Blazor.Events.Sample.Client.Examples.SharedCounter;

/// <summary>Writes the shared state. It doesn't read it reactively, so it never re-renders because of it.</summary>
public partial class ClickButtons
{
    private int _renders;

    private void Add(int amount) => State<ClickState>().Update(s => s with { Count = s.Count + amount });

    private void Reset() => State<ClickState>().Set(ClickState.Initial);

    // Record equality makes this a no-op: no subscriber re-renders.
    private void SetSame() => State<ClickState>().Set(State<ClickState>().Value with { });
}
