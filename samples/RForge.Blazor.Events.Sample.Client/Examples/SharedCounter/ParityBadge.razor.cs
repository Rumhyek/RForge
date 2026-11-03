namespace RForge.Blazor.Events.Sample.Client.Examples.SharedCounter;

/// <summary>Reads a projection: re-renders only when even/odd flips, so "+2" leaves it alone.</summary>
public partial class ParityBadge
{
    private int _renders;

    [UseState(Select = nameof(SelectIsEven))]
    private partial bool IsEven { get; }

    private static bool SelectIsEven(ClickState state) => state.Count % 2 == 0;
}
