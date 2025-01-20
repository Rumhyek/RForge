
# RForge

A light weight Blazor library using Bulma. More details at [RForge](https://rforge.azurewebsites.net/).

## Light Weight and Memory Efficient
RForge will focus on key components and avoid over-bloating the library with small, simple ones. This approach allows the CSS framework Bulma to excel at what it does best. By leveraging Bulma effectively, RForge ensures low load times and minimal memory usage.

Additionally, RForge will strive to eliminate any JavaScript dependencies, helping to keep the download file size to a minimum. The primary goal is to minimize client-side downloads, particularly when used in WASM environments, where the client would otherwise be burdened with both WASM and JavaScript files.

Consistent with these principles, RForge will keep its package CSS minimal, concentrating on the utilization of Bulma. This strategy not only keeps RForge's CSS download size compact but also reduces the complexity and memory requirements of the Blazor components themselves.

## True UI Components
One of RForge's primary objectives is to keep the components focused purely on UI, leaving any form of business logic to the developer. This approach attempts to remove any bias towards how an application should be coded, allowing developers using RForge the freedom to implement their way.

At the same time, RForge's internal code remains lean, reducing feature bloat, file size, and memory usage. This design philosophy grants developers more flexibility in their choices for implementing features.

However, RForge has no plans to abstract the base CSS, adding an additional layer of abstraction that developers would need to understand. Instead, we fully leverage Bulma CSS, avoiding simple component implementations. This simplifies the development process for developers while maximizing overall memory efficiency and load speed by reducing the number of components.

# How to Setup
Setting up RForge is simple. Follow the below steps to add RForge Blazor components to your project.

1. Install the Nuget package
1. Import the namespaces (optional)
1. Register RForge services
1. Include Bulma CSS

### Install the Nuget Package
RForge Blazor components are installed via Nuget. 
There are two packages. One for the blazor app and one for the library if any.

**Please note**: currently you must setup packages with github.

#### Blazor Project Installation
##### Command Line
```dotnet add package RForge.Blazor```

##### Package Manager Console
```Install-Package RForge.Blazor```

#### Library Project Installation
This is optional but may be useful if you want to use common enums across the backend and frontend.

##### Command Line
```dotnet add package RForge.Abstractions```
##### Package Manager Console
```Install-Package RForge.Abstractions```

### Import the namespaces
To simplify namespacing while using RForge, the package is designed with most all components sitting within the root RForge namespace.

You may want to include the following namespaces in your _import.razor for ease of use.

```csharp
@using RForgeBlazor
@using RForgeBlazor.Services
@using RForge.Abstractions
```

| Namespace | Purpose
| --- | ---
| `RForgeBlaozr` | Houses all of the blazor components.
| `RForgeBlaozr.Services`	| Houses interfaces you may need to communicate with certain components.
| `RForgeBlaozr.Abstractions` |	Common enums used through out the components.

### Register RForge Services

In order for some components to work they must first have their services registered.

Open up `Program.cs` and add the following line before the var `app = builder.Build();`.

```csharp
using RForgeBlazor;
//...
builder.Services.AddRfForgeBlazorServices();
//...
var app = builder.Build();
```

### Include Bulma CSS
RForge makes use of Bulma CSS to stylize the components. Add / install Bulma and add the CSS file to the head. Link to download: [Bulma releases](https://github.com/jgthms/bulma/releases/).