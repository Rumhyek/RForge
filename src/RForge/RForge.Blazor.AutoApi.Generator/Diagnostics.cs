using Microsoft.CodeAnalysis;

namespace RForge.Blazor.AutoApi.Generator
{
    internal static class Diagnostics
    {
        private const string Category = "RForge.AutoApi";

        public static readonly DiagnosticDescriptor MarkerShape = new(
            "RFAA001", "Marker class must be static partial",
            "'{0}' must be a non-nested, non-generic 'static partial' class to use [{1}]",
            Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor NoInterfaces = new(
            "RFAA002", "No Auto API interfaces found",
            "Assembly '{0}' contains no accessible [RForgeAutoApi] interfaces",
            Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor UnsupportedReturnType = new(
            "RFAA003", "Unsupported return type",
            "'{0}' returns '{1}'; only Task, Task<T>, ValueTask and ValueTask<T> are supported",
            Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor GenericNotSupported = new(
            "RFAA004", "Generics are not supported",
            "'{0}' is generic; generic interfaces and generic methods are not supported",
            Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor RefParameter = new(
            "RFAA005", "ref/out/in parameters are not supported",
            "Parameter '{1}' of '{0}' is ref/out/in; these are not supported",
            Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor QueryParameterNotSimple = new(
            "RFAA006", "GET/DELETE parameters must be simple types",
            "'{0}' uses {1} but parameter '{2}' of type '{3}' is not a simple type",
            Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor RoutePlaceholder = new(
            "RFAA007", "Route placeholders are not supported",
            "Custom route '{1}' on '{0}' contains a placeholder; routes must be static",
            Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor DuplicateRoute = new(
            "RFAA008", "Duplicate route",
            "Route '{0}' is used by more than one method ('{1}' and '{2}')",
            Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor TypeNotInJsonContext = new(
            "RFAA009", "Type missing from JsonSerializerContext",
            "'{0}' is not registered in '{1}'; reflection-based serialization will be used for it",
            Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor AttributeNotCopied = new(
            "RFAA010", "Attribute not copied to endpoint",
            "Attribute '{0}' on '{1}' could not be reconstructed and was not copied to the endpoint",
            Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MultipleCancellationTokens = new(
            "RFAA011", "Multiple CancellationToken parameters",
            "'{0}' has more than one CancellationToken parameter",
            Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor NoJsonContext = new(
            "RFAA012", "No JsonSerializerContext configured",
            "No JsonContext is configured for assembly '{0}'; using reflection-based serialization",
            Category, DiagnosticSeverity.Info, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor NonMethodMember = new(
            "RFAA013", "Only methods are supported",
            "'{0}' declares '{1}'; [RForgeAutoApi] interfaces may only contain instance methods",
            Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MultipleMarkers = new(
            "RFAA014", "Only one marker class per project",
            "Only one [{0}] class is allowed per project; '{1}' is ignored",
            Category, DiagnosticSeverity.Error, isEnabledByDefault: true);
    }
}
