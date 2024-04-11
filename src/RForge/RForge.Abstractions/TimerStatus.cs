namespace RForge.Abstractions;

/// <summary>
/// Used to manage the state of <see cref="CountdownTimer" />
/// </summary>
public enum TimerStatus
{
    /// <summary>
    /// Stopped / has not be ran yet.
    /// </summary>
    Stopped,
    /// <summary>
    /// The timer is currently running.
    /// </summary>
    Running,
    /// <summary>
    /// The timer has been started and ran to completion.
    /// </summary>
    Completed
}
