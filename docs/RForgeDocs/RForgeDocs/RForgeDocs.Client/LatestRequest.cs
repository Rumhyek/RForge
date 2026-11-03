namespace RForgeDocs.Client;

/// <summary>
/// Hands out a ticket per async request so a page only applies the response of the most recent
/// one. A slow response for an earlier filter would otherwise land after, and overwrite, the
/// result for the filter the user actually ended on.
/// </summary>
public sealed class LatestRequest
{
    private int _latest;

    /// <summary>Starts a request and returns its ticket; every earlier ticket is now stale.</summary>
    public int Begin() => Interlocked.Increment(ref _latest);

    /// <summary>True when no newer request has started since <paramref name="ticket"/> was issued.</summary>
    public bool IsLatest(int ticket) => Volatile.Read(ref _latest) == ticket;
}
