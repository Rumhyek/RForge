using AutoApiSample.Shared;
using RForge.Blazor.AutoApi;

namespace AutoApiSample.Client;

[RForgeAutoApiClient(typeof(ITodoService))]
public static partial class AutoApiClients { }
