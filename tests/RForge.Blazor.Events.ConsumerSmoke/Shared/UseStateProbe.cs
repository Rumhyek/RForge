using RForge.Blazor.Events;

namespace Smoke;

public sealed record CountState(int Count) : IState<CountState>
{
    public static CountState Initial => new(0);
}

// [UseState] is declared as a C# 13 partial property.
public partial class UseStateProbe : RfEventComponent
{
    [UseState] private partial CountState Counter { get; }
}
