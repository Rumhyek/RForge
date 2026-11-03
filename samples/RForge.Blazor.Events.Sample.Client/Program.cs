using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using RForge.Blazor.Events.Sample.Client.Shared;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// The WebAssembly runtime has its own DI container, so it needs its own hub.
builder.Services.AddRForgeEvents(SampleEventHub.Configure);

await builder.Build().RunAsync();
