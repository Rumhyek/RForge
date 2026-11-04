using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using RForgeDocs.Client;
using RForge.Blazor;
using RForge.Blazor.Events;
using RForgeDocs.Client.EventsExamples;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddHttpClient("api", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));

builder.Services.AddRfForgeBlazorServices();

// The WebAssembly runtime has its own DI container, so it needs its own event hub.
builder.Services.AddRForgeEvents(EventsDemoHub.Configure);

IocConfig.Register(builder.Configuration, builder.Services);

await builder.Build().RunAsync();
