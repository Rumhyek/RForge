using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RForge.Blazor.AutoApi.Generator
{
    internal static class ModelBuilder
    {
        public const string AttributeNamespace = "RForge.Blazor.AutoApi";
        public const string HostAttribute = AttributeNamespace + ".RForgeAutoApiHostAttribute";
        public const string ClientAttribute = AttributeNamespace + ".RForgeAutoApiClientAttribute";

        private static readonly SymbolDisplayFormat TypeFormat = SymbolDisplayFormat.FullyQualifiedFormat
            .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

        private static readonly SymbolDisplayFormat NonNullableTypeFormat = SymbolDisplayFormat.FullyQualifiedFormat;

        // ---------------------------------------------------------------- syntax stage

        public static MarkerInfo GetMarkerInfo(GeneratorAttributeSyntaxContext context, MarkerKind kind, CancellationToken cancellationToken)
        {
            var symbol = (INamedTypeSymbol)context.TargetSymbol;
            var syntax = (ClassDeclarationSyntax)context.TargetNode;
            var attribute = context.Attributes[0];

            var shapeValid = symbol.IsStatic
                && syntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword))
                && symbol.ContainingType is null
                && !symbol.IsGenericType;

            var assemblyNames = new List<string>();
            if (attribute.ConstructorArguments.Length == 1 && attribute.ConstructorArguments[0].Kind == TypedConstantKind.Array)
            {
                foreach (var value in attribute.ConstructorArguments[0].Values)
                {
                    var type = value.Value as ITypeSymbol;
                    while (type is IArrayTypeSymbol array) type = array.ElementType;
                    var name = type?.ContainingAssembly?.Name;
                    if (name is not null && !assemblyNames.Contains(name)) assemblyNames.Add(name);
                }
            }

            var location = attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation()
                ?? syntax.Identifier.GetLocation();

            return new MarkerInfo(
                kind,
                symbol.ContainingNamespace.IsGlobalNamespace ? null : symbol.ContainingNamespace.ToDisplayString(),
                symbol.Name,
                LocationInfo.From(location),
                shapeValid,
                assemblyNames.ToEquatableArray());
        }

        // ---------------------------------------------------------------- semantic stage

        public static MarkerModel Analyze(Compilation compilation, MarkerInfo marker, CancellationToken cancellationToken)
        {
            var diagnostics = new List<DiagnosticInfo>();
            var interfaces = new List<InterfaceModel>();
            var known = new KnownTypes(compilation);
            string? defaultClientName = null;

            if (!marker.ShapeValid)
            {
                diagnostics.Add(DiagnosticInfo.Create(Diagnostics.MarkerShape, marker.Location, marker.ClassName,
                    marker.Kind == MarkerKind.Host ? "RForgeAutoApiHost" : "RForgeAutoApiClient"));
                return new MarkerModel(marker.Kind, marker.Namespace, marker.ClassName, EquatableArray<InterfaceModel>.Empty,
                    marker.Location, diagnostics.ToEquatableArray(), "RForgeAutoApi", false);
            }

            var state = new AnalysisState(compilation, known, marker.Location, diagnostics);

            foreach (var assemblyName in marker.TargetAssemblyNames)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var assembly = FindAssembly(compilation, assemblyName);
                if (assembly is null) continue;

                var defaults = AssemblyDefaults.Read(assembly, known, compilation);
                defaultClientName ??= defaults.HttpClientName;
                state.Defaults = defaults;

                if (defaults.JsonContext is null)
                    diagnostics.Add(DiagnosticInfo.Create(Diagnostics.NoJsonContext, marker.Location, assembly.Name));

                var found = new List<INamedTypeSymbol>();
                CollectInterfaces(assembly.GlobalNamespace, known, compilation, found);
                if (found.Count == 0)
                {
                    diagnostics.Add(DiagnosticInfo.Create(Diagnostics.NoInterfaces, marker.Location, assembly.Name));
                    continue;
                }

                foreach (var type in found)
                {
                    var model = BuildInterface(type, state);
                    if (model is not null) interfaces.Add(model);
                }
            }

            CheckDuplicateRoutes(interfaces, marker.Location, diagnostics);

            var hasWasmEnvironment = compilation.GetTypeByMetadataName(
                "Microsoft.AspNetCore.Components.WebAssembly.Hosting.IWebAssemblyHostEnvironment") is not null;

            return new MarkerModel(marker.Kind, marker.Namespace, marker.ClassName, interfaces.ToEquatableArray(),
                marker.Location, diagnostics.ToEquatableArray(), defaultClientName ?? "RForgeAutoApi", hasWasmEnvironment);
        }

        private static IAssemblySymbol? FindAssembly(Compilation compilation, string name)
        {
            if (compilation.Assembly.Name == name) return compilation.Assembly;
            return compilation.SourceModule.ReferencedAssemblySymbols.FirstOrDefault(a => a.Name == name);
        }

        private static void CollectInterfaces(INamespaceOrTypeSymbol container, KnownTypes known, Compilation compilation, List<INamedTypeSymbol> found)
        {
            foreach (var member in container.GetMembers())
            {
                if (member is INamespaceSymbol ns)
                {
                    CollectInterfaces(ns, known, compilation, found);
                }
                else if (member is INamedTypeSymbol type)
                {
                    if (type.TypeKind == TypeKind.Interface
                        && known.Has(type, known.AutoApiAttribute)
                        && compilation.IsSymbolAccessibleWithin(type, compilation.Assembly))
                    {
                        found.Add(type);
                    }

                    CollectInterfaces(type, known, compilation, found);
                }
            }
        }

        private static InterfaceModel? BuildInterface(INamedTypeSymbol type, AnalysisState state)
        {
            var known = state.Known;
            var defaults = state.Defaults!;
            var errorsBefore = state.ErrorCount;
            var interfaceName = type.ToDisplayString(NonNullableTypeFormat);

            if (type.IsGenericType)
            {
                state.Report(Diagnostics.GenericNotSupported, interfaceName);
                return null;
            }

            var interfaceAttribute = type.GetAttributes().First(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, known.AutoApiAttribute));
            var interfaceClientName = GetNamedString(interfaceAttribute, "HttpClientName");
            var routeSegment = (GetNamedString(interfaceAttribute, "RouteSegment") ?? type.Name).Trim('/');
            var interfaceAuth = ReadAuth(type, known);
            var interfaceAllowAnonymous = known.Has(type, known.AllowAnonymousAttribute);
            var interfaceMetadata = ReadMetadata(type, interfaceName, state);

            var identifier = state.UniqueIdentifier(type.Name.Length > 1 && type.Name[0] == 'I' && char.IsUpper(type.Name[1])
                ? type.Name.Substring(1)
                : type.Name);

            var methods = new List<MethodModel>();
            var nameCounts = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (var current in new[] { type }.Concat(type.AllInterfaces))
            {
                foreach (var member in current.GetMembers())
                {
                    switch (member)
                    {
                        case IMethodSymbol { MethodKind: MethodKind.Ordinary } method:
                            if (method.IsStatic)
                            {
                                if (method.IsAbstract || method.IsVirtual)
                                    state.Report(Diagnostics.NonMethodMember, interfaceName, method.Name);
                                continue;
                            }

                            nameCounts.TryGetValue(method.Name, out var count);
                            nameCounts[method.Name] = count + 1;
                            var suffix = count == 0 ? "" : count.ToString(CultureInfo.InvariantCulture);

                            var model = BuildMethod(method, suffix, type, routeSegment, interfaceClientName,
                                interfaceAuth, interfaceAllowAnonymous, interfaceMetadata, identifier, state);
                            if (model is not null) methods.Add(model);
                            break;

                        case IPropertySymbol:
                        case IEventSymbol:
                            if (!member.IsStatic || member.IsAbstract || member.IsVirtual)
                                state.Report(Diagnostics.NonMethodMember, interfaceName, member.Name);
                            break;
                    }
                }
            }

            if (state.ErrorCount > errorsBefore) return null;

            return new InterfaceModel(interfaceName, type.Name, identifier, defaults.RoutePrefix, methods.ToEquatableArray());
        }

        private static MethodModel? BuildMethod(
            IMethodSymbol method,
            string suffix,
            INamedTypeSymbol apiInterface,
            string routeSegment,
            string? interfaceClientName,
            List<AuthModel> interfaceAuth,
            bool interfaceAllowAnonymous,
            List<string> interfaceMetadata,
            string interfaceIdentifier,
            AnalysisState state)
        {
            var known = state.Known;
            var defaults = state.Defaults!;
            var displayName = $"{apiInterface.Name}.{method.Name}";
            var valid = true;

            if (method.IsGenericMethod)
            {
                state.Report(Diagnostics.GenericNotSupported, displayName);
                valid = false;
            }

            // Return type
            ReturnKind returnKind;
            ITypeSymbol? resultType = null;
            var returnType = method.ReturnType as INamedTypeSymbol;
            var returnDefinition = returnType?.OriginalDefinition;
            if (SymbolEqualityComparer.Default.Equals(returnDefinition, known.Task)) returnKind = ReturnKind.Task;
            else if (SymbolEqualityComparer.Default.Equals(returnDefinition, known.ValueTask)) returnKind = ReturnKind.ValueTask;
            else if (SymbolEqualityComparer.Default.Equals(returnDefinition, known.TaskOfT)) { returnKind = ReturnKind.TaskOfT; resultType = returnType!.TypeArguments[0]; }
            else if (SymbolEqualityComparer.Default.Equals(returnDefinition, known.ValueTaskOfT)) { returnKind = ReturnKind.ValueTaskOfT; resultType = returnType!.TypeArguments[0]; }
            else
            {
                state.Report(Diagnostics.UnsupportedReturnType, displayName, method.ReturnType.ToDisplayString());
                returnKind = ReturnKind.Task;
                valid = false;
            }

            // Per-method settings
            var methodAttribute = method.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, known.AutoApiMethodAttribute));
            var httpMethod = defaults.HttpMethod;
            if (methodAttribute is not null && TryGetNamed(methodAttribute, "HttpMethod", out var verb) && verb.Value is int verbValue)
                httpMethod = verbValue;
            var verbName = HttpMethodName(httpMethod);
            var usesQuery = verbName is "GET" or "DELETE";

            var customRoute = methodAttribute is null ? null : GetNamedString(methodAttribute, "Route");
            string route;
            if (customRoute is not null)
            {
                route = customRoute.Trim().Trim('/');
                if (route.IndexOf('{') >= 0 || route.IndexOf('}') >= 0)
                {
                    state.Report(Diagnostics.RoutePlaceholder, displayName, customRoute);
                    valid = false;
                }
            }
            else
            {
                route = routeSegment + "/" + method.Name + suffix;
            }

            var clientName = (methodAttribute is null ? null : GetNamedString(methodAttribute, "HttpClientName"))
                ?? interfaceClientName
                ?? defaults.HttpClientName;

            // Parameters
            var parameters = new List<ParameterModel>();
            var signature = new List<string>();
            var arguments = new List<string>();
            var serverArguments = new List<string>();
            string? cancellationTokenName = null;
            foreach (var parameter in method.Parameters)
            {
                var name = EscapeIdentifier(parameter.Name);
                signature.Add(parameter.Type.ToDisplayString(TypeFormat) + " " + name);
                arguments.Add(name);
                serverArguments.Add(SymbolEqualityComparer.Default.Equals(parameter.Type, known.CancellationToken)
                    ? "cancellationToken"
                    : "__p" + parameters.Count.ToString(CultureInfo.InvariantCulture));

                if (parameter.RefKind != RefKind.None)
                {
                    state.Report(Diagnostics.RefParameter, displayName, parameter.Name);
                    valid = false;
                    continue;
                }

                if (SymbolEqualityComparer.Default.Equals(parameter.Type, known.CancellationToken))
                {
                    if (cancellationTokenName is not null)
                    {
                        state.Report(Diagnostics.MultipleCancellationTokens, displayName);
                        valid = false;
                    }
                    cancellationTokenName = name;
                    continue;
                }

                string? queryParse = null;
                if (usesQuery)
                {
                    queryParse = QueryParseExpression(parameter.Type);
                    if (queryParse is null)
                    {
                        state.Report(Diagnostics.QueryParameterNotSimple, displayName, verbName, parameter.Name, parameter.Type.ToDisplayString());
                        valid = false;
                        continue;
                    }
                }

                parameters.Add(new ParameterModel(
                    name,
                    ToCamelCase(parameter.Name),
                    parameter.Type.ToDisplayString(TypeFormat),
                    usesQuery ? new JsonTypeModel(parameter.Type.ToDisplayString(NonNullableTypeFormat), null) : state.Json(parameter.Type),
                    MissingValue(parameter),
                    queryParse));
            }

            if (!valid) return null;

            // Authorization and metadata
            var allowAnonymous = interfaceAllowAnonymous || known.Has(method, known.AllowAnonymousAttribute);
            var auth = allowAnonymous ? new List<AuthModel>() : interfaceAuth.Concat(ReadAuth(method, known)).ToList();
            var metadata = interfaceMetadata.Concat(ReadMetadata(method, displayName, state)).ToList();

            return new MethodModel(
                method.Name,
                method.ContainingType.ToDisplayString(NonNullableTypeFormat),
                route,
                verbName,
                usesQuery,
                apiInterface.Name + "_" + method.Name + suffix,
                interfaceIdentifier + "_" + method.Name + suffix,
                clientName,
                returnKind,
                method.ReturnType.ToDisplayString(TypeFormat),
                string.Join(", ", signature),
                string.Join(", ", arguments),
                string.Join(", ", serverArguments),
                resultType?.ToDisplayString(TypeFormat),
                resultType is null ? null : state.Json(resultType),
                parameters.ToEquatableArray(),
                cancellationTokenName,
                allowAnonymous,
                auth.ToEquatableArray(),
                metadata.ToEquatableArray());
        }

        private static void CheckDuplicateRoutes(List<InterfaceModel> interfaces, LocationInfo? location, List<DiagnosticInfo> diagnostics)
        {
            var seen = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var api in interfaces)
            {
                foreach (var method in api.Methods)
                {
                    var key = api.RoutePrefix + "/" + method.Route;
                    var owner = api.Name + "." + method.Name;
                    if (seen.TryGetValue(key, out var existing))
                        diagnostics.Add(DiagnosticInfo.Create(Diagnostics.DuplicateRoute, location, key, existing, owner));
                    else
                        seen[key] = owner;
                }
            }
        }

        // ---------------------------------------------------------------- helpers

        private static string HttpMethodName(int value) => value switch
        {
            0 => "GET",
            2 => "PUT",
            3 => "PATCH",
            4 => "DELETE",
            _ => "POST",
        };

        private static List<AuthModel> ReadAuth(ISymbol symbol, KnownTypes known)
        {
            var list = new List<AuthModel>();
            foreach (var attribute in symbol.GetAttributes())
            {
                if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, known.AuthorizeAttribute)) continue;

                var policy = attribute.ConstructorArguments.Length == 1 ? attribute.ConstructorArguments[0].Value as string : null;
                policy = GetNamedString(attribute, "Policy") ?? policy;
                list.Add(new AuthModel(policy, GetNamedString(attribute, "Roles"), GetNamedString(attribute, "AuthenticationSchemes")));
            }
            return list;
        }

        private static List<string> ReadMetadata(ISymbol symbol, string displayName, AnalysisState state)
        {
            var list = new List<string>();
            foreach (var attribute in symbol.GetAttributes())
            {
                var attributeClass = attribute.AttributeClass;
                if (attributeClass is null || IsExcluded(attributeClass, state.Defaults!)) continue;

                var expression = AttributeExpression(attribute, state.Compilation);
                if (expression is null)
                    state.Report(Diagnostics.AttributeNotCopied, attributeClass.ToDisplayString(), displayName, warning: true);
                else
                    list.Add(expression);
            }
            return list;
        }

        private static bool IsExcluded(INamedTypeSymbol attributeClass, AssemblyDefaults defaults)
        {
            var ns = attributeClass.ContainingNamespace?.ToDisplayString() ?? "";
            if (ns == AttributeNamespace) return true;
            if (ns.StartsWith("System.Runtime.CompilerServices", StringComparison.Ordinal)) return true;
            if (ns.StartsWith("System.Runtime.InteropServices", StringComparison.Ordinal)) return true;
            if (ns.StartsWith("System.Diagnostics", StringComparison.Ordinal)) return true;

            var fullName = attributeClass.ToDisplayString();
            if (fullName is "System.ObsoleteAttribute" or "System.Reflection.DefaultMemberAttribute") return true;

            return defaults.ExcludedAttributes.Any(t => SymbolEqualityComparer.Default.Equals(t, attributeClass));
        }

        private static string? AttributeExpression(AttributeData attribute, Compilation compilation)
        {
            var attributeClass = attribute.AttributeClass!;
            if (attributeClass.TypeKind == TypeKind.Error) return null;
            if (!compilation.IsSymbolAccessibleWithin(attributeClass, compilation.Assembly)) return null;

            var args = new List<string>();
            foreach (var argument in attribute.ConstructorArguments)
            {
                var literal = ConstantExpression(argument);
                if (literal is null) return null;
                args.Add(literal);
            }

            var named = new List<string>();
            foreach (var pair in attribute.NamedArguments)
            {
                var literal = ConstantExpression(pair.Value);
                if (literal is null) return null;
                named.Add($"{EscapeIdentifier(pair.Key)} = {literal}");
            }

            var expression = $"new {attributeClass.ToDisplayString(NonNullableTypeFormat)}({string.Join(", ", args)})";
            if (named.Count > 0) expression += " { " + string.Join(", ", named) + " }";
            return expression;
        }

        private static string? ConstantExpression(TypedConstant constant)
        {
            switch (constant.Kind)
            {
                case TypedConstantKind.Array:
                    if (constant.IsNull) return "null";
                    var items = new List<string>();
                    foreach (var value in constant.Values)
                    {
                        var item = ConstantExpression(value);
                        if (item is null) return null;
                        items.Add(item);
                    }
                    return $"new {constant.Type!.ToDisplayString(NonNullableTypeFormat)} {{ {string.Join(", ", items)} }}";

                case TypedConstantKind.Type:
                    return constant.Value is ITypeSymbol type ? $"typeof({type.ToDisplayString(NonNullableTypeFormat)})" : "null";

                case TypedConstantKind.Enum:
                    return $"(({constant.Type!.ToDisplayString(NonNullableTypeFormat)})({PrimitiveLiteral(constant.Value)}))";

                case TypedConstantKind.Primitive:
                    var literal = PrimitiveLiteral(constant.Value);
                    return constant.Value is null || constant.Value is string or bool or char
                        ? literal
                        : $"(({constant.Type!.ToDisplayString(NonNullableTypeFormat)})({literal}))";

                default:
                    return null;
            }
        }

        private static string PrimitiveLiteral(object? value) => value switch
        {
            null => "null",
            string s => SymbolDisplay.FormatLiteral(s, quote: true),
            char c => SymbolDisplay.FormatLiteral(c, quote: true),
            bool b => b ? "true" : "false",
            float f when float.IsNaN(f) => "float.NaN",
            float f when float.IsPositiveInfinity(f) => "float.PositiveInfinity",
            float f when float.IsNegativeInfinity(f) => "float.NegativeInfinity",
            float f => f.ToString("R", CultureInfo.InvariantCulture) + "F",
            double d when double.IsNaN(d) => "double.NaN",
            double d when double.IsPositiveInfinity(d) => "double.PositiveInfinity",
            double d when double.IsNegativeInfinity(d) => "double.NegativeInfinity",
            double d => d.ToString("R", CultureInfo.InvariantCulture) + "D",
            decimal m => m.ToString(CultureInfo.InvariantCulture) + "M",
            ulong ul => ul.ToString(CultureInfo.InvariantCulture) + "UL",
            long l => l.ToString(CultureInfo.InvariantCulture) + "L",
            uint ui => ui.ToString(CultureInfo.InvariantCulture) + "U",
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? "null",
        };

        /// <summary>Expression used when a parameter is missing from the request, or null when it is required.</summary>
        private static string? MissingValue(IParameterSymbol parameter)
        {
            var typeName = parameter.Type.ToDisplayString(TypeFormat);
            if (parameter.HasExplicitDefaultValue)
            {
                var value = parameter.ExplicitDefaultValue;
                if (value is null) return $"default({typeName})";
                var underlying = parameter.Type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable
                    ? nullable.TypeArguments[0]
                    : parameter.Type;
                if (underlying.TypeKind == TypeKind.Enum || value is not (string or bool or char))
                    return $"(({typeName})({PrimitiveLiteral(value)}))";
                return PrimitiveLiteral(value);
            }

            var isNullable = parameter.Type.IsReferenceType
                ? parameter.NullableAnnotation == NullableAnnotation.Annotated
                : parameter.Type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
            return isNullable ? $"default({typeName})" : null;
        }

        /// <summary>
        /// Returns a format string (with {0} for the raw string) that parses a simple type from the query string,
        /// or null when the type is not a simple type.
        /// </summary>
        private static string? QueryParseExpression(ITypeSymbol type)
        {
            if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
                type = nullable.TypeArguments[0];

            const string invariant = "global::System.Globalization.CultureInfo.InvariantCulture";
            var name = type.ToDisplayString(NonNullableTypeFormat);

            if (type.TypeKind == TypeKind.Enum) return $"global::System.Enum.Parse<{name}>({{0}}, true)";

            switch (type.SpecialType)
            {
                case SpecialType.System_String: return "{0}";
                case SpecialType.System_Boolean: return "bool.Parse({0})";
                case SpecialType.System_Char: return "char.Parse({0})";
                case SpecialType.System_DateTime:
                    return $"global::System.DateTime.Parse({{0}}, {invariant}, global::System.Globalization.DateTimeStyles.RoundtripKind)";
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_Decimal:
                    return $"{name}.Parse({{0}}, {invariant})";
            }

            switch (name)
            {
                case "global::System.Guid":
                case "global::System.DateTimeOffset":
                case "global::System.DateOnly":
                case "global::System.TimeOnly":
                case "global::System.TimeSpan":
                    return $"{name}.Parse({{0}}, {invariant})";
            }

            return null;
        }

        private static string ToCamelCase(string name)
            => name.Length == 0 || !char.IsUpper(name[0]) ? name : char.ToLowerInvariant(name[0]) + name.Substring(1);

        internal static string EscapeIdentifier(string name)
            => SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None ? "@" + name : name;

        private static string? GetNamedString(AttributeData attribute, string name)
            => TryGetNamed(attribute, name, out var value) ? value.Value as string : null;

        private static bool TryGetNamed(AttributeData attribute, string name, out TypedConstant value)
        {
            foreach (var pair in attribute.NamedArguments)
            {
                if (pair.Key == name)
                {
                    value = pair.Value;
                    return true;
                }
            }
            value = default;
            return false;
        }

        // ---------------------------------------------------------------- nested types

        private sealed class KnownTypes
        {
            public KnownTypes(Compilation compilation)
            {
                AutoApiAttribute = compilation.GetTypeByMetadataName(AttributeNamespace + ".RForgeAutoApiAttribute");
                AutoApiMethodAttribute = compilation.GetTypeByMetadataName(AttributeNamespace + ".RForgeAutoApiMethodAttribute");
                AuthorizeAttribute = compilation.GetTypeByMetadataName(AttributeNamespace + ".RfAuthorizeAttribute");
                AllowAnonymousAttribute = compilation.GetTypeByMetadataName(AttributeNamespace + ".RfAllowAnonymousAttribute");
                DefaultsAttribute = compilation.GetTypeByMetadataName(AttributeNamespace + ".RForgeAutoApiDefaultsAttribute");
                Task = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
                TaskOfT = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
                ValueTask = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
                ValueTaskOfT = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");
                CancellationToken = compilation.GetTypeByMetadataName("System.Threading.CancellationToken");
                JsonTypeInfoOfT = compilation.GetTypeByMetadataName("System.Text.Json.Serialization.Metadata.JsonTypeInfo`1");
            }

            public INamedTypeSymbol? AutoApiAttribute { get; }
            public INamedTypeSymbol? AutoApiMethodAttribute { get; }
            public INamedTypeSymbol? AuthorizeAttribute { get; }
            public INamedTypeSymbol? AllowAnonymousAttribute { get; }
            public INamedTypeSymbol? DefaultsAttribute { get; }
            public INamedTypeSymbol? Task { get; }
            public INamedTypeSymbol? TaskOfT { get; }
            public INamedTypeSymbol? ValueTask { get; }
            public INamedTypeSymbol? ValueTaskOfT { get; }
            public INamedTypeSymbol? CancellationToken { get; }
            public INamedTypeSymbol? JsonTypeInfoOfT { get; }

            public bool Has(ISymbol symbol, INamedTypeSymbol? attribute)
                => attribute is not null && symbol.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attribute));
        }

        private sealed class AssemblyDefaults
        {
            public string RoutePrefix { get; private set; } = "/api/auto";
            public int HttpMethod { get; private set; } = 1;
            public string HttpClientName { get; private set; } = "RForgeAutoApi";
            public INamedTypeSymbol? JsonContext { get; private set; }
            public string? JsonContextName { get; private set; }
            public Dictionary<ITypeSymbol, string> JsonTypeInfos { get; } = new(SymbolEqualityComparer.Default);
            public List<ITypeSymbol> ExcludedAttributes { get; } = new();

            public static AssemblyDefaults Read(IAssemblySymbol assembly, KnownTypes known, Compilation compilation)
            {
                var defaults = new AssemblyDefaults();
                var attribute = assembly.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, known.DefaultsAttribute));
                if (attribute is not null)
                {
                    foreach (var pair in attribute.NamedArguments)
                    {
                        switch (pair.Key)
                        {
                            case "RoutePrefix" when pair.Value.Value is string prefix:
                                defaults.RoutePrefix = prefix;
                                break;
                            case "DefaultHttpMethod" when pair.Value.Value is int method:
                                defaults.HttpMethod = method;
                                break;
                            case "HttpClientName" when pair.Value.Value is string name && name.Length > 0:
                                defaults.HttpClientName = name;
                                break;
                            case "JsonContext" when pair.Value.Value is INamedTypeSymbol context:
                                defaults.JsonContext = context;
                                break;
                            case "ExcludedAttributes" when pair.Value.Kind == TypedConstantKind.Array && !pair.Value.IsNull:
                                foreach (var value in pair.Value.Values)
                                    if (value.Value is ITypeSymbol excluded) defaults.ExcludedAttributes.Add(excluded);
                                break;
                        }
                    }
                }

                var trimmed = defaults.RoutePrefix.Trim().Trim('/');
                defaults.RoutePrefix = trimmed.Length == 0 ? "" : "/" + trimmed;

                if (defaults.JsonContext is not null)
                {
                    defaults.JsonContextName = defaults.JsonContext.ToDisplayString(NonNullableTypeFormat);
                    if (compilation.IsSymbolAccessibleWithin(defaults.JsonContext, compilation.Assembly))
                    {
                        foreach (var member in defaults.JsonContext.GetMembers())
                        {
                            if (member is IPropertySymbol { IsStatic: false, DeclaredAccessibility: Accessibility.Public, Type: INamedTypeSymbol propertyType }
                                && SymbolEqualityComparer.Default.Equals(propertyType.OriginalDefinition, known.JsonTypeInfoOfT)
                                && !defaults.JsonTypeInfos.ContainsKey(propertyType.TypeArguments[0]))
                            {
                                defaults.JsonTypeInfos[propertyType.TypeArguments[0]] =
                                    $"{defaults.JsonContextName}.Default.{EscapeIdentifier(member.Name)}";
                            }
                        }
                    }
                }

                return defaults;
            }
        }

        private sealed class AnalysisState(Compilation compilation, KnownTypes known, LocationInfo? location, List<DiagnosticInfo> diagnostics)
        {
            private readonly HashSet<string> _identifiers = new(StringComparer.Ordinal);
            private readonly HashSet<string> _reportedJsonTypes = new(StringComparer.Ordinal);

            public Compilation Compilation { get; } = compilation;
            public KnownTypes Known { get; } = known;
            public AssemblyDefaults? Defaults { get; set; }
            public int ErrorCount { get; private set; }

            public void Report(DiagnosticDescriptor descriptor, params string[] args) => Report(descriptor, args, warning: false);

            public void Report(DiagnosticDescriptor descriptor, string arg0, string arg1, bool warning) => Report(descriptor, new[] { arg0, arg1 }, warning);

            private void Report(DiagnosticDescriptor descriptor, string[] args, bool warning)
            {
                diagnostics.Add(DiagnosticInfo.Create(descriptor, location, args));
                if (!warning && descriptor.DefaultSeverity == DiagnosticSeverity.Error) ErrorCount++;
            }

            public JsonTypeModel Json(ITypeSymbol type)
            {
                var typeName = type.ToDisplayString(NonNullableTypeFormat);
                if (Defaults!.JsonTypeInfos.TryGetValue(type, out var typeInfo))
                    return new JsonTypeModel(typeName, typeInfo);

                if (Defaults.JsonContext is not null && _reportedJsonTypes.Add(typeName))
                    diagnostics.Add(DiagnosticInfo.Create(Diagnostics.TypeNotInJsonContext, location, type.ToDisplayString(), Defaults.JsonContextName!));

                return new JsonTypeModel(typeName, null);
            }

            public string UniqueIdentifier(string baseName)
            {
                var candidate = baseName;
                for (var i = 2; !_identifiers.Add(candidate); i++) candidate = baseName + i.ToString(CultureInfo.InvariantCulture);
                return candidate;
            }
        }
    }
}
