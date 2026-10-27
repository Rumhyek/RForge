
# RForge.Blazor

## Unreleased

- Fixed `RfDropDown` / `RfDropDownMulti`: the `CssClass` parameter is now applied to the dropdown root element and `CssFilterClass` to the filter input. Both parameters existed but were silently ignored; existing consumers that set them will now see their styles apply (#66).


