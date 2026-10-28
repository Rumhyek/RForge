# RForge.Blazor

## Unreleased

- Fixed `RfDropDown` / `RfDropDownMulti`: the filter pump no longer misbehaves across close,
  reopen and dispose. A filter `OnLoad` that threw used to latch the `is-loading` spinner on
  forever (the pump set `IsLoading` without a `try/finally`) and the exception was silently
  lost. Close, reopen and dispose now cancel the in-flight pump instead of orphaning it:
  consumer `FilterChanged`/`OnLoad` callbacks no longer fire after the dropdown is closed or
  the component is disposed, two pumps can no longer race the same filter queue into
  duplicate or out-of-order loads, and a keystroke typed during an in-flight load is loaded
  exactly once, after that load completes (#71).
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
- Fixed `RfDropDown` / `RfDropDownMulti`: the `CssClass` parameter is now applied to the dropdown root element and `CssFilterClass` to the filter input. Both parameters existed but were silently ignored; existing consumers that set them will now see their styles apply (#66).
- CI: merges to `main` no longer push packages to nuget.org. Each merge still builds and
  publishes packages to the GitHub package feed and creates a draft release; packages reach
  nuget.org only when a maintainer publishes a release (#116).
