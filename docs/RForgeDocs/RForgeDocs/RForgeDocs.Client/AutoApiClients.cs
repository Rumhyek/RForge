using RForge.Blazor.AutoApi;
using RForgeDocs.Abstractions.AutoApiDemo;

namespace RForgeDocs.Client;

[RForgeAutoApiClient(typeof(IDemoTodoService))]
public static partial class AutoApiClients { }
