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
