# RForge.Blazor

## Unreleased

- Fixed `RfDropDown` / `RfDropDownMulti`: rendering no longer throws a `NullReferenceException` when an item or selection must render with no template set. The invoked `@ChildContent` fallbacks (trigger display, show-at-top row, and item loop, in both components) are now null-guarded: with `ChildContent`, `RowItemTemplate`, and `SelectedItemTemplate` all unset, the slots render empty instead of crashing the render (#73).
- Fixed `RfDoubleClick`: a pending `OnSingleClick` no longer fires after the component is
  disposed (the click-delay continuation now tracks the component lifetime), and an `Element`
  parameter set to null, empty, or whitespace now falls back to the default `button` element
  instead of throwing `ArgumentNullException` on every render (#76).
- Fixed `RfContextMenu`: menu DOM ids are now unique per instance. The `aria-controls` / menu
  `id` value came from a per-instance `Random`, so instances could draw the same number and
  render duplicate DOM ids, silently breaking the trigger's `aria-controls` link and any JS
  targeting the menu. Consumer-visible: the id suffix changed from a random 6-digit number to
  a sequential per-process number (the `context-menu-` prefix is unchanged) (#77).
- Fixed `RfNotificationManager`: `INotificationManager.ClearAll()` no longer throws. The component's
  `OnClearAll` handler was an unimplemented `throw new NotImplementedException()`, so every `ClearAll()`
  call crashed into the caller and aborted the event multicast to later subscribers. It now disposes any
  countdown timers, clears every notification from all positions, and re-renders (#78).
- Fixed `RfTab`: a consumer `OnLoad` that throws no longer latches the tab forever.
  `TabListOnLoad` set `IsLoading` without a `try/finally`, so the thrown load left the spinner
  (`is-loading`) stuck on the tab and the load guard rejected every later activation of that
  tab; the latch now clears and a failed tab load can be retried by re-activating the tab.
  Successful-load behavior is unchanged (#89).
- Fixed `RfTreeNode`: a tree node rendered without the optional `Node` template no longer
  throws a `NullReferenceException` during render. `@Node(NodeData)` was invoked unguarded in
  both the has-children and the leaf branch; both sites now render only when `Node` is set,
  matching how every other optional fragment in the library is guarded. Nodes without a
  template now render their surrounding node markup with an empty label (#97).
- Docs app: the DataGrid double-click example no longer crashes for users with a null `Bio`
  (mock user Id 90). The detail modal's truncation branch is now null-guarded instead of
  dereferencing `ShownUser.Bio.Length` (a null Bio renders as an empty paragraph), and the
  paged user API's `Bio` filter skips null rows rather than 500s. Library surface and
  rendered output are unchanged; only the docs app's crash paths were fixed (#99).
- Fixed `RfDataGrid`: changing the filter now resets the pager to the first page.
  `OnFilterChangedCallback` emitted `CurrentPageIndexChanged(0)` but left the local
  `CurrentPageIndex` at its stale value, so a consumer that does not echo the new index back kept
  seeing the old page in the pager label, and the next Next/Last click emitted a stale page number
  (e.g. page 3 instead of page 1 after filtering from page 3). The callback now zeroes the local
  index before emitting, matching the sort path. Behavior is unchanged for consumers that echo
  `CurrentPageIndex` back to the grid (#92).
- Fixed `RfDropDownMulti`: tag deselect now honors a custom `ItemComparer`. `OnDeselectClick`
  removed the item with `List.Remove` (reference equality) while `IsSelected` and item clicks
  compared with `ItemComparer`, so clicking the 'x' on a tag for an item that was comparer-equal
  but not reference-equal to the `SelectedItems` entry removed nothing, emitted no
  `SelectedItemsChanged`, and left the tag stuck. The remove now goes through `ItemComparer`,
  matching the item-click toggle path. Consumers using the default comparer see no change (#75).
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
- Fixed `CountdownTimer`: `StartAsync` now ignores a start call while the timer is already running.
  Previously a double-start (e.g. a double-tapped button) tried to run a second countdown loop,
  which `PeriodicTimer` rejects: the second `StartAsync` task faulted silently (fire-and-forget
  callers never saw it, awaiting callers got an exception mid-countdown). The ignored second call
  now returns a completed task, and elapsed fires exactly once per countdown (#83).
- CI: merges to `main` no longer push packages to nuget.org. Each merge still builds and
  publishes packages to the GitHub package feed and creates a draft release; packages reach
  nuget.org only when a maintainer publishes a release (#116).
- Fixed `DialogManager` / `RfDialogManager`: disposing a replaced `RfDialogManager` no longer
  unregisters the live one. `Unregister` had no registrant-identity check, so when one dialog
  host component replaced another, the old host's `Dispose` cleared the new registration and
  every later Alert/Confirm/Prompt queued forever and no dialog ever rendered again. The
  component now unregisters with an identity check; the existing no-arg `Unregister()` on
  `IDialogManager` is unchanged for existing callers (#86).
- Fixed `RfDialogManager`: a null or throwing dialog callback no longer closes the dialog as if
  the action succeeded. The confirm, cancel, and multi-action handlers wrapped the user callback
  in a bare `catch {}`, which swallowed every failure, including a guaranteed
  `NullReferenceException` when `OnAlert` / `OnConfirm` / `OnPrompt` / `OnAction` was null: the
  operation was lost with zero diagnostics. Callback failures now propagate to the caller while
  the dialog state still unwinds through `try/finally` (`IsLoading` clears and queued dialogs
  advance), so nothing latches on a failed callback (#87).
