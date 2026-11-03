namespace RForge.Abstractions;

/// <summary>
/// Represents a countdown timer that triggers events at regular intervals and upon completion.
/// </summary>
public class CountdownTimer : IDisposable
{
    /// <summary>
    /// The number of progress ticks a countdown aims for when the duration is long enough
    /// to request one tick per percent.
    /// </summary>
    private const int PercentTicks = 100;

    /// <summary>
    /// The smallest tick period an OS timer can reliably deliver. 15.625 ms is the default
    /// Windows timer quantum; requesting anything finer is silently rounded up by the OS,
    /// so the model must treat it as the floor instead of pretending sub-quantum ticks exist.
    /// </summary>
    private const double MinTickMilliseconds = 15.625;

    private readonly PeriodicTimer _timer;
    private readonly int _ticksToTimeout;
    private readonly CancellationToken _cancellationToken;
    private int _percentComplete;
    private Func<int, Task> _tickDelegate;
    private Action _elapsedDelegate;

    /// <summary>
    /// The current status of this countdown timer.
    /// </summary>
    public TimerStatus Status { get; private set; } = TimerStatus.Stopped;

    /// <summary>
    /// Initializes a new instance of the <see cref="CountdownTimer"/> class with the specified timeout and cancellation token.
    /// The tick model is derived from the duration: the timer aims for 100 ticks (one per percent),
    /// but the tick period never drops below the OS timer resolution, so shorter durations take
    /// proportionally fewer, honest ticks instead of silently drifting past the requested timeout.
    /// </summary>
    /// <param name="timeout">The timeout duration in milliseconds. Must be positive.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="timeout"/> is not positive.</exception>
    public CountdownTimer(int timeout, CancellationToken cancellationToken = default)
    {
        if (timeout <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), "The timeout must be a positive number of milliseconds.");
        }

        // The previous model pinned _ticksToTimeout to 100, so interval = timeout/100. Below
        // the OS timer quantum that interval does not exist, the OS rounds it up, and 100 of
        // the rounded-up ticks overshoot short durations by up to ~15x (and sub-100 ms
        // durations produced a sub-millisecond period the PeriodicTimer ctor rejects).
        // Clamp the tick to what a timer can deliver and derive the tick count from the
        // duration instead: elapsed then closes at the requested timeout within one tick.
        double tickMs = Math.Max(timeout / (double)PercentTicks, MinTickMilliseconds);
        _ticksToTimeout = (int)Math.Ceiling(timeout / tickMs);
        _timer = new PeriodicTimer(TimeSpan.FromMilliseconds(tickMs));
        _cancellationToken = cancellationToken;
    }

    /// <summary>
    /// Call to set a callback function that fires every tick with the percent complete (1-100).
    /// For durations too short for 100 OS timer ticks the percent advances in larger steps,
    /// but always tracks real elapsed time and ends at exactly 100.
    /// </summary>
    /// <param name="updateProgressDelegate">The function to fire every tick.</param>
    public CountdownTimer OnTick(Func<int, Task> updateProgressDelegate)
    {
        _tickDelegate = updateProgressDelegate;
        return this;
    }
    /// <summary>
    /// Call to set a callback function that fires when the timer runs to completion.
    /// </summary>
    /// <param name="elapsedDelegate">The function to fire when the timer completes.</param>
    public CountdownTimer OnElapsed(Action elapsedDelegate)
    {
        _elapsedDelegate = elapsedDelegate;
        return this;
    }

    /// <summary>
    /// Call to start / restart the timer. If the timer is already running, this call is ignored:
    /// a second start cannot spawn a concurrent countdown loop.
    /// </summary>
    public async Task StartAsync()
    {
        if (Status == TimerStatus.Running)
        {
            return;
        }

        _percentComplete = 0;
        Status = TimerStatus.Running;
        await DoWorkAsync();
    }


    private async Task DoWorkAsync()
    {
        int ticksElapsed = 0;
        while (await _timer.WaitForNextTickAsync(_cancellationToken) && _cancellationToken.IsCancellationRequested == false)
        {

            ticksElapsed++;

            // Report honest percent complete even when the duration is too short to afford
            // 100 ticks; the value still lands on exactly 100 for the final tick.
            _percentComplete = (int)Math.Min(100, (long)ticksElapsed * 100 / _ticksToTimeout);

            if (_tickDelegate != null)
            {
                await _tickDelegate(_percentComplete);
            }

            if (ticksElapsed >= _ticksToTimeout)
            {
                Status = TimerStatus.Completed;
                _elapsedDelegate?.Invoke();
                break;
            }
        }
    }

    /// <summary>
    /// Disposes the timer.
    /// </summary>
    public void Dispose()
    {
        _timer.Dispose();
    }
}
