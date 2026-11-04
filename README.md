
# RForge

A light weight Blazor library using Bulma. More details at [RForge](https://rforge.azurewebsites.net/).

## Light Weight and Memory Efficient
RForge will focus on key components and avoid over-bloating the library with small, simple ones. This approach allows the CSS framework Bulma to excel at what it does best. By leveraging Bulma effectively, RForge ensures low load times and minimal memory usage.

Additionally, RForge will strive to eliminate any JavaScript dependencies, helping to keep the download file size to a minimum. The primary goal is to minimize client-side downloads, particularly when used in WASM environments, where the client would otherwise be burdened with both WASM and JavaScript files. The one exception is hot keys, because a page-wide key listener isn't possible without JavaScript. `RfHotKey` and the components that use it (open dropdowns and visible modals) load one small file on first use, and those components can opt out with `UseHotKeys="false"`. No other component uses JavaScript.

Consistent with these principles, RForge will keep its package CSS minimal, concentrating on the utilization of Bulma. This strategy not only keeps RForge's CSS download size compact but also reduces the complexity and memory requirements of the Blazor components themselves.

## True UI Components
One of RForge's primary objectives is to keep the components focused purely on UI, leaving any form of business logic to the developer. This approach attempts to remove any bias towards how an application should be coded, allowing developers using RForge the freedom to implement their way.

At the same time, RForge's internal code remains lean, reducing feature bloat, file size, and memory usage. This design philosophy grants developers more flexibility in their choices for implementing features.

However, RForge has no plans to abstract the base CSS, adding an additional layer of abstraction that developers would need to understand. Instead, we fully leverage Bulma CSS, avoiding simple component implementations. This simplifies the development process for developers while maximizing overall memory efficiency and load speed by reducing the number of components.

# How to Setup
Setting up RForge is simple. Follow the below steps to add RForge Blazor components to your project.

1. Install the NuGet package
1. Import the namespaces (optional)
1. Register RForge services
1. Include Bulma CSS

### Install the NuGet Package
RForge is a family of packages that are all released at the same version.

| Package | Install it when |
| --- | --- |
| `RForge.Blazor` | You want everything: the components, typed events and state, and the auto API generator. |
| `RForge.Blazor.Components` | You only want the UI components. |
| `RForge.Blazor.Abstractions` | A shared class library uses the enums and contracts that the components also use. The components already depend on it. |
| `RForge.Blazor.Events` | You only want typed events and reactive state. |
| `RForge.Blazor.AutoApi.Abstractions` and `RForge.Blazor.AutoApi.Generator` | You only want the auto API generator. |

`RForge.Blazor` brings the Events and AutoApi source generators into your build as analyzers. They do nothing unless your code uses their attributes or base types, but if you only want the UI components, install `RForge.Blazor.Components` instead.

##### Command Line
```dotnet add package RForge.Blazor.Components```

##### Package Manager Console
```Install-Package RForge.Blazor.Components```

Use `RForge.Blazor` in place of `RForge.Blazor.Components` to install everything.

### Upgrading from 0.x
Before 1.0.0, `RForge.Blazor` was the component library. It is now the bundle, and the components are in `RForge.Blazor.Components`. Install one of them, then update the namespaces in your code and in `_Imports.razor`:

| Before | After |
| --- | --- |
| `RForgeBlazor`, `RForgeBlazor.Models`, `RForgeBlazor.Services` | `RForge.Blazor`, `RForge.Blazor.Models`, `RForge.Blazor.Services` |
| `RForge.Abstractions` and its sub-namespaces | `RForge.Blazor.Abstractions` and the same sub-namespaces |

The 0.x packages stay on NuGet unchanged.

### Import the namespaces
To simplify namespacing while using RForge, the package is designed with most all components sitting within the root RForge namespace.

You may want to include the following namespaces in your _import.razor for ease of use.

```csharp
@using RForge.Blazor
@using RForge.Blazor.Services
@using RForge.Blazor.Abstractions
```

| Namespace | Purpose
| --- | ---
| `RForge.Blazor` | Houses all of the blazor components.
| `RForge.Blazor.Services` | Houses interfaces you may need to communicate with certain components.
| `RForge.Blazor.Abstractions` | Common enums used through out the components.

### Register RForge Services

In order for some components to work they must first have their services registered.

Open up `Program.cs` and add the following line before the var `app = builder.Build();`.

```csharp
using RForge.Blazor;
//...
builder.Services.AddRfForgeBlazorServices();
//...
var app = builder.Build();
```

### Include Bulma CSS
RForge makes use of Bulma CSS to stylize the components. Add / install Bulma and add the CSS file to the head. Link to download: [Bulma releases](https://github.com/jgthms/bulma/releases/).