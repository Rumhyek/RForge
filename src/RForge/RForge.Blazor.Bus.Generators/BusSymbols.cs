using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RForge.Blazor.Bus.Generators;

internal enum HandlerReturn
{
    Void,
    Task,
    ValueTask,
}

internal enum HandlerProblem
{
    None,
    BadSignature,
    NotAnEvent,
}

internal enum UseStateProblem
{
    None,
    BadProperty,
    NotAState,
    BadSelector,
}

/// <summary>Resolved RForge.Blazor.Bus symbols plus the validation rules shared by the generator and the analyzer.</summary>
internal sealed class BusSymbols
{
    public const string Namespace = "RForge.Blazor.Bus";
    public const string SubscribeAttributeName = Namespace + ".SubscribeAttribute";
    public const string UseStateAttributeName = Namespace + ".UseStateAttribute";
    public const string GeneratedFileSuffix = ".Bus.g.cs";

    private readonly Compilation _compilation;

    private BusSymbols(Compilation compilation, INamedTypeSymbol @event, INamedTypeSymbol state)
    {
        _compilation = compilation;
        Event = @event;
        State = state;
        Component = compilation.GetTypeByMetadataName(Namespace + ".BusComponentBase");
        Subscribe = compilation.GetTypeByMetadataName(SubscribeAttributeName);
        UseState = compilation.GetTypeByMetadataName(UseStateAttributeName);
        CancellationToken = compilation.GetTypeByMetadataName("System.Threading.CancellationToken");
        Task = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
        ValueTask = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
        Disposable = compilation.GetSpecialType(SpecialType.System_IDisposable);
        AsyncDisposable = compilation.GetTypeByMetadataName("System.IAsyncDisposable");
    }

    public INamedTypeSymbol Event { get; }
    public INamedTypeSymbol State { get; }
    public INamedTypeSymbol? Component { get; }
    public INamedTypeSymbol? Subscribe { get; }
    public INamedTypeSymbol? UseState { get; }
    public INamedTypeSymbol? CancellationToken { get; }
    public INamedTypeSymbol? Task { get; }
    public INamedTypeSymbol? ValueTask { get; }
    public INamedTypeSymbol Disposable { get; }
    public INamedTypeSymbol? AsyncDisposable { get; }

    public static BusSymbols? Create(Compilation compilation)
    {
        var @event = compilation.GetTypeByMetadataName(Namespace + ".IEvent");
        var state = compilation.GetTypeByMetadataName(Namespace + ".IState`1");
        return @event is null || state is null ? null : new BusSymbols(compilation, @event, state);
    }

    // ── Type checks ──────────────────────────────────────────────────────

    public bool IsEvent(ITypeSymbol type) =>
        type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, Event));

    /// <summary>Whether <paramref name="type"/> implements <c>IState&lt;type&gt;</c>.</summary>
    public bool IsState(ITypeSymbol type) =>
        type.AllInterfaces.Any(i =>
            SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, State)
            && SymbolEqualityComparer.Default.Equals(i.TypeArguments[0], type));

    public bool DerivesFromComponent(INamedTypeSymbol type)
    {
        if (Component is null)
        {
            return false;
        }

        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, Component))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsPartialIncludingContainers(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.ContainingType)
        {
            foreach (var reference in current.DeclaringSyntaxReferences)
            {
                if (reference.GetSyntax() is TypeDeclarationSyntax declaration
                    && !declaration.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    return false;
                }
            }
        }

        return true;
    }

    // ── [Subscribe] ──────────────────────────────────────────────────────

    public HandlerProblem CheckHandler(IMethodSymbol method, out HandlerReturn returns, out bool hasToken)
    {
        returns = HandlerReturn.Void;
        hasToken = false;

        if (method.IsStatic || method.IsGenericMethod || method.MethodKind != MethodKind.Ordinary
            || method.Parameters.Length is 0 or > 2
            || method.Parameters.Any(p => p.RefKind != RefKind.None || p.IsParams))
        {
            return HandlerProblem.BadSignature;
        }

        if (method.ReturnsVoid)
        {
            returns = HandlerReturn.Void;
        }
        else if (SymbolEqualityComparer.Default.Equals(method.ReturnType, Task))
        {
            returns = HandlerReturn.Task;
        }
        else if (SymbolEqualityComparer.Default.Equals(method.ReturnType, ValueTask))
        {
            returns = HandlerReturn.ValueTask;
        }
        else
        {
            return HandlerProblem.BadSignature;
        }

        if (method.Parameters.Length == 2)
        {
            if (returns == HandlerReturn.Void
                || !SymbolEqualityComparer.Default.Equals(method.Parameters[1].Type, CancellationToken))
            {
                return HandlerProblem.BadSignature;
            }

            hasToken = true;
        }

        return IsEvent(method.Parameters[0].Type) ? HandlerProblem.None : HandlerProblem.NotAnEvent;
    }

    // ── [UseState] ───────────────────────────────────────────────────────

    public UseStateProblem CheckUseState(IPropertySymbol property, string? selectorName, out ITypeSymbol? stateType, out IMethodSymbol? selector)
    {
        stateType = null;
        selector = null;

        if (property.IsStatic || property.IsIndexer || !property.IsPartialDefinition
            || property.GetMethod is null || property.SetMethod is not null)
        {
            return UseStateProblem.BadProperty;
        }

        if (selectorName is null)
        {
            if (!IsState(property.Type))
            {
                return UseStateProblem.NotAState;
            }

            stateType = property.Type;
            return UseStateProblem.None;
        }

        foreach (var candidate in property.ContainingType.GetMembers(selectorName).OfType<IMethodSymbol>())
        {
            if (candidate.IsStatic && !candidate.IsGenericMethod
                && candidate.Parameters.Length == 1
                && candidate.Parameters[0].RefKind == RefKind.None
                && IsState(candidate.Parameters[0].Type)
                && IsIdentityConvertible(candidate.ReturnType, property.Type))
            {
                selector = candidate;
                stateType = candidate.Parameters[0].Type;
                return UseStateProblem.None;
            }
        }

        return UseStateProblem.BadSelector;
    }

    /// <summary>Same type up to tuple element names and nullable annotations (e.g. <c>(int, int)</c> vs <c>(int A, int B)</c>).</summary>
    private bool IsIdentityConvertible(ITypeSymbol source, ITypeSymbol destination) =>
        SymbolEqualityComparer.Default.Equals(source, destination)
        || _compilation.ClassifyCommonConversion(source, destination).IsIdentity;

    // ── Attribute arguments ──────────────────────────────────────────────

    public static AttributeArguments ReadArguments(AttributeData attribute)
    {
        var scope = 0;
        string? scopeName = null;
        string? select = null;
        var render = true;

        foreach (var argument in attribute.NamedArguments)
        {
            switch (argument.Key)
            {
                case "Scope" when argument.Value.Value is int value:
                    scope = value;
                    break;
                case "ScopeName":
                    scopeName = argument.Value.Value as string;
                    break;
                case "Select":
                    select = argument.Value.Value as string;
                    break;
                case "Render" when argument.Value.Value is bool value:
                    render = value;
                    break;
            }
        }

        return new AttributeArguments(scope, scopeName, select, render);
    }

    /// <summary>Whether a location is inside code generated by the Razor compiler.</summary>
    public static bool IsRazorGenerated(Location location)
    {
        if (!location.IsInSource)
        {
            return false;
        }

        var filePath = location.SourceTree?.FilePath ?? string.Empty;
        if (filePath.EndsWith(".razor.g.cs", StringComparison.OrdinalIgnoreCase)
            || filePath.EndsWith("_razor.g.cs", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var mapped = location.GetMappedLineSpan();
        return mapped.HasMappedPath && mapped.Path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase);
    }
}

internal readonly struct AttributeArguments(int scope, string? scopeName, string? select, bool render)
{
    private const int Root = 1;
    private const int Named = 2;

    public string? ScopeName { get; } = scopeName;
    public string? Select { get; } = select;
    public bool Render { get; } = render;
    public int Scope { get; } = scope;

    /// <summary>A problem description for RFBUS010, or null if consistent.</summary>
    public string? ScopeProblem =>
        ScopeName is not null && Scope != Named && Scope != 0
            ? $"ScopeName '{ScopeName}' is set, so Scope is treated as Named; remove Scope or set it to ScopeKind.Named"
            : ScopeName is null && Scope == Named
                ? "Scope is ScopeKind.Named but ScopeName is not set; the nearest scope will be used"
                : null;

    /// <summary>The C# expression for the effective <c>ScopeTarget</c>.</summary>
    public string ToScopeTargetExpression()
    {
        const string prefix = "global::RForge.Blazor.Bus.ScopeTarget.";
        if (ScopeName is not null)
        {
            return prefix + "Named(" + SymbolDisplay.FormatLiteral(ScopeName, quote: true) + ")";
        }

        return Scope == Root ? prefix + "Root" : prefix + "Nearest";
    }
}
