namespace CodeUI.Core.Models;

/// <summary>
/// Represents the state of a CLI process.
/// </summary>
public enum ProcessState
{
    /// <summary>
    /// Process has not been started yet.
    /// </summary>
    NotStarted,
    
    /// <summary>
    /// Process is currently running.
    /// </summary>
    Running,
    
    /// <summary>
    /// Process has completed successfully.
    /// </summary>
    Completed,
    
    /// <summary>
    /// Process has failed or was terminated.
    /// </summary>
    Failed
}

/// <summary>
/// Represents signals that can be sent to a process.
/// </summary>
public enum ProcessSignal
{
    /// <summary>
    /// SIGINT - Interrupt signal (Ctrl+C).
    /// </summary>
    Interrupt = 2,
    
    /// <summary>
    /// SIGTERM - Termination signal.
    /// </summary>
    Terminate = 15,
    
    /// <summary>
    /// SIGKILL - Kill signal (cannot be caught or ignored).
    /// </summary>
    Kill = 9,
    
    /// <summary>
    /// SIGQUIT - Quit signal (Ctrl+\).
    /// </summary>
    Quit = 3,
    
    /// <summary>
    /// SIGSTOP - Stop signal (Ctrl+Z).
    /// </summary>
    Stop = 19,
    
    /// <summary>
    /// SIGCONT - Continue signal.
    /// </summary>
    Continue = 18
}

/// <summary>
/// Represents a line of output from a CLI process.
/// </summary>
public record OutputLine
{
    /// <summary>
    /// The raw text content of the output line.
    /// </summary>
    public required string Text { get; init; }
    
    /// <summary>
    /// Whether this line came from stdout (true) or stderr (false).
    /// </summary>
    public required bool IsStdOut { get; init; }
    
    /// <summary>
    /// The timestamp when this line was received.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Information about a CLI process execution.
/// </summary>
public record ProcessInfo
{
    /// <summary>
    /// The process ID.
    /// </summary>
    public int ProcessId { get; init; }
    
    /// <summary>
    /// The current state of the process.
    /// </summary>
    public ProcessState State { get; init; }
    
    /// <summary>
    /// The command being executed.
    /// </summary>
    public required string Command { get; init; }
    
    /// <summary>
    /// The arguments passed to the command.
    /// </summary>
    public required string Arguments { get; init; }
    
    /// <summary>
    /// The working directory for the process.
    /// </summary>
    public required string WorkingDirectory { get; init; }
    
    /// <summary>
    /// When the process was started.
    /// </summary>
    public DateTime StartTime { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the process ended (if completed).
    /// </summary>
    public DateTime? EndTime { get; init; }
    
    /// <summary>
    /// The exit code of the process (if completed).
    /// </summary>
    public int? ExitCode { get; init; }
}