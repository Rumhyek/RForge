namespace RForge.Abstractions;

public class CountdownTimer : IDisposable
{
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

    public CountdownTimer(int timeout, CancellationToken cancellationToken = default)
    {
        _ticksToTimeout = 100;
        _timer = new PeriodicTimer(TimeSpan.FromMilliseconds(timeout / (double)_ticksToTimeout));
        _cancellationToken = cancellationToken;
    }

    /// <summary>
    /// Call to set a callback function that fires every 1 percent of the timer.
    /// </summary>
    /// <param name="updateProgressDelegate">The function to fire ever tick.</param>
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
        Status = TimerStatus.Completed;
        _elapsedDelegate = elapsedDelegate;
        return this;
    }

    /// <summary>
    /// Call to start / restart the timer.
    /// </summary>
    public async Task StartAsync()
    {
        _percentComplete = 0;
        Status = TimerStatus.Running;
        await DoWorkAsync();
    }


    private async Task DoWorkAsync()
    {
        while (await _timer.WaitForNextTickAsync(_cancellationToken) && _cancellationToken.IsCancellationRequested == false)
        {

            _percentComplete++;

            if (_tickDelegate != null)
            {
                await _tickDelegate(_percentComplete);
            }

            if (_percentComplete >= _ticksToTimeout)
            {
                _elapsedDelegate?.Invoke();
                break;
            }
        }
    }
    public void Dispose()
    {
        _timer.Dispose();
    }
}
