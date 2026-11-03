namespace RForge.Blazor.Bus.Tests;

public sealed record Ping(int Value) : IEvent;

public sealed record Pong(string Text) : IEvent;

public sealed record CounterState(int Count, string Label) : IState<CounterState>
{
    public static CounterState Initial => new(0, "start");
}

/// <summary>Shared, ordered record of what handlers observed.</summary>
public sealed class EventLog
{
    private readonly List<string> _entries = [];

    public IReadOnlyList<string> Entries
    {
        get
        {
            lock (_entries)
            {
                return [.. _entries];
            }
        }
    }

    public void Add(string entry)
    {
        lock (_entries)
        {
            _entries.Add(entry);
        }
    }
}
