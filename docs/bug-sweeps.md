# Bug sweeps & repro tests (#68)

An exhaustive runtime-bug hunt ran across every project in the solution (issue #68).
It landed as test infrastructure: a bUnit harness plus **25 repro tests, all
`[Ignore]`d and red-by-construction** — each one encodes a confirmed bug and stays
red until its fix issue is worked.

> **Note for contributors:** `AGENTS.md` still says the test project has "no bUnit".
> That line is stale — bUnit 1.40.0 is PO-approved (spec #68 §4) and installed in
> `RForge.Blazor.UnitTest`. This file is the current record until `AGENTS.md` is
> updated (a PO-owned file).

## Where things live

```
src/RForge/RForge.Blazor.UnitTest/BugSweeps/
├── BugSweepHarness.cs                  # shared test-only helpers
├── CountdownTimer_BugSweep.cs          (3 repros)
├── DataGrid_BugSweep.cs                (5 repros)
├── DialogManager_BugSweep.cs           (3 repros)
├── NotificationManager_BugSweep.cs     (3 repros)
├── RfDoubleClick_BugSweep.cs           (1 repro)
├── RfDropDownBase_BugSweep.cs          (2 repros)
├── RfDropDown_BugSweep.cs              (3 repros)
├── RfTabList_BugSweep.cs               (3 repros)
└── TreeView_BugSweep.cs                (2 repros)
```

Full sweep findings (including claims that were investigated and **disproved**)
are recorded on issue #68's timeline — the issue is the canonical record.

## How a repro test works

Each repro test:

1. Carries `[Ignore("pending #<fix-issue#>")]` — it does not run in the normal suite.
2. Carries a `// BUG #<fix-issue#>` comment tagging the fix issue it belongs to.
3. Asserts the *correct* behavior, so it is red whenever it is allowed to run
   against unfixed code.

## Working a fix issue (the TDD contract)

If you are assigned one of the drafted fix issues (#70–#114):

1. Find its tagged test: `grep -rn "BUG #<your-issue#>" BugSweeps/`.
2. **RED:** remove the `[Ignore]`, run it, and confirm it fails *for the reason the
   spec describes* — a failure for any other reason means the repro drifted and
   needs repair before the fix.
3. **GREEN:** implement the fix; the test must now pass, unmodified.
4. Keep the un-ignored test in the fix PR. Do not re-`[Ignore]` it and do not
   weaken its assertions.

If no tagged test exists for your issue (some findings had no bUnit repro — see
#68's timeline), write a new test against the same convention instead.

## bUnit usage in this repo

bUnit is 1.x: use `TestContext.RenderComponent<T>(ComponentParameter.CreateParameter(name, value))`
(bUnit 2.x's `TestContext.Render` does not exist here). MSTest's `TestContext`
collides with `Bunit.TestContext` — alias it (`using BunitTestContext = Bunit.TestContext;`).
The runner container has the net10 runtime only: build all TFMs, run tests with `-f net10.0`.

## Snapshots must be culture-invariant (#110)

A Verify snapshot is a committed file: the expected output in each `.verified.txt`
was recorded under one host culture. Any part of the verified payload that came
from a culture-dependent `ToString()` (a decimal, a date, a number list) prints a
different decimal separator or date order on a comma-decimal host such as de-DE
or fr-FR, so the test goes red with no behavior change at all.
`Rf_StyleWhen.TestLongStyle` hit exactly this: `(1m / 3m).ToString()` produced
`0,333...` under de-DE against a committed `0.333...` snapshot (issue #110,
fixed in PR #144).

The pattern: format every value that can reach a `Verify(...)` payload
culture-invariantly, so the string is identical on every host:

```csharp
(1m / 3m).ToString(CultureInfo.InvariantCulture)  // always "0.3333333333333333333333333333"
(1m / 3m).ToString()                              // host culture decides dot or comma
```

Verify does not normalize this for you: the separator is baked into the string
before Verify ever sees it. The same rule covers any assertion that compares a
formatted string against a committed fixture.

To prove a snapshot is culture-invariant, run the test class under a comma
culture; it must stay green:

```bash
LC_ALL=de_DE.UTF-8 dotnet test src/RForge/RForge.Blazor.UnitTest -f net10.0 --filter "Rf_StyleWhen"
```

(#110's tester used exactly this run as the red-at-base, green-at-head evidence.)

## Every snapshot needs a live test method (#111)

A `.verified.txt` file with no test method behind it is worse than no snapshot:
it reads as coverage, never runs, and if a method with that name is ever added
it validates against expectations recorded from some other class's behavior.
`Rf_Class` carried exactly that bug: 11 `Rf_Class.*.verified.txt` files for 6
Verify methods, all byte-identical copies of `Rf_ClassWhen` snapshots. The five
extra names were hide-semantics ones (`ShowTwoClasses`, `TestOneClassHide`,
`TestTwoClassHide`, `TestThreeClassOneHideTwoShow`,
`TestThreeClassTwoHideOneShow`), and `Rf.Class` has no show/hide parameter at
all, so those expectations were both unreachable and wrong. Issue #111 (PR #145)
deleted the five orphans and their dangling `None Update` entries in
`RForge.Blazor.UnitTest.csproj`; the class now holds 6 methods and 6 snapshots,
1:1.

The invariant: for a Verify test class, the set of
`<Class>.<Method>.verified.txt` files equals the set of methods that call
`Verify(...)`, because Verify names each snapshot after the method that
produced it. The same rule catches `.received.txt` leftovers from renamed or
deleted tests, and it reaches the csproj: each snapshot is listed there as a
`None Update` entry, so removing a snapshot must remove its entry too (the #145
review bounce caught exactly that half-done deletion).

Audit a class by diffing its method names against its snapshot basenames; the
output is empty when the class is 1:1 (names only on the left have no snapshot,
names only on the right are orphans):

```bash
cd src/RForge/RForge.Blazor.UnitTest
C=Rf_Class
comm -3 \
  <(grep -oE 'public Task [A-Za-z0-9_]+\(\)' $C.cs | sed -E 's/public Task ([A-Za-z0-9_]+).*/\1/' | sort) \
  <(ls $C.*.verified.txt | sed -E "s/^$C\.(.*)\.verified\.txt$/\1/" | sort)
```

The grep assumes the house pattern of `public Task Name()` test methods (as in
`Rf_Class` and `Rf_ClassWhen`); widen it if a class uses other signatures.

## Invoked RenderFragments need a null guard (#73)

Razor renders a `RenderFragment` in two forms that compile differently. A bare
`@ChildContent` becomes a `RenderTreeBuilder.AddContent` call and is null-safe
when the parameter was never set. The *invoked* form, `@ChildContent(item)`,
becomes `ChildContent.Invoke(item)` and throws `NullReferenceException` inside
`BuildRenderTree` when the parameter is null: the crash happens during render
and takes the whole component down, not just the slot.

The #68 sweep found this class at six sites, three in each dropdown component:
the selected-value, show-at-top-row, and item-loop `ChildContent` fallbacks in
`RfDropDown.razor` (lines 23, 84, 121) and `RfDropDownMulti.razor` (lines 30,
105, 143). The title's "7 sites" is arithmetic drift in the discovery note:
canary-4 plus "5 more sites" is 6, and a grep of main confirms exactly 6. The
`@SelectedItemTemplate`, `@RowItemTemplate`, and `@EmptyContentTemplate`
invocations were already null-guarded and stayed untouched.

Consumer-visible before the fix: a dropdown with a selection but neither the
matching template nor `ChildContent` (e.g. `SelectedItem` set with no
templates at all) threw the NRE at render and never painted. PR #128 (merge
`7231fb3`) guards every invoked site with `else if (ChildContent != null)`,
mirroring each file's own `@if (Template != null)` idiom: the slot now renders
empty while rows, chips, and the selected anchor still render. The render
change is consumer-visible, so it carries a `CHANGELOG.md` bullet (it feeds
`PackageReleaseNotes`). Six `// BUG #73` tests in `RfDropDown_BugSweep.cs`
pin it, one per site, all `*DoesNotThrow` (the multi-component repros live in
that same class).

When fixing a template-render crash, find every invoked site in the component
and check each one sits under a null test; leave bare `@Frag` sites alone,
they are already null-safe and do not need a "fix":

```bash
cd src/RForge/RForgeBlazor
grep -nE '@(ChildContent|[A-Za-z]+Template)\(' RfDropDown.razor RfDropDownMulti.razor
grep -B2 -E '@(ChildContent|[A-Za-z]+Template)\(' RfDropDown.razor RfDropDownMulti.razor \
  | grep -E '!= null'
```

The second command prints one guard line per invoked site (14 for these two
files at the time of writing: 7 per file, 4 template invocations plus 3
`ChildContent` fallbacks each). A site with no line above it is a crash
waiting for a consumer who omits that template.

## A custom ItemComparer must reach every match site (#75)

`RfDropDownMulti` answers "is this item selected?" and "remove this item" at
three sites in `RfDropDownMulti.razor.cs`: the `IsSelected` membership test,
the `OnItemClick` add/remove toggle, and the tag 'x' handler
`OnDeselectClick`. `ItemComparer` is the parameter through which a consumer
with identity-by-Id models defines what "same item" means; a comparer that
steers two of the three sites is a partial contract.

Before the fix the tag handler was the odd site out. `OnDeselectClick` called
`SelectedItems.Remove(item)`, which is `List<T>`'s default equality
(`EqualityComparer<TItem>.Default`: reference equality unless `TItem`
overrides `Equals`), while `IsSelected` and `OnItemClick` both compared with
`ItemComparer`. With a custom comparer set, a click on a tag carrying an
instance that was comparer-equal but not reference-equal to the
`SelectedItems` entry made `Remove` return false: silent early return, no
`SelectedItemsChanged`, and the tag stayed stuck. The #71 red-run arbitration
showed the naive path always hits (the razor tag loop iterates the list's own
instances), so the reachable vehicle is an in-place selection swap: the
consumer mutates the bound `List<T>` (clear and re-add an equal-but-new
instance), Blazor has no change detection on `List<T>` so no re-render
happens, and the still-visible tag keeps its click closure over the departed
instance.

Consumer-visible before PR #158 (merge `947ea4d`): that tag click did
nothing. After: `OnDeselectClick` removes with `SelectedItems.RemoveAll(i =>
ItemComparer(i, item) == true)`, the exact expression `OnItemClick` uses, so a
comparer-equal entry is dropped, `SelectedItemsChanged` fires, and the tag
disappears. A true miss (zero removals) still early-returns without emitting.
Default-comparer consumers see no change: the component's fallback comparer is
`EqualityComparer<TItem>.Default`, what `List.Remove` already used, so the fix
is behavior-preserving for them. The consumer-visible change for custom
comparers rode with the fix PR as a `CHANGELOG.md` bullet (it feeds
`PackageReleaseNotes`). `RfDropDownMulti_BugSweep.TagDeselect_HonorsItemComparer`
pins the behavior; it was authored fresh on the fix issue (sweep PR #115
committed no repro for this row, the #104 precedent) and ships un-ignored as
the TDD red-to-green step.

The audit rule: when a component exposes a custom-equality parameter, every
site that matches or removes an item by value must route through it; one site
left on `List<T>` defaults is a silent no-op waiting for the consumer who sets
the parameter:

```bash
cd src/RForge/RForgeBlazor
grep -nE 'SelectedItems\.(Remove|RemoveAll|Contains|Any|IndexOf)\(' RfDropDownMulti.razor.cs
```

Every line the command prints must mention `ItemComparer` (three at the time
of writing: the `IsSelected` `Any`, the `OnItemClick` toggle, the
`OnDeselectClick` remove). The pattern deliberately excludes the positional
`RemoveAt` trims under `MaxSelectableItems`, which match no item. A printed
line without `ItemComparer` is this bug class: re-test it with a comparer
stricter than reference equality.
