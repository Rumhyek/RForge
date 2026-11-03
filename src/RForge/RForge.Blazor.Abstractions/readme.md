# RForge.Blazor.Abstractions

Enums and contracts shared between a Blazor front end and its back end: sort orders, drop down positions, and the dialog and notification options and backend interfaces used by the [RForge.Blazor.Components](https://www.nuget.org/packages/RForge.Blazor.Components) library.

Put this package in a shared class library to use those types from server code without referencing the components. The components already depend on it, so you only need to install it when a project outside the UI uses these types.

```
dotnet add package RForge.Blazor.Abstractions
```

The namespaces are `RForge.Blazor.Abstractions` and its `DataGrids`, `DropDowns`, `Modal` and `Notifications` sub-namespaces. Before 1.0.0 they were `RForge.Abstractions`.

See the [documentation site](https://rforge.azurewebsites.net).
