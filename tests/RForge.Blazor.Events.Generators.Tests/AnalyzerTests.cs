namespace RForge.Blazor.Events.Generators.Tests;

public sealed class AnalyzerTests
{
    private static string Component(string members, string declaration = "public partial class View : RfEventComponent") => $$"""
        using System;
        using System.Threading;
        using System.Threading.Tasks;
        using RForge.Blazor.Events;

        namespace App;

        {{declaration}}
        {
        {{members}}
        }
        """;

    [Fact]
    public void RFEVT001_attribute_in_razor_generated_code()
    {
        var result = Harness.Run(("Components_View_razor.g.cs", Component("[Subscribe] private void On(Ping e) { }")));
        Assert.Contains("RFEVT001", result.AnalyzerIds);
    }

    [Fact]
    public void RFEVT002_type_not_partial()
    {
        var result = Harness.Run(Component("[Subscribe] private void On(Ping e) { }", "public class View : RfEventComponent"));
        Assert.Contains("RFEVT002", result.AnalyzerIds);
    }

    [Fact]
    public void RFEVT002_containing_type_not_partial()
    {
        var result = Harness.Run(Component(
            "public partial class Inner : RfEventComponent { [Subscribe] private void On(Ping e) { } }",
            "public class View"));
        Assert.Contains("RFEVT002", result.AnalyzerIds);
    }

    [Fact]
    public void RFEVT003_not_an_event_component()
    {
        var result = Harness.Run(Component("[Subscribe] private void On(Ping e) { }", "public partial class View"));
        Assert.Contains("RFEVT003", result.AnalyzerIds);
    }

    [Theory]
    [InlineData("[Subscribe] private static void On(Ping e) { }")]
    [InlineData("[Subscribe] private void On<T>(Ping e) { }")]
    [InlineData("[Subscribe] private void On() { }")]
    [InlineData("[Subscribe] private void On(Ping e, CancellationToken ct) { }")]
    [InlineData("[Subscribe] private int On(Ping e) => 0;")]
    [InlineData("[Subscribe] private Task<int> On(Ping e) => Task.FromResult(0);")]
    [InlineData("[Subscribe] private Task On(Ping e, int other) => Task.CompletedTask;")]
    [InlineData("[Subscribe] private void On(ref Ping e) { }")]
    public void RFEVT004_invalid_handler_signature(string member)
    {
        Assert.Contains("RFEVT004", Harness.Run(Component(member)).AnalyzerIds);
    }

    [Fact]
    public void RFEVT005_parameter_not_an_event()
    {
        Assert.Contains("RFEVT005", Harness.Run(Component("[Subscribe] private void On(string e) { }")).AnalyzerIds);
    }

    [Theory]
    [InlineData("[UseState] private Counter State { get; }")]
    [InlineData("[UseState] private partial Counter State { get; set; }")]
    [InlineData("[UseState] private static partial Counter State { get; }")]
    public void RFEVT006_invalid_use_state_property(string member)
    {
        Assert.Contains("RFEVT006", Harness.Run(Component(member)).AnalyzerIds);
    }

    [Fact]
    public void RFEVT007_property_type_not_state()
    {
        Assert.Contains("RFEVT007", Harness.Run(Component("[UseState] private partial int State { get; }")).AnalyzerIds);
    }

    [Theory]
    [InlineData("[UseState(Select = \"Missing\")] private partial int Count { get; }")]
    [InlineData("[UseState(Select = nameof(Sel))] private partial int Count { get; } private int Sel(Counter c) => c.Count;")]
    [InlineData("[UseState(Select = nameof(Sel))] private partial int Count { get; } private static string Sel(Counter c) => c.Label;")]
    [InlineData("[UseState(Select = nameof(Sel))] private partial int Count { get; } private static int Sel(string s) => 0;")]
    public void RFEVT008_invalid_selector(string member)
    {
        var result = Harness.Run(Component(member));

        Assert.Contains("RFEVT008", result.AnalyzerIds);
        // The generator stubs the property so the analyzer's error isn't hidden behind CS9248.
        Assert.Empty(result.CompilerErrors);
    }

    [Fact]
    public void RFEVT007_property_is_stubbed_so_the_build_only_reports_the_analyzer_error()
    {
        var result = Harness.Run(Component("[UseState] private partial int State { get; }"));

        Assert.Empty(result.CompilerErrors);
        Assert.Contains("throw new global::System.InvalidOperationException", result.GeneratedSource("App.View.Hub.g.cs"));
    }

    [Fact]
    public void Selector_may_differ_from_property_in_tuple_names_only()
    {
        var result = Harness.Run(Component("""
            [UseState(Select = nameof(Sel))] private partial (int Count, string Label) Summary { get; }
            private static (int, string) Sel(Counter c) => (c.Count, c.Label);
            """));

        Assert.Empty(result.CompilerErrors);
        Assert.Empty(result.AnalyzerDiagnostics);
    }

    [Fact]
    public void RFEVT009_manual_override_with_attributes()
    {
        var result = Harness.Run(Component("""
            [Subscribe] private void On(Ping e) { }
            protected override void RegisterSubscriptions(SubscriptionBuilder builder) { }
            """));
        Assert.Contains("RFEVT009", result.AnalyzerIds);
    }

    [Theory]
    [InlineData("[Subscribe(Scope = ScopeKind.Named)] private void On(Ping e) { }")]
    [InlineData("[Subscribe(Scope = ScopeKind.Root, ScopeName = \"x\")] private void On(Ping e) { }")]
    public void RFEVT010_inconsistent_scope(string member)
    {
        Assert.Contains("RFEVT010", Harness.Run(Component(member)).AnalyzerIds);
    }

    [Fact]
    public void RFEVT011_duplicate_handlers()
    {
        var result = Harness.Run(Component("""
            [Subscribe] private void A(Ping e) { }
            [Subscribe] private void B(Ping e) { }
            """));
        Assert.Contains("RFEVT011", result.AnalyzerIds);
    }

    [Fact]
    public void RFEVT012_mutable_event()
    {
        var result = Harness.Run(Component(
            "[Subscribe] private void On(Mutable e) { }",
            "public sealed class Mutable : IEvent { public int Value { get; set; } }\npublic partial class View : RfEventComponent"));
        Assert.Contains("RFEVT012", result.AnalyzerIds);
    }

    [Fact]
    public void RFEVT013_state_not_a_record()
    {
        var result = Harness.Run(Component(
            "[UseState] private partial ClassState State { get; }",
            "public sealed class ClassState : IState<ClassState> { public static ClassState Initial => new(); }\npublic partial class View : RfEventComponent"));
        Assert.Contains("RFEVT013", result.AnalyzerIds);
    }

    [Theory]
    [InlineData("IDisposable", "public void Dispose() { }")]
    [InlineData("IAsyncDisposable", "public new ValueTask DisposeAsync() => default;")]
    public void RFEVT014_implements_disposal_directly(string iface, string member)
    {
        var result = Harness.Run(Component(member, $"public class View : RfEventComponent, {iface}"));
        Assert.Contains("RFEVT014", result.AnalyzerIds);
    }

    [Fact]
    public void Valid_component_reports_nothing()
    {
        var result = Harness.Run(Component("""
            [UseState] private partial Counter State { get; }
            [Subscribe(ScopeName = "x")] private Task On(Ping e, CancellationToken ct) => Task.CompletedTask;
            protected override ValueTask DisposeAsyncCore() => default;
            """));

        Assert.Empty(result.AnalyzerDiagnostics);
        Assert.Empty(result.CompilerErrors);
    }
}
