namespace RForge.Blazor.Events;

/// <summary>Thrown when a named route or subscription can't find a matching ancestor-or-self scope.</summary>
public sealed class RfEventScopeNotFoundException(string scopeName)
    : InvalidOperationException($"No enclosing hub scope named '{scopeName}' was found. Named scopes are matched against the current scope and its ancestors only.")
{
    /// <summary>The name that was looked up.</summary>
    public string ScopeName { get; } = scopeName;
}

/// <summary>Thrown when nested publishes exceed <see cref="RfEventOptions.MaxPublishDepth"/>, which usually means an event loop.</summary>
public sealed class RfEventReentrancyException(int maxDepth, Type eventType)
    : InvalidOperationException($"Publishing {eventType.Name} exceeded the maximum nested publish depth of {maxDepth}. This usually means handlers are publishing events that trigger each other in a loop.")
{
    /// <summary>The configured maximum depth.</summary>
    public int MaxDepth { get; } = maxDepth;

    /// <summary>The event type whose publish exceeded the limit.</summary>
    public Type EventType { get; } = eventType;
}
