using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using RForgeBlazor.Models;
using RForgeTest.Client.TestModels;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped<IGetFakeUsers, GetFakeUsers>();
builder.Services.AddScoped<INotificationManager, NotificationManager>();

await builder.Build().RunAsync();
