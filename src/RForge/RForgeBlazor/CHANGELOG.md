# RForge.Blazor

## Unreleased

- Fixed `RfDataGrid`: the Next and Last pager buttons no longer emit page index `-1` when the grid
  is empty (`TotalCount=0`, so `TotalPages=0`). Previously `OnNextPageClick` / `OnLastPageClick`
  computed `TotalPages - 1` and fired `CurrentPageIndexChanged(-1)`, which consumers fed into a
  negative `Skip` (ArgumentOutOfRangeException). The target page is now clamped to the valid range,
  and the Next/Last disabled guards use `>=` instead of `==`, so both buttons render disabled on an
  empty grid instead of staying enabled. Behavior on non-empty grids is unchanged (#91).
- Fixed `DataGridContext` / `RfTabListContext`: raising a multicast async event now awaits every
  subscriber. Previously the raisers invoked the delegate directly, which returns only the LAST
  subscriber's task, so earlier subscribers ran fire-and-forget: their exceptions were unobserved
  and the raiser returned while their work was still in flight (e.g. `SortChanged` could return
  while a data load was still running). All subscribers now run to completion; a single subscriber
  fault is rethrown unchanged, multiple faults are combined into one `AggregateException` (#90).
- Fixed `RfDropDown`: a consumer `OnLoad` that throws no longer disables the dropdown forever.
  `OpenDropDown` set `IsLoading` without a `try/finally`, so the thrown load left the spinner
  (`is-loading`) latched on and the open-guard rejected every later click; the latch now clears
  and the dropdown can be reopened after a failed load. Successful-open behavior is unchanged (#70).
- Fixed `RfTreeView`: changing `AllowClick` after the first render now takes effect. The flag
  was only copied to the cascaded context at initialization, so revoking node clicks later was
  silently ignored and nodes stayed clickable (#98).
- Fixed `RfTreeNode`: a toggle click that arrives while an expand/collapse load is still in
  flight is now ignored. Previously it collapsed the node mid-load and raised a duplicate,
  misordered `NodeExpandChange`, leaving children under a collapsed node (#98).
- Fixed `RfDropDownMulti`: the first item click no longer throws `NullReferenceException` when a
  consumer binds only `SelectedItemsChanged`. `SelectedItems` was never started when unset; the
  click path now starts the list, adds the item, and emits `SelectedItemsChanged` (#74).
- Fixed `RfDropDown` / `RfDropDownMulti`: the `CssClass` parameter is now applied to the dropdown root element and `CssFilterClass` to the filter input. Both parameters existed but were silently ignored; existing consumers that set them will now see their styles apply (#66).
- Fixed `CountdownTimer`: `Status` now tracks the real lifecycle. Subscribing an elapsed handler
  via `OnElapsed` no longer marks a never-run timer `Completed`, and a countdown that runs to
  completion now ends `Completed` instead of staying `Running`. Code keyed off `Status` (completion
  checks, restart logic) will observe the corrected values (#82).
- CI: merges to `main` no longer push packages to nuget.org. Each merge still builds and
  publishes packages to the GitHub package feed and creates a draft release; packages reach
  nuget.org only when a maintainer publishes a release (#116).
- Fixed `DialogManager` / `RfDialogManager`: disposing a replaced `RfDialogManager` no longer
  unregisters the live one. `Unregister` had no registrant-identity check, so when one dialog
  host component replaced another, the old host's `Dispose` cleared the new registration and
  every later Alert/Confirm/Prompt queued forever and no dialog ever rendered again. The
  component now unregisters with an identity check; the existing no-arg `Unregister()` on
  `IDialogManager` is unchanged for existing callers (#86).
