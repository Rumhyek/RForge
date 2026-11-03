using RForge.Blazor.Events.Sample.Client.Shared;
using RForge.Blazor.Events.Sample.Components;
using RForge.Blazor.Events.Sample.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// One root hub per circuit (and per request for static rendering / prerendering).
// The WebAssembly client registers its own hub with the same configuration in its Program.cs.
builder.Services.AddRForgeEvents(SampleEventHub.Configure);

// Server-only service that publishes from a timer thread (live dashboard example).
builder.Services.AddScoped<MarketFeed>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(RForge.Blazor.Events.Sample.Client._Imports).Assembly);

app.Run();
