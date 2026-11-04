namespace RForgeDocs.Client.EventsExamples.SharedCounter;

/// <summary>Reads the whole state: re-renders on every effective change.</summary>
public partial class ClickDisplay
{
    private int _renders;

    [UseState]
    private partial ClickState Clicks { get; }
}
