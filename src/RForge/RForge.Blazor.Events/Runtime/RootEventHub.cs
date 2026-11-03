using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace RForge.Blazor.Events;

/// <summary>The root of a scope tree; one per DI scope.</summary>
internal sealed class RootEventHub : EventHubScope, IRfEventHub
{
    private readonly StatePersistence? _persistence;

    public RootEventHub(RfEventOptions options, ILogger? logger = null, PersistentComponentState? persistentState = null)
        : base(root: null, parent: null, name: null)
    {
        Options = options;
        Logger = logger ?? NullLogger.Instance;

        if (persistentState is not null && options.PersistedStates.Count > 0)
        {
            _persistence = new StatePersistence(this, persistentState, options.PersistedStates);
        }
    }

    internal RfEventOptions Options { get; }

    internal ILogger Logger { get; }

    /// <summary>Creates the root store for <typeparamref name="TState"/>. Called under the tree lock.</summary>
    internal StateStore<TState> CreateRootStore<TState>() where TState : IState<TState>
    {
        var initial = _persistence is not null && _persistence.TryRestore<TState>(out var restored)
            ? restored
            : TState.Initial;

        return new StateStore<TState>(this, initial);
    }

    public override void Dispose()
    {
        base.Dispose();
        _persistence?.Dispose();
    }
}
