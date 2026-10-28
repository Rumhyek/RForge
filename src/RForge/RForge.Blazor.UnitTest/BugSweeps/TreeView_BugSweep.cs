using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RForgeBlazor;
using RForgeBlazor.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using BunitTestContext = Bunit.TestContext;

namespace RForge.Blazor.UnitTest.BugSweeps;

/// <summary>
/// Repro tests from the #68 bug sweep for RfTreeView / RfTreeNode. Red-by-construction.
/// </summary>
[TestClass]
public class TreeView_BugSweep
{
    private BunitTestContext ctx;

    [TestInitialize]
    public void Setup() => ctx = new BunitTestContext();

    [TestCleanup]
    public void Cleanup() => ctx.Dispose();

    private static RenderFragment<SweepUser> NodeTemplate
        => user => builder => builder.AddContent(0, user.Name);

    // BUG #97 — RfTreeNode.razor:33,66: @Node(NodeData) is invoked unguarded in BOTH the
    // has-children and the leaf branch, although Node is an optional RenderFragment<TTreeItemData>
    // (every other optional fragment in the tree components is guarded). crash, low.
    [TestMethod]
    public void NoNodeTemplate_DoesNotThrow()
    {
        RenderFragment nodes = b =>
        {
            b.OpenComponent<RfTreeNode<SweepUser>>(0);
            b.AddAttribute(1, "NodeData", new SweepUser { Id = 1, Name = "root" });
            b.CloseComponent();
        };

        var cut = ctx.RenderComponent<RfTreeView<SweepUser>>(
            ComponentParameter.CreateParameter("Nodes", nodes));

        // Today the leaf branch invokes @Node(NodeData) → NRE during render.
        Assert.IsTrue(cut.Markup.Contains("tree-node"),
            "RfTreeNode must render safely with NodeData but no Node template (guard it like the other optional fragments).");
    }

    // BUG #98 — RfTreeView.razor.cs:77-84: OnParametersSet re-syncs AllowSelection/AllowExpand/
    // ShowAsPrerender onto the context but NOT AllowNodeClick (only set at OnInitialized:65) →
    // flipping AllowClick=false after first render is ignored, nodes stay clickable. stuck, low.
    [TestMethod]
    public void AllowClickChange_TakesEffect()
    {
        int nodeClicks = 0;

        RenderFragment nodes = b =>
        {
            b.OpenComponent<RfTreeNode<SweepUser>>(0);
            b.AddAttribute(1, "NodeData", new SweepUser { Id = 1, Name = "leaf" });
            b.AddAttribute(2, "Node", NodeTemplate);
            b.AddAttribute(3, "NodeClick", EventCallback.Factory.Create<TreeViewNodeOnClickEventArgs<SweepUser>>(this,
                (Action<TreeViewNodeOnClickEventArgs<SweepUser>>)(_ => nodeClicks++)));
            b.CloseComponent();
        };

        var cut = ctx.RenderComponent<RfTreeView<SweepUser>>(
            ComponentParameter.CreateParameter("Nodes", nodes),
            ComponentParameter.CreateParameter("AllowClick", true));

        // Parent revokes node clicks after first render.
        cut.SetParametersAndRender(
            ComponentParameter.CreateParameter("Nodes", nodes),
            ComponentParameter.CreateParameter("AllowClick", false));

        cut.Find("a.tree-node").Click();

        Assert.IsTrue(nodeClicks == 0,
            "AllowClick=false must take effect on re-render; today AllowNodeClick is only seeded at OnInitialized and never re-synced.");
    }

    // BUG #98 — RfTreeNode.razor.cs:207-245 (ChangeExpansion): NO in-flight guard around the
    // awaited NodeExpandChange. A second toggle while the consumer's expand load is still in
    // flight collapses the node mid-load and raises a SECOND NodeExpandChange →
    // duplicate/misordered expand callbacks, children land under a collapsed node. race, med.
    // NOTE: authored in the #98 fix PR — this repro never landed in #68's test PR (only
    // AllowClickChange_TakesEffect did); written to the same BugSweeps convention per
    // docs/bug-sweeps.md ("If no tagged test exists … write a new test"). See #98 timeline.
    [TestMethod]
    public void DoubleToggle_ExpandFiresOnce()
    {
        var expandGate = new TaskCompletionSource<bool>();
        int expandEvents = 0;

        RenderFragment children = b =>
        {
            b.OpenComponent<RfTreeNode<SweepUser>>(0);
            b.AddAttribute(1, "NodeData", new SweepUser { Id = 2, Name = "child" });
            b.AddAttribute(2, "Node", NodeTemplate);
            b.CloseComponent();
        };

        RenderFragment nodes = b =>
        {
            b.OpenComponent<RfTreeNode<SweepUser>>(0);
            b.AddAttribute(1, "NodeData", new SweepUser { Id = 1, Name = "root" });
            b.AddAttribute(2, "Node", NodeTemplate);
            b.AddAttribute(3, "Children", children);
            b.AddAttribute(4, "NodeExpandChange", EventCallback.Factory.Create<TreeViewNodeIsExpandEventArgs<SweepUser>>(this,
                (Func<TreeViewNodeIsExpandEventArgs<SweepUser>, Task>)(async _ =>
                {
                    Interlocked.Increment(ref expandEvents);
                    await expandGate.Task; // consumer's lazy child-load is in flight
                })));
            b.CloseComponent();
        };

        var cut = ctx.RenderComponent<RfTreeView<SweepUser>>(
            ComponentParameter.CreateParameter("Nodes", nodes));

        cut.Find("button.node-icon").Click(); // expand — suspends inside the awaited NodeExpandChange
        cut.Find("button.node-icon").Click(); // second toggle mid-load: today this collapses + re-fires

        expandGate.TrySetResult(true);
        try
        {
            cut.WaitForState(() => Volatile.Read(ref expandEvents) >= 2, TimeSpan.FromMilliseconds(500));
        }
        catch (Exception)
        {
            // wait never satisfied == the second toggle was guarded and NodeExpandChange never ran twice.
        }

        Assert.IsTrue(Volatile.Read(ref expandEvents) == 1,
            "A toggle raised while an expand load is in flight must be ignored; today it collapses mid-load and raises a second NodeExpandChange (duplicate/misordered callbacks).");

        Assert.IsTrue(cut.Find("button.node-icon").GetAttribute("aria-expanded") == "true",
            "The node must end expanded after a double-toggle whose first load completed.");
    }
}
