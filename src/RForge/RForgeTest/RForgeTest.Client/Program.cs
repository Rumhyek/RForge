using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using RForge.Blazor.Services;
using RForgeTest.Client.TestModels;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped<IGetFakeUsers, GetFakeUsers>();
builder.Services.AddScoped<INotificationManager, NotificationManager>();
builder.Services.AddScoped<IDialogManager, DialogManager>();

await builder.Build().RunAsync();
