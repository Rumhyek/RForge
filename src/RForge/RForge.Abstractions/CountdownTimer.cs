namespace RForge.Abstractions;

public class CountdownTimer : IDisposable
{
    private PeriodicTimer _timer;
    private readonly int _ticksToTimeout;
    private readonly CancellationToken _cancellationToken;
    private int _percentComplete;
    private Func<int, Task> _tickDelegate;
    private Action _elapsedDelegate;
    public TimerStatus Status { get; private set; } = TimerStatus.Stopped;
    public CountdownTimer(int timeout, CancellationToken cancellationToken = default)
    {
        _ticksToTimeout = 100;
        _timer = new PeriodicTimer(TimeSpan.FromMilliseconds(timeout / (double)_ticksToTimeout));
        _cancellationToken = cancellationToken;
    }
    public CountdownTimer OnTick(Func<int, Task> updateProgressDelegate)
    {
        _tickDelegate = updateProgressDelegate;
        return this;
    }
    public CountdownTimer OnElapsed(Action elapsedDelegate)
    {
        Status = TimerStatus.Completed;
        _elapsedDelegate = elapsedDelegate;
        return this;
    }
    public async Task StartAsync()
    {
        _percentComplete = 0;
        Status = TimerStatus.Running;
        await DoWorkAsync();
    }


    private async Task DoWorkAsync()
    {
        while (await _timer.WaitForNextTickAsync(_cancellationToken) && !_cancellationToken.IsCancellationRequested)
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
    public void Dispose() => _timer.Dispose();
}
