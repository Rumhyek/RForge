using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace RForge.Blazor.Events;

/// <summary>How failures in non-component handlers are reported.</summary>
public enum HandlerErrorMode
{
    /// <summary>Log the exception and continue with the remaining handlers (default).</summary>
    Log,

    /// <summary>Run all handlers, then throw an <see cref="AggregateException"/> to the publisher.</summary>
    Throw,
}

/// <summary>Options for <c>AddRForgeEvents</c>.</summary>
public sealed class RfEventOptions
{
    private int _maxPublishDepth = 16;

    /// <summary>
    /// Maximum nesting of publishes made from inside handlers before <see cref="RfEventReentrancyException"/>
    /// is thrown. Defaults to 16.
    /// </summary>
    public int MaxPublishDepth
    {
        get => _maxPublishDepth;
        set => _maxPublishDepth = value >= 1 ? value : throw new ArgumentOutOfRangeException(nameof(value), "Must be at least 1.");
    }

    /// <summary>How failures in non-component handlers are reported. Component handlers always go to their <c>ErrorBoundary</c>.</summary>
    public HandlerErrorMode HandlerErrorMode { get; set; } = HandlerErrorMode.Log;

    internal List<PersistedState> PersistedStates { get; } = [];

    internal List<Action<IServiceCollection>> ServiceRegistrations { get; } = [];

    /// <summary>
    /// Persists root-provided <typeparamref name="TState"/> from prerendering into the interactive session,
    /// using reflection-based JSON serialization.
    /// </summary>
    [RequiresUnreferencedCode("Uses reflection-based JSON serialization. Use the JsonTypeInfo overload for trimmed apps.")]
    [RequiresDynamicCode("Uses reflection-based JSON serialization. Use the JsonTypeInfo overload for AOT apps.")]
    public RfEventOptions PersistState<TState>() where TState : IState<TState> =>
        AddPersisted(PersistedState<TState>.Create());

    /// <summary>
    /// Persists root-provided <typeparamref name="TState"/> from prerendering into the interactive session,
    /// using source-generated JSON metadata (trim and AOT safe).
    /// </summary>
    public RfEventOptions PersistState<TState>(JsonTypeInfo<TState> typeInfo) where TState : IState<TState>
    {
        ArgumentNullException.ThrowIfNull(typeInfo);
        return AddPersisted(PersistedState<TState>.Create(typeInfo));
    }

    /// <summary>
    /// Also exposes root-provided <typeparamref name="TState"/> as a cascading value, so components that don't
    /// derive from <see cref="RfEventComponent"/> can read it with <c>[CascadingParameter]</c>.
    /// Every such consumer re-renders on every effective change.
    /// </summary>
    public RfEventOptions CascadeRootState<TState>() where TState : IState<TState>
    {
        ServiceRegistrations.Add(static services => services.AddCascadingValue(CreateCascadingSource<TState>));
        return this;
    }

    private RfEventOptions AddPersisted(PersistedState registration)
    {
        if (PersistedStates.Exists(r => r.StateType == registration.StateType))
        {
            throw new InvalidOperationException($"State {registration.StateType.Name} is already registered for persistence.");
        }

        PersistedStates.Add(registration);
        return this;
    }

    private static CascadingValueSource<TState> CreateCascadingSource<TState>(IServiceProvider services)
        where TState : IState<TState>
    {
        var store = services.GetRequiredService<IRfEventHub>().State<TState>();
        var source = new CascadingValueSource<TState>(store.Value, isFixed: false);
        store.Subscribe(value => source.NotifyChangedAsync(value).Observe());
        return source;
    }
}
