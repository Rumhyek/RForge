
# RForge.Abstractions

An abstraction layer to support common objects between the front end and back end.


## Unreleased

- Fixed `CountdownTimer`: the tick model is derived from the duration instead of a fixed 100 ticks. Previously the interval was always `timeout / 100`, which falls below the OS timer resolution for durations under ~1.6 s: the OS rounded each tick up while the timer still waited for all 100, so short countdowns overshot the requested duration by up to ~15x (`ShowFor=100` closed after ~1-2 s) and durations under 100 ms threw `ArgumentOutOfRangeException` from the constructor (`RfNotificationManager` callers with `ShowFor < 100` crashed into `Add*`). Consumer-visible: short durations now close within one tick of the requested time, and `OnTick` percent values advance in larger steps for durations too short for 100 OS timer ticks (final value is still exactly 100); a non-positive `timeout` now throws `ArgumentOutOfRangeException(argument: 'timeout')` naming the real problem (#84).


## Version 0.1.0

The initial creation. Includes features for:

- **DataGrids** - `DataGridFilterBy.cs` and `DataGridSortBy.cs`. Allow for backend data models to support sorting and filtering the `DataGrid`.
- **DropDowns** - `RfDropDownPosition.cs` and `RfShowSelectionInDropDown.cs`. Allows backend data models to support position management.
- **Modal** - IDalog