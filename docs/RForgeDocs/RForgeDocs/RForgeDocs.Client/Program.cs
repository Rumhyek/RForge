using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using RForgeDocs.Client;
using RForgeBlazor;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddHttpClient("api", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));

builder.Services.AddRfForgeBlazorServices();

IocConfig.Register(builder.Configuration, builder.Services);

await builder.Build().RunAsync();
