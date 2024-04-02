using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using RForgeTest.Client.TestModels;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped<IGetFakeUsers, GetFakeUsers>();

await builder.Build().RunAsync();
