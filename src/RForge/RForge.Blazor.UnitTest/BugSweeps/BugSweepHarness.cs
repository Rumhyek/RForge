using Microsoft.AspNetCore.Components;
using RForgeBlazor.Models;

namespace RForge.Blazor.UnitTest.BugSweeps;

// Shared helpers for the #68 bug-sweep repro tests. Repro tests live in *_BugSweep classes,
// are [Ignore("pending #<follow-up#>")] red-by-construction, and each carries a
// "// BUG #<follow-up#>" tag naming the fix issue that un-ignores it (spec #68 §4/§8).

/// <summary>
/// Test-only component that captures the <see cref="DataGridContext"/> cascaded by RfDataGrid
/// so tests can raise grid events (filter/sort changes) exactly like shipped child components do.
/// </summary>
public class BugSweepContextSpy : ComponentBase
{
    [CascadingParameter]
    public DataGridContext GridContext { get; set; }

    /// <summary>The most recently captured grid context. Reset by each test before rendering.</summary>
    public static DataGridContext Captured;

    protected override void OnInitialized()
    {
        Captured = GridContext;
    }
}

/// <summary>Plain class (reference equality) used where comparer semantics matter.</summary>
public class SweepUser
{
    public int Id { get; set; }
    public string Name { get; set; }
}
