# RForge.Blazor.Components

A light weight Blazor component library built on [Bulma CSS](https://bulma.io). The components are UI only: no business logic and a minimal CSS footprint on top of Bulma.

Almost all of it runs without JavaScript. `RfHotKey` loads one small file on first use, and open dropdowns and visible modals use it for Esc. Set `UseHotKeys="false"` on those components to opt out.

Drop downs, a data grid, tabs, a tree view, dialogs, notifications, a context menu, hot keys, a markdown viewer and more. See every component with live examples on the [documentation site](https://rforge.azurewebsites.net).

## Install

```
dotnet add package RForge.Blazor.Components
```

Install [`RForge.Blazor`](https://www.nuget.org/packages/RForge.Blazor) instead if you also want the typed events and the auto API generator.

## Set up

1. **Import the namespaces** (optional) in `_Imports.razor`:

   ```razor
   @using RForge.Blazor
   @using RForge.Blazor.Services
   @using RForge.Blazor.Abstractions
   ```

2. **Register the services** in `Program.cs`, before `builder.Build()`. The dialog and notification managers need them:

   ```csharp
   using RForge.Blazor;

   builder.Services.AddRfForgeBlazorServices();
   ```

3. **Include Bulma CSS** in the `<head>`. Download it from the [Bulma releases](https://github.com/jgthms/bulma/releases/).

## Upgrading from 0.x

Before 1.0.0 the components were in the package named `RForge.Blazor`, with the namespace `RForgeBlazor`. Install `RForge.Blazor.Components` and replace `RForgeBlazor` with `RForge.Blazor` in your `using` and `@using` lines. The full list of changes is in the [changelog](https://github.com/Rumhyek/RForge/blob/main/src/RForge/RForge.Blazor.Components/CHANGELOG.md).
