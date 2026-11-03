using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace RForge.Blazor.Events.Generators;

/// <summary>Reports misuse of <c>[Subscribe]</c>, <c>[UseState]</c> and <c>RfEventComponent</c> (RFEVT001–RFEVT014).</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EventAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        DiagnosticDescriptors.DeclaredInRazor,
        DiagnosticDescriptors.TypeNotPartial,
        DiagnosticDescriptors.NotEventComponent,
        DiagnosticDescriptors.InvalidHandlerSignature,
        DiagnosticDescriptors.HandlerParameterNotEvent,
        DiagnosticDescriptors.InvalidUseStateProperty,
        DiagnosticDescriptors.PropertyTypeNotState,
        DiagnosticDescriptors.InvalidSelector,
        DiagnosticDescriptors.ManualRegisterOverride,
        DiagnosticDescriptors.InconsistentScope,
        DiagnosticDescriptors.DuplicateHandler,
        DiagnosticDescriptors.MutableEvent,
        DiagnosticDescriptors.StateNotRecord,
        DiagnosticDescriptors.ImplementsDisposable);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        // Razor output is generated code; we must analyze it to catch attributes in @code blocks (RFEVT001).
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static start =>
        {
            if (EventSymbols.Create(start.Compilation) is not { } symbols || symbols.Subscribe is null || symbols.UseState is null)
            {
                return;
            }

            start.RegisterSymbolAction(ctx => AnalyzeType(ctx, symbols), SymbolKind.NamedType);
        });
    }

    private static void AnalyzeType(SymbolAnalysisContext context, EventSymbols symbols)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        var isComponent = symbols.DerivesFromComponent(type);

        if (isComponent)
        {
            CheckDisposal(context, symbols, type);
        }

        var handlers = new List<(IMethodSymbol Method, AttributeData Attribute)>();
        var states = new List<(IPropertySymbol Property, AttributeData Attribute)>();

        foreach (var member in type.GetMembers())
        {
            var attribute = member.GetAttributes().FirstOrDefault(a =>
                SymbolEqualityComparer.Default.Equals(a.AttributeClass, symbols.Subscribe)
                || SymbolEqualityComparer.Default.Equals(a.AttributeClass, symbols.UseState));
            if (attribute is null)
            {
                continue;
            }

            var location = member.Locations.FirstOrDefault() ?? Location.None;
            if (EventSymbols.IsRazorGenerated(location))
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.DeclaredInRazor, location, member.Name));
                continue;
            }

            switch (member)
            {
                case IMethodSymbol method when SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, symbols.Subscribe):
                    handlers.Add((method, attribute));
                    break;
                case IPropertySymbol property when SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, symbols.UseState):
                    states.Add((property, attribute));
                    break;
            }
        }

        if (handlers.Count == 0 && states.Count == 0)
        {
            return;
        }

        var typeLocation = type.Locations.FirstOrDefault(static l => !EventSymbols.IsRazorGenerated(l)) ?? type.Locations.FirstOrDefault() ?? Location.None;

        if (!EventSymbols.IsPartialIncludingContainers(type))
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.TypeNotPartial, typeLocation, type.Name));
        }

        if (!isComponent)
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.NotEventComponent, typeLocation, type.Name));
        }

        var manualOverride = type.GetMembers("RegisterSubscriptions").OfType<IMethodSymbol>()
            .Where(static m => m.IsOverride)
            .SelectMany(static m => m.DeclaringSyntaxReferences)
            .FirstOrDefault(static r => !r.SyntaxTree.FilePath.EndsWith(EventSymbols.GeneratedFileSuffix, StringComparison.Ordinal));
        if (manualOverride is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ManualRegisterOverride,
                Location.Create(manualOverride.SyntaxTree, manualOverride.Span),
                type.Name));
        }

        AnalyzeHandlers(context, symbols, type, handlers);
        AnalyzeStates(context, symbols, states);
    }

    private static void AnalyzeHandlers(
        SymbolAnalysisContext context,
        EventSymbols symbols,
        INamedTypeSymbol type,
        List<(IMethodSymbol Method, AttributeData Attribute)> handlers)
    {
        var seenEvents = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        foreach (var (method, attribute) in handlers)
        {
            var location = method.Locations.FirstOrDefault() ?? Location.None;
            ReportScopeProblem(context, attribute, location);

            switch (symbols.CheckHandler(method, out _, out _))
            {
                case HandlerProblem.BadSignature:
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.InvalidHandlerSignature, location, method.Name));
                    continue;

                case HandlerProblem.NotAnEvent:
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.HandlerParameterNotEvent,
                        method.Parameters[0].Locations.FirstOrDefault() ?? location,
                        method.Name,
                        method.Parameters[0].Type.ToDisplayString()));
                    continue;
            }

            var eventType = method.Parameters[0].Type;
            if (!seenEvents.Add(eventType))
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.DuplicateHandler, location, type.Name, eventType.ToDisplayString()));
            }

            if (!IsImmutable(eventType))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.MutableEvent,
                    method.Parameters[0].Locations.FirstOrDefault() ?? location,
                    eventType.ToDisplayString()));
            }
        }
    }

    private static void AnalyzeStates(
        SymbolAnalysisContext context,
        EventSymbols symbols,
        List<(IPropertySymbol Property, AttributeData Attribute)> states)
    {
        foreach (var (property, attribute) in states)
        {
            var location = property.Locations.FirstOrDefault() ?? Location.None;
            var arguments = EventSymbols.ReadArguments(attribute);
            ReportScopeProblem(context, attribute, location);

            switch (symbols.CheckUseState(property, arguments.Select, out var stateType, out _))
            {
                case UseStateProblem.BadProperty:
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.InvalidUseStateProperty, location, property.Name));
                    break;

                case UseStateProblem.NotAState:
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.PropertyTypeNotState, location, property.Name, property.Type.ToDisplayString()));
                    break;

                case UseStateProblem.BadSelector:
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.InvalidSelector, location, arguments.Select, property.Name, property.Type.ToDisplayString()));
                    break;

                case UseStateProblem.None when stateType is { IsRecord: false }:
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.StateNotRecord, location, stateType.ToDisplayString()));
                    break;
            }
        }
    }

    private static void ReportScopeProblem(SymbolAnalysisContext context, AttributeData attribute, Location location)
    {
        if (EventSymbols.ReadArguments(attribute).ScopeProblem is { } problem)
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.InconsistentScope, location, problem));
        }
    }

    private static void CheckDisposal(SymbolAnalysisContext context, EventSymbols symbols, INamedTypeSymbol type)
    {
        foreach (var direct in type.Interfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(direct, symbols.Disposable)
                || SymbolEqualityComparer.Default.Equals(direct, symbols.AsyncDisposable))
            {
                var location = type.Locations.FirstOrDefault(static l => !EventSymbols.IsRazorGenerated(l)) ?? type.Locations.FirstOrDefault() ?? Location.None;
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.ImplementsDisposable, location, type.Name, direct.Name));
            }
        }
    }

    private static bool IsImmutable(ITypeSymbol type)
    {
        if (!type.IsRecord)
        {
            return false;
        }

        foreach (var member in type.GetMembers())
        {
            switch (member)
            {
                case IPropertySymbol { IsStatic: false, SetMethod: { IsInitOnly: false } }:
                    return false;
                case IFieldSymbol { IsStatic: false, IsReadOnly: false, IsConst: false, IsImplicitlyDeclared: false }:
                    return false;
            }
        }

        return true;
    }
}
