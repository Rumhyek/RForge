using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RForgeBlazor;
using RForgeBlazor.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BunitTestContext = Bunit.TestContext;

namespace RForge.Blazor.UnitTest.BugSweeps;

/// <summary>
/// Tester acceptance tests for #98 (spec #68 §8, both findings), derived from the spec
/// bullets' observable behavior — not from the fix implementation. The dev's
/// TreeView_BugSweep covers the two bad paths (revoked click ignored; double toggle
/// while a load is in flight ignored); this class adds the good paths and boundaries:
/// the click baseline, the re-enable direction of the AllowClick re-sync, and the
/// expansion guard releasing after the in-flight load completes.
/// Semantic locators only (role/text), no Bulma-class coupling beyond the
/// pre-existing tree-node/node-icon element contract the sweep tests already use.
/// </summary>
[TestClass]
public class TreeView_Issue98_Acceptance
{
    private BunitTestContext ctx;

    [TestInitialize]
    public void Setup() => ctx = new BunitTestContext();

    [TestCleanup]
    public void Cleanup() => ctx.Dispose();

    private static RenderFragment<SweepUser> NodeTemplate
        => user => builder => builder.AddContent(0, user.Name);

    private static RenderFragment LeafNodes(EventCallback<TreeViewNodeOnClickEventArgs<SweepUser>> nodeClick)
        => b =>
        {
            b.OpenComponent<RfTreeNode<SweepUser>>(0);
            b.AddAttribute(1, "NodeData", new SweepUser { Id = 1, Name = "leaf" });
            b.AddAttribute(2, "Node", NodeTemplate);
            b.AddAttribute(3, "NodeClick", nodeClick);
            b.CloseComponent();
        };

    // Bullet 1 (good path): with node clicks allowed, clicking a node fires NodeClick
    // exactly once — the guard fixes must not suppress the behavior that works today.
    [TestMethod]
    public void Given_AllowClickEnabled_When_ClickNode_Then_NodeClickFiresOnce()
    {
        int nodeClicks = 0;
        var nodeClick = EventCallback.Factory.Create<TreeViewNodeOnClickEventArgs<SweepUser>>(this,
            (Action<TreeViewNodeOnClickEventArgs<SweepUser>>)(_ => nodeClicks++));

        var cut = ctx.RenderComponent<RfTreeView<SweepUser>>(
            ComponentParameter.CreateParameter("Nodes", LeafNodes(nodeClick)),
            ComponentParameter.CreateParameter("AllowClick", true));

        cut.Find("a.tree-node").Click();

        Assert.IsTrue(nodeClicks == 1,
            "Given AllowClick=true, one node click must raise exactly one NodeClick.");
    }

    // Bullet 1 (boundary): the re-sync must work in BOTH directions — #98 reported the
    // revoke direction; the restore direction is the same contract on the same re-sync.
    [TestMethod]
    public void Given_AllowClickRevokedThenRestored_When_ClickNode_Then_NodeClickFires()
    {
        int nodeClicks = 0;
        var nodeClick = EventCallback.Factory.Create<TreeViewNodeOnClickEventArgs<SweepUser>>(this,
            (Action<TreeViewNodeOnClickEventArgs<SweepUser>>)(_ => nodeClicks++));
        var nodes = LeafNodes(nodeClick);

        var cut = ctx.RenderComponent<RfTreeView<SweepUser>>(
            ComponentParameter.CreateParameter("Nodes", nodes),
            ComponentParameter.CreateParameter("AllowClick", true));

        cut.SetParametersAndRender(
            ComponentParameter.CreateParameter("Nodes", nodes),
            ComponentParameter.CreateParameter("AllowClick", false));
        cut.SetParametersAndRender(
            ComponentParameter.CreateParameter("Nodes", nodes),
            ComponentParameter.CreateParameter("AllowClick", true));

        cut.Find("a.tree-node").Click();

        Assert.IsTrue(nodeClicks == 1,
            "Given AllowClick revoked then restored across re-renders, clicking must fire NodeClick again.");
    }

    // Bullet 2 (boundary): the in-flight guard must RELEASE — after a completed expand
    // load, a later toggle still expands/collapses normally. A guard that latches would
    // pass the dev's double-toggle repro while breaking the component permanently.
    [TestMethod]
    public void Given_CompletedExpandLoad_When_ToggleAgain_Then_ExpansionStillWorks()
    {
        var expandGate = new TaskCompletionSource<bool>();
        int handlerReturns = 0;
        var expandStates = new List<bool>();

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
                (Func<TreeViewNodeIsExpandEventArgs<SweepUser>, Task>)(async e =>
                {
                    lock (expandStates) { expandStates.Add(e.IsExpanded); }
                    await expandGate.Task; // consumer's lazy child-load
                    Interlocked.Increment(ref handlerReturns);
                })));
            b.CloseComponent();
        };

        var cut = ctx.RenderComponent<RfTreeView<SweepUser>>(
            ComponentParameter.CreateParameter("Nodes", nodes));

        // Expand with the load suspended, then let it complete.
        cut.Find("button.node-icon").Click();
        expandGate.TrySetResult(true);
        cut.WaitForState(() => Volatile.Read(ref handlerReturns) == 1, TimeSpan.FromSeconds(5));

        // Guard is released: a fresh toggle must collapse, raising a second event.
        cut.Find("button.node-icon").Click();
        cut.WaitForState(() => Volatile.Read(ref handlerReturns) == 2, TimeSpan.FromSeconds(5));

        bool[] states;
        lock (expandStates) { states = expandStates.ToArray(); }

        Assert.IsTrue(states.Length == 2 && states[0] && !states[1],
            "After a completed expand, a second toggle must raise NodeExpandChange with IsExpanded=false.");
        Assert.IsTrue(cut.Find("button.node-icon").GetAttribute("aria-expanded") == "false",
            "The node must end collapsed when the post-load collapse toggle completes.");
    }
}
