namespace RForge.Blazor.Events;

/// <summary>
/// Subscribes a method of a <see cref="RfEventComponent"/> to the event type of its first parameter.
/// The RForge.Blazor.Events source generator turns this into a <c>RegisterSubscriptions</c> call.
/// </summary>
/// <remarks>
/// Supported signatures: <c>void M(TEvent)</c>, <c>Task M(TEvent)</c>, <c>ValueTask M(TEvent)</c>, and the
/// <c>Task</c>/<c>ValueTask</c> forms with a trailing <see cref="CancellationToken"/>.
/// The containing type must be <c>partial</c> and declared in a C# file (code-behind), not a <c>@code</c> block.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class SubscribeAttribute : Attribute
{
    /// <summary>Which scope to attach to. Defaults to the component's nearest scope.</summary>
    public ScopeKind Scope { get; set; }

    /// <summary>Name of the scope to attach to. Setting it implies <see cref="ScopeKind.Named"/>.</summary>
    public string? ScopeName { get; set; }

    /// <summary>Whether to call <c>StateHasChanged</c> after the handler runs. Defaults to <see langword="true"/>.</summary>
    public bool Render { get; set; } = true;
}

/// <summary>
/// Implements a <c>partial</c> get-only property as a live, reactive view of a state value.
/// The RForge.Blazor.Events source generator supplies the property body and the subscription.
/// </summary>
/// <remarks>
/// Without <see cref="Select"/>, the property type must implement <see cref="IState{TSelf}"/>. With it, the property
/// type is the selector's return type and the component re-renders only when the selected value changes.
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class UseStateAttribute : Attribute
{
    /// <summary>Name of a static selector method in the same type: <c>static TSelected M(TState state)</c>.</summary>
    public string? Select { get; set; }

    /// <summary>Where provider resolution starts. Defaults to the component's nearest scope.</summary>
    public ScopeKind Scope { get; set; }

    /// <summary>Name of the scope where provider resolution starts. Setting it implies <see cref="ScopeKind.Named"/>.</summary>
    public string? ScopeName { get; set; }

    /// <summary>Whether an effective change re-renders the component. Defaults to <see langword="true"/>.</summary>
    public bool Render { get; set; } = true;
}
