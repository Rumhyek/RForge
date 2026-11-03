namespace RForge.Blazor.Events;

/// <summary>Tracks nested publishes along the async call chain to catch event loops.</summary>
internal static class PublishDepth
{
    private static readonly AsyncLocal<int> s_depth = new();

    internal static int Current => s_depth.Value;

    public static Scope Enter(int maxDepth, Type eventType)
    {
        var previous = s_depth.Value;
        if (previous >= maxDepth)
        {
            throw new RfEventReentrancyException(maxDepth, eventType);
        }

        s_depth.Value = previous + 1;
        return new Scope(previous);
    }

    internal readonly struct Scope(int previous) : IDisposable
    {
        public void Dispose() => s_depth.Value = previous;
    }
}
