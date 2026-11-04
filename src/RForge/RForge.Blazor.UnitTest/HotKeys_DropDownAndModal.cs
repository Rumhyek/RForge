using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BunitTestContext = Bunit.TestContext;

namespace RForge.Blazor.UnitTest;

/// <summary>
/// Dropdowns and modals use <see cref="RfHotKey"/> for Esc while open, and <c>UseHotKeys="false"</c> means no JS at all.
/// A hot key press is simulated by calling what JavaScript calls.
/// </summary>
[TestClass]
public class HotKeys_DropDownAndModal
{
    private BunitTestContext ctx;
    private BunitJSModuleInterop module;

    [TestInitialize]
    public void Setup()
    {
        ctx = new BunitTestContext();
        module = ctx.SetupHotKeys();
    }

    [TestCleanup]
    public void Cleanup() => ctx.Dispose();

    private int Registered => module.Invocations["register"].Count;

    private void AssertNoJs() => Assert.AreEqual(0, ctx.JSInterop.Invocations.Count, "UseHotKeys=false must not make any JS call.");

    private IRenderedComponent<RfDropDown<string>> RenderDropDown(bool open, bool useHotKeys = true, bool isReadonly = false) =>
        ctx.RenderComponent<RfDropDown<string>>(p => p
            .Add(x => x.Items, new List<string> { "a", "b" })
            .Add(x => x.IsOpen, open)
            .Add(x => x.IsReadonly, isReadonly)
            .Add(x => x.UseHotKeys, useHotKeys)
            .Add(x => x.ChildContent, item => b => b.AddContent(0, item)));

    [TestMethod]
    public void Given_ClosedDropDown_When_Rendered_Then_NoJs()
    {
        RenderDropDown(open: false);

        AssertNoJs();
    }

    [TestMethod]
    public async Task Given_OpenDropDown_When_EscPressed_Then_Closes()
    {
        var cut = RenderDropDown(open: true);
        Assert.AreEqual(1, Registered);

        await cut.FindComponent<RfHotKey>().Instance.OnKeyPressed();

        cut.WaitForAssertion(() => Assert.IsFalse(cut.Find(".dropdown").ClassList.Contains("is-active")));
        Assert.IsFalse(cut.HasComponent<RfHotKey>(), "A closed dropdown must not keep its hot key.");
        module.VerifyInvoke("unregister");
    }

    [TestMethod]
    public void Given_ReadonlyOpenDropDown_When_Rendered_Then_NoHotKey()
    {
        var cut = RenderDropDown(open: true, isReadonly: true);

        Assert.IsFalse(cut.HasComponent<RfHotKey>());
        AssertNoJs();
    }

    [TestMethod]
    public void Given_OptedOutDropDown_When_Open_Then_NoJs()
    {
        RenderDropDown(open: true, useHotKeys: false);

        AssertNoJs();
    }

    [TestMethod]
    public void Given_OptedOutDropDown_When_EscOnItem_Then_Closes()
    {
        var cut = RenderDropDown(open: true, useHotKeys: false);

        cut.Find("a.dropdown-item").KeyDown(new KeyboardEventArgs { Code = "Escape" });

        Assert.IsFalse(cut.Find(".dropdown").ClassList.Contains("is-active"));
    }

    [TestMethod]
    public void Given_HotKeyDropDown_When_EscOnItem_Then_LeftToTheHotKey()
    {
        var cut = RenderDropDown(open: true);

        cut.Find("a.dropdown-item").KeyDown(new KeyboardEventArgs { Code = "Escape" });

        Assert.IsTrue(cut.Find(".dropdown").ClassList.Contains("is-active"),
            "Handling Esc on the item as well as through the hot key would close two layers on one press.");
    }

    [TestMethod]
    public async Task Given_OpenDropDownMulti_When_EscPressed_Then_Closes()
    {
        var cut = ctx.RenderComponent<RfDropDownMulti<string>>(p => p
            .Add(x => x.Items, new List<string> { "a" })
            .Add(x => x.IsOpen, true)
            .Add(x => x.ChildContent, item => b => b.AddContent(0, item)));

        await cut.FindComponent<RfHotKey>().Instance.OnKeyPressed();

        cut.WaitForAssertion(() => Assert.IsFalse(cut.Find(".dropdown").ClassList.Contains("is-active")));
    }

    [TestMethod]
    public void Given_OptedOutDropDownMulti_When_Open_Then_NoJs()
    {
        ctx.RenderComponent<RfDropDownMulti<string>>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.UseHotKeys, false));

        AssertNoJs();
    }

    [TestMethod]
    public void Given_HiddenModal_When_Rendered_Then_NoJs()
    {
        ctx.RenderComponent<RfModal>(p => p.Add(x => x.IsVisible, false));
        ctx.RenderComponent<RfModalCard>(p => p.Add(x => x.IsVisible, false));
        ctx.RenderComponent<RfModalImage>(p => p.Add(x => x.IsVisible, false));

        AssertNoJs();
    }

    [TestMethod]
    public void Given_OptedOutModal_When_Visible_Then_NoJs()
    {
        ctx.RenderComponent<RfModal>(p => p.Add(x => x.IsVisible, true).Add(x => x.UseHotKeys, false));
        ctx.RenderComponent<RfModalCard>(p => p.Add(x => x.IsVisible, true).Add(x => x.UseHotKeys, false));
        ctx.RenderComponent<RfModalImage>(p => p.Add(x => x.IsVisible, true).Add(x => x.UseHotKeys, false));

        AssertNoJs();
    }

    [TestMethod]
    public async Task Given_VisibleModals_When_EscPressed_Then_AskToClose()
    {
        bool? modal = null, card = null, image = null;

        var modalCut = ctx.RenderComponent<RfModal>(p => p
            .Add(x => x.IsVisible, true)
            .Add(x => x.IsVisibleChanged, v => modal = v));
        var cardCut = ctx.RenderComponent<RfModalCard>(p => p
            .Add(x => x.IsVisible, true)
            .Add(x => x.IsVisibleChanged, v => card = v));
        var imageCut = ctx.RenderComponent<RfModalImage>(p => p
            .Add(x => x.IsVisible, true)
            .Add(x => x.IsVisibleChanged, v => image = v));

        await modalCut.FindComponent<RfHotKey>().Instance.OnKeyPressed();
        await cardCut.FindComponent<RfHotKey>().Instance.OnKeyPressed();
        await imageCut.FindComponent<RfHotKey>().Instance.OnKeyPressed();

        Assert.AreEqual(false, modal);
        Assert.AreEqual(false, card);
        Assert.AreEqual(false, image);
    }

    [TestMethod]
    public async Task Given_ModalWithNoCloseButton_When_EscPressed_Then_StaysOpenButStillClaimsEsc()
    {
        bool? changed = null;
        var cut = ctx.RenderComponent<RfModal>(p => p
            .Add(x => x.IsVisible, true)
            .Add(x => x.ShowCloseButton, false)
            .Add(x => x.IsVisibleChanged, v => changed = v));

        Assert.AreEqual(1, Registered, "It must still register, or Esc would reach a modal behind this one.");

        await cut.FindComponent<RfHotKey>().Instance.OnKeyPressed();

        Assert.IsNull(changed);
    }

    [TestMethod]
    public void Given_ModalClosed_When_Rerendered_Then_Unregisters()
    {
        var cut = ctx.RenderComponent<RfModal>(p => p.Add(x => x.IsVisible, true));

        cut.SetParametersAndRender(p => p.Add(x => x.IsVisible, false));

        module.VerifyInvoke("unregister");
    }

    [TestMethod]
    public void Given_DropDownInsideModal_When_Rendered_Then_DropDownRegistersLast()
    {
        var cut = ctx.RenderComponent<RfModal>(p => p
            .Add(x => x.IsVisible, true)
            .Add(x => x.ChildContent, (RenderFragment)(b =>
            {
                b.OpenComponent<RfDropDown<string>>(0);
                b.AddComponentParameter(1, nameof(RfDropDown<string>.IsOpen), true);
                b.CloseComponent();
            })));

        // The newest registration wins, so the inner layer must come second to get Esc first. Both register "escape",
        // so the order is checked through the owner of each reference.
        var owners = module.Invocations["register"]
            .Select(i => ((DotNetObjectReference<RfHotKey>)i.Arguments[0]).Value)
            .ToList();

        var dropDownHotKey = cut.FindComponent<RfDropDown<string>>().FindComponent<RfHotKey>().Instance;

        Assert.AreEqual(2, owners.Count);
        Assert.AreSame(dropDownHotKey, owners[1]);
    }
}
