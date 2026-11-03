namespace RForge.Blazor.Events;

internal static class TaskExtensions
{
    /// <summary>Fire-and-forget that still observes faults, so they never surface as unobserved task exceptions.</summary>
    public static void Observe(this Task task)
    {
        if (!task.IsCompleted)
        {
            _ = task.ContinueWith(
                static t => _ = t.Exception,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
        else if (task.IsFaulted)
        {
            _ = task.Exception;
        }
    }
}
