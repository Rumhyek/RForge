using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace RForge.Blazor.Bus.Generators;

/// <summary>
/// Generates <c>RegisterSubscriptions</c> overrides and <c>[UseState]</c> property bodies.
/// Invalid members are skipped silently; <see cref="BusAnalyzer"/> reports why.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class BusGenerator : IIncrementalGenerator
{
    private const string Ns = "global::RForge.Blazor.Bus.";

    private static readonly SymbolDisplayFormat TypeFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var handlers = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                BusSymbols.SubscribeAttributeName,
                static (node, _) => node is MethodDeclarationSyntax,
                static (ctx, ct) => CreateHandler(ctx, ct))
            .Where(static model => model is not null)
            .Select(static (model, _) => model!);

        var states = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                BusSymbols.UseStateAttributeName,
                static (node, _) => node is PropertyDeclarationSyntax,
                static (ctx, ct) => CreateState(ctx, ct))
            .Where(static model => model is not null)
            .Select(static (model, _) => model!);

        context.RegisterSourceOutput(handlers.Collect().Combine(states.Collect()), static (spc, pair) => Emit(spc, pair.Left, pair.Right));
    }

    // ── Models ───────────────────────────────────────────────────────────

    private static HandlerModel? CreateHandler(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.TargetSymbol is not IMethodSymbol method
            || BusSymbols.Create(context.SemanticModel.Compilation) is not { } symbols
            || !BusSymbols.IsPartialIncludingContainers(method.ContainingType)
            || symbols.CheckHandler(method, out var returns, out var hasToken) != HandlerProblem.None)
        {
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var eventType = method.Parameters[0].Type.ToDisplayString(TypeFormat);
        var returnType = returns switch
        {
            HandlerReturn.Task => "global::System.Threading.Tasks.Task",
            HandlerReturn.ValueTask => "global::System.Threading.Tasks.ValueTask",
            _ => null,
        };
        var delegateType = returnType is null
            ? $"global::System.Action<{eventType}>"
            : hasToken
                ? $"global::System.Func<{eventType}, global::System.Threading.CancellationToken, {returnType}>"
                : $"global::System.Func<{eventType}, {returnType}>";

        var arguments = BusSymbols.ReadArguments(context.Attributes[0]);
        var location = context.TargetNode.GetLocation();

        return new HandlerModel(
            TypeModel.Create(method.ContainingType),
            method.Name,
            eventType,
            delegateType,
            arguments.ToScopeTargetExpression(),
            arguments.Render,
            location.SourceTree?.FilePath ?? string.Empty,
            location.SourceSpan.Start);
    }

    private static StateModel? CreateState(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.TargetSymbol is not IPropertySymbol property
            || context.TargetNode is not PropertyDeclarationSyntax syntax
            || BusSymbols.Create(context.SemanticModel.Compilation) is not { } symbols
            || !BusSymbols.IsPartialIncludingContainers(property.ContainingType))
        {
            return null;
        }

        var arguments = BusSymbols.ReadArguments(context.Attributes[0]);
        var problem = symbols.CheckUseState(property, arguments.Select, out var stateType, out var selector);
        if (problem == UseStateProblem.BadProperty)
        {
            return null; // Not a partial get-only declaration: nothing we could implement.
        }

        cancellationToken.ThrowIfCancellationRequested();

        // For a bad type or selector, still implement the property (as a throwing stub). Otherwise the only build
        // error would be CS9248 "must have an implementation part", which hides the analyzer's explanation:
        // the compiler doesn't run analyzers when compilation errors exist.
        var stubDiagnostic = problem switch
        {
            UseStateProblem.NotAState => DiagnosticDescriptors.PropertyTypeNotState.Id,
            UseStateProblem.BadSelector => DiagnosticDescriptors.InvalidSelector.Id,
            _ => null,
        };

        var location = syntax.GetLocation();
        return new StateModel(
            TypeModel.Create(property.ContainingType),
            property.Name,
            string.Join(" ", syntax.Modifiers.Select(static m => m.Text)),
            property.Type.ToDisplayString(TypeFormat),
            stateType?.ToDisplayString(TypeFormat) ?? string.Empty,
            selector?.Name,
            arguments.ToScopeTargetExpression(),
            arguments.Render,
            location.SourceTree?.FilePath ?? string.Empty,
            location.SourceSpan.Start,
            stubDiagnostic);
    }

    // ── Emission ─────────────────────────────────────────────────────────

    private static void Emit(SourceProductionContext context, ImmutableArray<HandlerModel> handlers, ImmutableArray<StateModel> states)
    {
        var types = handlers.Select(static h => h.Type).Concat(states.Select(static s => s.Type)).Distinct();

        foreach (var type in types)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var typeHandlers = handlers.Where(h => h.Type.Equals(type)).OrderBy(static h => h.FilePath, System.StringComparer.Ordinal).ThenBy(static h => h.Position).ToList();
            var typeStates = states.Where(s => s.Type.Equals(type)).OrderBy(static s => s.FilePath, System.StringComparer.Ordinal).ThenBy(static s => s.Position).ToList();

            context.AddSource(type.HintName, SourceText.From(Render(type, typeHandlers, typeStates), Encoding.UTF8));
        }
    }

    private static string Render(TypeModel type, List<HandlerModel> handlers, List<StateModel> states)
    {
        var writer = new CodeWriter();
        writer.Line("// <auto-generated/>");
        writer.Line("#nullable enable");
        writer.Line("#pragma warning disable CS1591");
        writer.Line();

        if (type.Namespace is not null)
        {
            writer.Open("namespace " + type.Namespace);
        }

        foreach (var container in type.Declarations)
        {
            writer.Open(container);
        }

        foreach (var state in states.Where(static s => s.StubDiagnostic is not null))
        {
            writer.Line($"{state.Modifiers} {state.PropertyType} {state.PropertyName} => throw new global::System.InvalidOperationException(" +
                        $"\"[UseState] on '{state.PropertyName}' is invalid ({state.StubDiagnostic}); see the build diagnostics.\");");
            writer.Line();
        }

        states = states.Where(static s => s.StubDiagnostic is null).ToList();

        foreach (var state in states)
        {
            var handleType = state.Selector is null
                ? $"{Ns}StateHandle<{state.StateType}>"
                : $"{Ns}SelectorHandle<{state.StateType}, {state.PropertyType}>";

            writer.Line($"private {handleType} __bus_{state.PropertyName} = default!;");
            writer.Line($"{state.Modifiers} {state.PropertyType} {state.PropertyName} => __bus_{state.PropertyName}.Value;");
            writer.Line();
        }

        writer.Line($"partial void OnRegisterSubscriptions({Ns}SubscriptionBuilder builder);");
        writer.Line();
        writer.Line("/// <inheritdoc />");
        writer.Open($"protected override void RegisterSubscriptions({Ns}SubscriptionBuilder builder)");
        writer.Line("base.RegisterSubscriptions(builder);");

        foreach (var state in states)
        {
            var render = state.Render ? "true" : "false";
            writer.Line(state.Selector is null
                ? $"__bus_{state.PropertyName} = builder.State<{state.StateType}>({state.ScopeTarget}, render: {render});"
                : $"__bus_{state.PropertyName} = builder.State<{state.StateType}, {state.PropertyType}>(" +
                  $"(global::System.Func<{state.StateType}, {state.PropertyType}>){state.Selector}, {state.ScopeTarget}, render: {render});");
        }

        foreach (var handler in handlers)
        {
            var render = handler.Render ? "true" : "false";
            writer.Line($"builder.On<{handler.EventType}>(({handler.DelegateType}){handler.MethodName}, {handler.ScopeTarget}, render: {render});");
        }

        writer.Line("OnRegisterSubscriptions(builder);");
        writer.CloseAll();
        return writer.ToString();
    }

    private sealed class CodeWriter
    {
        private readonly StringBuilder _builder = new();
        private int _depth;

        public void Line(string text = "")
        {
            if (text.Length > 0)
            {
                _builder.Append(' ', _depth * 4);
            }

            _builder.AppendLine(text);
        }

        public void Open(string header)
        {
            Line(header);
            Line("{");
            _depth++;
        }

        public void CloseAll()
        {
            while (_depth > 0)
            {
                _depth--;
                Line("}");
            }
        }

        public override string ToString() => _builder.ToString();
    }
}

/// <summary>Everything needed to re-declare a (possibly nested, generic) partial type.</summary>
internal sealed record TypeModel(string? Namespace, EquatableArray<string> Declarations, string HintName)
{
    public static TypeModel Create(INamedTypeSymbol type)
    {
        var declarations = new List<string>();
        for (var current = type; current is not null; current = current.ContainingType)
        {
            declarations.Insert(0, Declare(current));
        }

        var ns = type.ContainingNamespace is { IsGlobalNamespace: false } containing ? containing.ToDisplayString() : null;
        var hint = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", string.Empty)
            .Replace('<', '_').Replace('>', '_').Replace(',', '_').Replace(' ', '_');

        return new TypeModel(ns, new EquatableArray<string>(declarations.ToArray()), hint + BusSymbols.GeneratedFileSuffix);
    }

    private static string Declare(INamedTypeSymbol type)
    {
        var keyword = type switch
        {
            { IsRecord: true, IsValueType: true } => "record struct",
            { IsRecord: true } => "record",
            { TypeKind: TypeKind.Struct } => "struct",
            { TypeKind: TypeKind.Interface } => "interface",
            _ => "class",
        };

        var typeParameters = type.TypeParameters.Length == 0
            ? string.Empty
            : "<" + string.Join(", ", type.TypeParameters.Select(static p => p.Name)) + ">";

        return $"partial {keyword} {type.Name}{typeParameters}";
    }
}

internal sealed record HandlerModel(
    TypeModel Type,
    string MethodName,
    string EventType,
    string DelegateType,
    string ScopeTarget,
    bool Render,
    string FilePath,
    int Position);

internal sealed record StateModel(
    TypeModel Type,
    string PropertyName,
    string Modifiers,
    string PropertyType,
    string StateType,
    string? Selector,
    string ScopeTarget,
    bool Render,
    string FilePath,
    int Position,
    string? StubDiagnostic = null);
