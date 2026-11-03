# RForge.Blazor

## Version 1.0.0

`RForge.Blazor` is now a bundle with no code of its own. Installing it installs `RForge.Blazor.Components`, `RForge.Blazor.Abstractions`, `RForge.Blazor.Events`, `RForge.Blazor.AutoApi.Abstractions` and `RForge.Blazor.AutoApi.Generator`, all at the same version.

Before 1.0.0 this package was the component library. The components moved to `RForge.Blazor.Components`; install that package if you only want the UI components. Replace the `RForgeBlazor` namespace with `RForge.Blazor` in your code (see that package's changelog for the full list).

The Events and AutoApi source generators come with the bundle as analyzers.
