using AutoApiSample.Client;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Generated HttpClient implementations of the shared interfaces.
builder.Services.AddRForgeAutoApi();

await builder.Build().RunAsync();
