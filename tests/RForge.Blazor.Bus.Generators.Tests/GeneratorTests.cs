using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace RForge.Blazor.Bus.Generators.Tests;

public sealed class GeneratorTests
{
    [Fact]
    public void Generates_wiring_for_every_supported_member_shape()
    {
        var result = Harness.Run("""
            using System.Threading;
            using System.Threading.Tasks;
            using RForge.Blazor.Bus;

            namespace App.Components;

            public partial class CartBadge : BusComponentBase
            {
                [UseState] private partial Counter Cart { get; }

                [UseState(Select = nameof(SelectCount), ScopeName = "checkout", Render = false)]
                protected partial int Count { get; }

                private static int SelectCount(Counter c) => c.Count;

                [Subscribe] private void OnSync(Ping e) { }
                [Subscribe(Scope = ScopeKind.Root)] private Task OnTask(Ping e) => Task.CompletedTask;
                [Subscribe] private ValueTask OnValueTask(Pong e) => default;
                [Subscribe] private Task OnTaskToken(Ping e, CancellationToken ct) => Task.CompletedTask;
                [Subscribe(Render = false)] private ValueTask OnValueTaskToken(Pong e, CancellationToken ct) => default;

                public int Read() => Cart.Count + Count;
            }
            """);

        Assert.Empty(result.CompilerErrors);
        // Three Ping handlers in one type is legal; the analyzer only warns.
        Assert.All(result.AnalyzerDiagnostics, d => Assert.Equal("RFBUS011", d.Id));

        var source = result.GeneratedSource("App.Components.CartBadge.Bus.g.cs");
        Assert.Contains("private partial global::App.Counter Cart => __bus_Cart.Value;", source);
        Assert.Contains("protected partial int Count => __bus_Count.Value;", source);
        Assert.Contains("__bus_Cart = builder.State<global::App.Counter>(global::RForge.Blazor.Bus.ScopeTarget.Nearest, render: true);", source);
        Assert.Contains("builder.State<global::App.Counter, int>((global::System.Func<global::App.Counter, int>)SelectCount, global::RForge.Blazor.Bus.ScopeTarget.Named(\"checkout\"), render: false);", source);
        Assert.Contains("builder.On<global::App.Ping>((global::System.Action<global::App.Ping>)OnSync, global::RForge.Blazor.Bus.ScopeTarget.Nearest, render: true);", source);
        Assert.Contains("(global::System.Func<global::App.Ping, global::System.Threading.Tasks.Task>)OnTask, global::RForge.Blazor.Bus.ScopeTarget.Root", source);
        Assert.Contains("(global::System.Func<global::App.Pong, global::System.Threading.Tasks.ValueTask>)OnValueTask", source);
        Assert.Contains("(global::System.Func<global::App.Ping, global::System.Threading.CancellationToken, global::System.Threading.Tasks.Task>)OnTaskToken", source);
        Assert.Contains("OnValueTaskToken, global::RForge.Blazor.Bus.ScopeTarget.Nearest, render: false);", source);
        Assert.Contains("partial void OnRegisterSubscriptions(global::RForge.Blazor.Bus.SubscriptionBuilder builder);", source);

        // State handles are registered before handlers, each in declaration order.
        Assert.True(source.IndexOf("__bus_Cart =", StringComparison.Ordinal) < source.IndexOf("__bus_Count =", StringComparison.Ordinal));
        Assert.True(source.IndexOf("__bus_Count =", StringComparison.Ordinal) < source.IndexOf("OnSync", StringComparison.Ordinal));
        Assert.True(source.IndexOf("OnSync", StringComparison.Ordinal) < source.IndexOf("OnValueTaskToken", StringComparison.Ordinal));
    }

    [Fact]
    public void Supports_nested_generic_types_and_global_namespace()
    {
        var result = Harness.Run(
            ("Nested.cs", """
                using RForge.Blazor.Bus;

                namespace App;

                public partial class Outer<T>
                {
                    public partial class Inner<U> : BusComponentBase
                    {
                        [Subscribe] private void On(Ping e) { }
                    }
                }
                """),
            ("Global.cs", """
                using RForge.Blazor.Bus;

                public partial class GlobalComponent : BusComponentBase
                {
                    [UseState] private partial App.Counter State { get; }
                }
                """));

        Assert.Empty(result.CompilerErrors);
        Assert.Contains("partial class Outer<T>", result.GeneratedSource("App.Outer_T_.Inner_U_.Bus.g.cs"));
        Assert.DoesNotContain("namespace", result.GeneratedSource("GlobalComponent.Bus.g.cs"));
    }

    [Fact]
    public void Derived_component_calls_base_registration()
    {
        var result = Harness.Run("""
            using RForge.Blazor.Bus;

            namespace App;

            public partial class BaseView : BusComponentBase
            {
                [Subscribe] private void OnPing(Ping e) { }
            }

            public partial class DerivedView : BaseView
            {
                [Subscribe] private void OnPong(Pong e) { }
            }
            """);

        Assert.Empty(result.CompilerErrors);
        Assert.Contains("base.RegisterSubscriptions(builder);", result.GeneratedSource("App.DerivedView.Bus.g.cs"));
    }

    [Fact]
    public void User_can_implement_the_partial_hook()
    {
        var result = Harness.Run("""
            using RForge.Blazor.Bus;

            namespace App;

            public partial class Hooked : BusComponentBase
            {
                [Subscribe] private void OnPing(Ping e) { }

                partial void OnRegisterSubscriptions(SubscriptionBuilder builder) => builder.On<Pong>(_ => { });
            }
            """);

        Assert.Empty(result.CompilerErrors);
        Assert.Empty(result.AnalyzerDiagnostics);
    }

    [Fact]
    public void Types_without_attributes_get_no_output()
    {
        var result = Harness.Run("""
            using RForge.Blazor.Bus;

            namespace App;

            public class Plain : BusComponentBase { }
            """);

        Assert.Empty(result.CompilerErrors);
        Assert.Empty(result.RunResult.GeneratedTrees);
    }

    [Fact]
    public void Unrelated_edits_reuse_cached_output()
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var component = CSharpSyntaxTree.ParseText("""
            using RForge.Blazor.Bus;
            namespace App;
            public partial class View : BusComponentBase { [Subscribe] private void On(Ping e) { } }
            """, parseOptions, "View.cs");
        var prelude = CSharpSyntaxTree.ParseText(Harness.Prelude, parseOptions, "Prelude.cs");

        var compilation = CSharpCompilation.Create("App", [prelude, component],
            Harness.Run("").Output.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new BusGenerator().AsSourceGenerator()],
            parseOptions: parseOptions,
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

        driver = driver.RunGenerators(compilation);
        driver = driver.RunGenerators(compilation.AddSyntaxTrees(
            CSharpSyntaxTree.ParseText("namespace App; public class Unrelated { }", parseOptions, "Unrelated.cs")));

        var outputs = driver.GetRunResult().Results.Single().TrackedOutputSteps
            .SelectMany(static step => step.Value)
            .SelectMany(static step => step.Outputs);

        Assert.All(outputs, output => Assert.True(
            output.Reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged,
            $"Expected cached output but was {output.Reason}"));
    }
}
