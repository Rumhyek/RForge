using RForgeDocs;
using RForgeDocs.Abstractions.AutoApiDemo;
using RForgeDocs.AutoApiDemo;
using RForgeDocs.Components;
using RForgeDocs.EndPoints;
using RForge.Blazor;
using RForge.Blazor.Events;
using RForgeDocs.Client.EventsExamples;
using RForgeDocs.EventsExamples.Dashboard;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddRfForgeBlazorServices();

// One root event hub per circuit (and per request for static rendering and prerendering).
// The WebAssembly client registers its own hub with the same configuration.
builder.Services.AddRForgeEvents(EventsDemoHub.Configure);

// Server-only service that publishes from a timer thread (live dashboard example).
builder.Services.AddScoped<MarketFeed>();

IocConfig.Register(builder.Configuration, builder.Services);

// AutoApi demo: Interactive Server and prerendering call the service directly, WebAssembly calls the generated endpoints.
builder.Services.AddSingleton<DemoTodoStore>();
builder.Services.AddScoped<IDemoTodoService, DemoTodoService>();
builder.Services.AddRForgeAutoApiServer();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if(app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(RForgeDocs.Client._Imports).Assembly);

app.MapUserApi();
app.MapRForgeAutoApi();

app.Run();
