using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RForgeBlazor;
using RForgeBlazor.Models;
using System;
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
    [Ignore("pending #97 — #68 bug-sweep repro, red-by-construction")]
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
    [Ignore("pending #98 — #68 bug-sweep repro, red-by-construction")]
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
}
