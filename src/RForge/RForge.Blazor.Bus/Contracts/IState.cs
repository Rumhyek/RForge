namespace RForge.Blazor.Bus;

/// <summary>
/// A piece of shared state with a well-defined starting value.
/// </summary>
/// <typeparam name="TSelf">The implementing type. Should be an immutable <c>record</c>.</typeparam>
/// <remarks>
/// Stores compare values with <see cref="EqualityComparer{T}.Default"/> and skip notifications when a
/// new value equals the current one, so records give no-op suppression for free.
/// </remarks>
public interface IState<TSelf> where TSelf : IState<TSelf>
{
    /// <summary>The value a store starts with when nothing else supplies one.</summary>
    static abstract TSelf Initial { get; }
}
