namespace RForge.Blazor.AutoApi.Generator
{
    internal enum MarkerKind { Host, Client }

    internal enum ReturnKind { Task, TaskOfT, ValueTask, ValueTaskOfT }

    /// <summary>What the syntax pipeline learns about a marker class before semantic analysis.</summary>
    internal sealed record MarkerInfo(
        MarkerKind Kind,
        string? Namespace,
        string ClassName,
        LocationInfo? Location,
        bool ShapeValid,
        EquatableArray<string> TargetAssemblyNames);

    internal sealed record MarkerModel(
        MarkerKind Kind,
        string? Namespace,
        string ClassName,
        EquatableArray<InterfaceModel> Interfaces,
        LocationInfo? Location,
        EquatableArray<DiagnosticInfo> Diagnostics,
        string DefaultClientName,
        bool HasWebAssemblyHostEnvironment);

    internal sealed record InterfaceModel(
        string FullName,
        string Name,
        string Identifier,
        string RoutePrefix,
        EquatableArray<MethodModel> Methods);

    internal sealed record MethodModel(
        string Name,
        string DeclaringInterface,
        string Route,
        string HttpMethod,
        bool UsesQuery,
        string EndpointName,
        string Identifier,
        string ClientName,
        ReturnKind ReturnKind,
        string ReturnType,
        string ParameterList,
        string ArgumentList,
        string ServerArgumentList,
        string? ResultType,
        JsonTypeModel? ResultJson,
        EquatableArray<ParameterModel> Parameters,
        string? CancellationTokenParameter,
        bool AllowAnonymous,
        EquatableArray<AuthModel> Auth,
        EquatableArray<string> Metadata)
    {
        public bool HasResult => ReturnKind is ReturnKind.TaskOfT or ReturnKind.ValueTaskOfT;
        public bool RequiresAuthorization => !AllowAnonymous && Auth.Length > 0;
    }

    internal sealed record ParameterModel(
        string Name,
        string JsonName,
        string Type,
        JsonTypeModel Json,
        string? MissingValue,
        string? QueryParse);

    /// <summary>
    /// <paramref name="TypeInfo"/> is the JsonSerializerContext property expression, or null when the type
    /// must use the reflection fallback.
    /// </summary>
    internal sealed record JsonTypeModel(string Type, string? TypeInfo);

    internal sealed record AuthModel(string? Policy, string? Roles, string? AuthenticationSchemes);
}
