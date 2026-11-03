using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

namespace RForge.Blazor.Bus;

/// <summary>
/// Carries opted-in root state from a prerender into the interactive session via
/// <see cref="PersistentComponentState"/>. Values are stored as JSON strings so the framework
/// only ever (de)serializes <see cref="string"/>, which is trim-safe.
/// </summary>
internal sealed class StatePersistence : IDisposable
{
    private readonly RootBus _root;
    private readonly PersistentComponentState _state;
    private readonly IReadOnlyList<PersistedState> _registrations;
    private readonly PersistingComponentStateSubscription _subscription;

    public StatePersistence(RootBus root, PersistentComponentState state, IReadOnlyList<PersistedState> registrations)
    {
        _root = root;
        _state = state;
        _registrations = registrations;
        // A callback registered outside a component must name a render mode (.NET 10). Auto targets both the
        // Server and the WebAssembly state stores, so root state follows whichever runtime takes over.
        _subscription = state.RegisterOnPersisting(PersistAsync, RenderMode.InteractiveAuto);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Only System.String is serialized by the framework.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Only System.String is serialized by the framework.")]
    private Task PersistAsync()
    {
        foreach (var registration in _registrations)
        {
            try
            {
                if (registration.Capture(_root) is { } json)
                {
                    _state.PersistAsJson(registration.Key, json);
                }
            }
            catch (Exception ex)
            {
                _root.Logger.LogError(ex, "Failed to persist state {StateType}.", registration.StateType.FullName);
            }
        }

        return Task.CompletedTask;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Only System.String is deserialized by the framework.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Only System.String is deserialized by the framework.")]
    public bool TryRestore<TState>([MaybeNullWhen(false)] out TState value) where TState : IState<TState>
    {
        foreach (var registration in _registrations)
        {
            if (registration is PersistedState<TState> typed
                && _state.TryTakeFromJson<string>(typed.Key, out var json)
                && json is not null)
            {
                try
                {
                    var restored = typed.Deserialize(json);
                    if (restored is not null)
                    {
                        value = restored;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _root.Logger.LogError(ex, "Failed to restore persisted state {StateType}; using its initial value.", typeof(TState).FullName);
                }
            }
        }

        value = default;
        return false;
    }

    public void Dispose() => _subscription.Dispose();
}

/// <summary>A state type opted into prerender persistence.</summary>
internal abstract class PersistedState(Type stateType)
{
    public Type StateType { get; } = stateType;

    public string Key { get; } = "RForge.Blazor.Bus:" + stateType.FullName;

    /// <summary>Serializes the current root value, or returns null if no root store exists.</summary>
    public abstract string? Capture(RootBus root);
}

internal sealed class PersistedState<TState>(Func<TState, string> serialize, Func<string, TState?> deserialize)
    : PersistedState(typeof(TState)) where TState : IState<TState>
{
    public static PersistedState<TState> Create(JsonTypeInfo<TState> typeInfo) =>
        new(value => JsonSerializer.Serialize(value, typeInfo), json => JsonSerializer.Deserialize(json, typeInfo));

    [RequiresUnreferencedCode("Uses reflection-based JSON serialization. Use the JsonTypeInfo overload for trimmed apps.")]
    [RequiresDynamicCode("Uses reflection-based JSON serialization. Use the JsonTypeInfo overload for AOT apps.")]
    public static PersistedState<TState> Create() =>
        new(value => JsonSerializer.Serialize(value), json => JsonSerializer.Deserialize<TState>(json));

    public TState? Deserialize(string json) => deserialize(json);

    public override string? Capture(RootBus root) =>
        root.TryGetProvidedStore<TState>() is { } store ? serialize(store.Value) : null;
}
