using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using RForgeDocs.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

IocConfig.Register(builder.Configuration, builder.Services);

await builder.Build().RunAsync();
