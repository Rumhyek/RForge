using Microsoft.CodeAnalysis;

namespace RForge.Blazor.Events.Generators;

internal static class DiagnosticDescriptors
{
    private const string Category = "RForge.Blazor.Events";

    public static readonly DiagnosticDescriptor DeclaredInRazor = new(
        "RFEVT001",
        "Hub attribute used in a Razor @code block",
        "'{0}' is declared in a .razor file; [Subscribe] and [UseState] members must be declared in a code-behind (.razor.cs) partial class",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true,
        description: "Source generators can't see code produced by the Razor compiler, so attributed members in @code blocks are never wired up.");

    public static readonly DiagnosticDescriptor TypeNotPartial = new(
        "RFEVT002",
        "Type using hub attributes must be partial",
        "Type '{0}' must be declared partial (including any containing types) to use [Subscribe] or [UseState]",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NotEventComponent = new(
        "RFEVT003",
        "Type using hub attributes must derive from RfEventComponent",
        "Type '{0}' uses [Subscribe] or [UseState] but does not derive from RfEventComponent",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidHandlerSignature = new(
        "RFEVT004",
        "Invalid [Subscribe] handler signature",
        "Handler '{0}' must be a non-static, non-generic method returning void, Task or ValueTask, taking (TEvent) or, for Task/ValueTask, (TEvent, CancellationToken), with no ref/out/in parameters",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor HandlerParameterNotEvent = new(
        "RFEVT005",
        "Handler parameter is not an event",
        "The first parameter of handler '{0}' has type '{1}', which does not implement IEvent",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidUseStateProperty = new(
        "RFEVT006",
        "Invalid [UseState] property",
        "Property '{0}' must be a non-static partial property declaration with only a get accessor",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor PropertyTypeNotState = new(
        "RFEVT007",
        "[UseState] property type is not a state type",
        "Property '{0}' has type '{1}', which does not implement IState<{1}>; use a state type or set Select",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidSelector = new(
        "RFEVT008",
        "Invalid [UseState] selector",
        "Selector '{0}' for property '{1}' must be a static method in the same type taking one IState<T> parameter and returning '{2}'",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ManualRegisterOverride = new(
        "RFEVT009",
        "RegisterSubscriptions is generated",
        "Type '{0}' uses [Subscribe] or [UseState] and also overrides RegisterSubscriptions; implement 'partial void OnRegisterSubscriptions(SubscriptionBuilder builder)' instead",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InconsistentScope = new(
        "RFEVT010",
        "Inconsistent scope arguments",
        "{0}",
        Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicateHandler = new(
        "RFEVT011",
        "Multiple handlers for the same event",
        "Type '{0}' has more than one [Subscribe] handler for '{1}'; all of them will run",
        Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MutableEvent = new(
        "RFEVT012",
        "Event type is not immutable",
        "Event type '{0}' is not immutable; the same instance is shared by every handler, so prefer a record with init-only members",
        Category, DiagnosticSeverity.Info, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor StateNotRecord = new(
        "RFEVT013",
        "State type is not a record",
        "State type '{0}' is not a record; change detection falls back to reference equality, so every Set with a new instance notifies",
        Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ImplementsDisposable = new(
        "RFEVT014",
        "Override DisposeAsyncCore instead of implementing disposal",
        "Type '{0}' derives from RfEventComponent and implements {1} directly; Blazor calls only one disposal interface, so override DisposeAsyncCore instead",
        Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);
}
