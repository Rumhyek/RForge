using System.Collections.Immutable;

namespace RForge.Blazor.Events.Sample.Services;

/// <summary>
/// A Scoped (per-circuit) service that publishes from a thread-pool timer, i.e. off the renderer's thread.
/// Components still update safely because the hub runs component handlers through InvokeAsync.
/// </summary>
public sealed class MarketFeed(IRfEventHub hub) : IDisposable
{
    public static readonly IReadOnlyList<string> Symbols = ["ACME", "GLOBX", "INITECH", "UMBRL", "WAYNE", "STARK"];

    private readonly Random _random = new();
    private readonly Lock _gate = new();
    private Timer? _timer;
    private int _ticking;

    public void Start(TimeSpan interval)
    {
        lock (_gate)
        {
            if (_timer is not null)
            {
                return;
            }

            var market = hub.State<MarketState>();
            if (market.Value.Prices.IsEmpty)
            {
                market.Update(s => s with
                {
                    Prices = Symbols.ToImmutableDictionary(static s => s, _ => Math.Round(50m + (decimal)_random.NextDouble() * 150m, 2)),
                });
            }

            market.Update(s => s with { Running = true });
            _timer = new Timer(_ => _ = TickAsync(), null, interval, interval);
        }
    }

    public void Stop()
    {
        lock (_gate)
        {
            _timer?.Dispose();
            _timer = null;
        }

        hub.State<MarketState>().Update(s => s with { Running = false });
    }

    private async Task TickAsync()
    {
        // Skip a tick if the previous one is still being delivered.
        if (Interlocked.Exchange(ref _ticking, 1) == 1)
        {
            return;
        }

        try
        {
            var symbol = Symbols[_random.Next(Symbols.Count)];
            var market = hub.State<MarketState>();
            var old = market.Value.Prices.GetValueOrDefault(symbol, 100m);
            var change = Math.Round(old * (decimal)((_random.NextDouble() - 0.5) * 0.06), 2);
            var price = Math.Max(1m, old + change);

            market.Update(s => s with { Prices = s.Prices.SetItem(symbol, price), Ticks = s.Ticks + 1 });
            await hub.PublishAsync(new PriceTick(symbol, price, change, DateTime.Now));
        }
        catch (ObjectDisposedException)
        {
            // The circuit ended between ticks.
        }
        finally
        {
            Volatile.Write(ref _ticking, 0);
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _timer?.Dispose();
            _timer = null;
        }
    }
}
