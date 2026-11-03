using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using RForge.Blazor.Bus.Sample.Client.Shared;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// The WebAssembly runtime has its own DI container, so it needs its own bus.
builder.Services.AddBlazorBus(SampleBus.Configure);

await builder.Build().RunAsync();
