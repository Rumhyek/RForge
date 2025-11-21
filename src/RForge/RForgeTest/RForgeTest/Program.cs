using RForgeBlazor.Services;
using RForgeTest.Client.Pages;
using RForgeTest.Client.TestModels;
using RForgeTest.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddScoped<IGetFakeUsers, GetFakeUsers>();
builder.Services.AddScoped<INotificationManager, NotificationManager>();
builder.Services.AddScoped<IDialogManager, DialogManager>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

#if NET8_0
app.UseStaticFiles();
#endif
#if NET9_0_OR_GREATER
app.MapStaticAssets();
#endif

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(RForgeTest.Client._Imports).Assembly);

app.Run();
