using Microsoft.AspNetCore.Components;

namespace RForge.Blazor.Bus.Tests.Components;

/// <summary>Logs "{Id}:{value}" for each Ping received on its nearest scope.</summary>
public partial class PingListener
{
    private int _received;

    [Inject]
    private EventLog Log { get; set; } = default!;

    [Parameter]
    public string Id { get; set; } = "listener";

    [Subscribe]
    private void OnPing(Ping ping)
    {
        _received++;
        Log.Add($"{Id}:{ping.Value}");
    }
}
