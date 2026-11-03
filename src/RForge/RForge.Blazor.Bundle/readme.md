# RForge.Blazor

**The whole RForge.Blazor family in one package.**

Installing `RForge.Blazor` installs every library below. If you only need part of it, install that package on its own instead.

| Package | What it is |
|---|---|
| [`RForge.Blazor.Components`](https://www.nuget.org/packages/RForge.Blazor.Components) | The light weight Bulma component library: drop downs, data grid, dialogs, notifications and more. |
| [`RForge.Blazor.Abstractions`](https://www.nuget.org/packages/RForge.Blazor.Abstractions) | Enums and contracts shared between a Blazor front end and its back end. The components depend on it. |
| [`RForge.Blazor.Events`](https://www.nuget.org/packages/RForge.Blazor.Events) | Typed events and reactive state for components anywhere in the tree. |
| [`RForge.Blazor.AutoApi.Abstractions`](https://www.nuget.org/packages/RForge.Blazor.AutoApi.Abstractions) and [`RForge.Blazor.AutoApi.Generator`](https://www.nuget.org/packages/RForge.Blazor.AutoApi.Generator) | Write a service interface once and call it from any Blazor render mode. A source generator creates the minimal API endpoints and the WebAssembly client. |

## Only want the UI components?

Install **`RForge.Blazor.Components`**. This bundle also brings the Events and AutoApi source generators into your build as analyzers. They do nothing unless your code uses their attributes or base types, but they do show up under the project's analyzers.

## Upgrading from 0.x

Before 1.0.0 this package *was* the component library. It is now the bundle, and the components moved to `RForge.Blazor.Components`.

1. Install `RForge.Blazor` (everything) or `RForge.Blazor.Components` (just the components).
2. Change the namespaces in your code and `_Imports.razor`:

   | Before | After |
   |---|---|
   | `RForgeBlazor`, `RForgeBlazor.Models`, `RForgeBlazor.Services` | `RForge.Blazor`, `RForge.Blazor.Models`, `RForge.Blazor.Services` |
   | `RForge.Abstractions` and its sub-namespaces | `RForge.Blazor.Abstractions` and the same sub-namespaces |

Version 0.x packages stay on NuGet unchanged.

See the [documentation site](https://rforge.azurewebsites.net) and the [repository](https://github.com/Rumhyek/RForge).
