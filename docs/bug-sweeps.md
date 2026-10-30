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
