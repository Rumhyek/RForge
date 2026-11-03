namespace RForge.Blazor.Bus;

/// <summary>
/// Marker for fire-and-forget messages published on the bus.
/// </summary>
/// <remarks>
/// Events have no current value: a subscriber only receives events published while it is subscribed.
/// The same instance is delivered to every handler, so prefer immutable <c>record</c> types.
/// </remarks>
public interface IEvent;
