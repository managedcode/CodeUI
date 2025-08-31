using System.Reactive.Subjects;
using CodeUI.Core.Models;

namespace CodeUI.Core.Services;

/// <summary>
/// Service for executing and managing CLI processes with real-time output streaming.
/// </summary>
public interface ICliExecutor : IDisposable
{
    /// <summary>
    /// Observable stream of output lines from running processes.
    /// </summary>
    IObservable<OutputLine> Output { get; }
    
    /// <summary>
    /// Gets information about the currently running process, if any.
    /// </summary>
    ProcessInfo? CurrentProcess { get; }
    
    /// <summary>
    /// Starts a CLI process with the specified command and arguments.
    /// </summary>
    /// <param name="command">The command to execute (e.g., "git", "claude-code", "gemini").</param>
    /// <param name="arguments">The arguments to pass to the command.</param>
    /// <param name="workingDirectory">The working directory for the process. Defaults to current directory.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that completes when the process starts, containing the process information.</returns>
    Task<ProcessInfo> StartProcessAsync(string command, string arguments, string? workingDirectory = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Starts a CLI process and waits for it to complete.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="arguments">The arguments to pass to the command.</param>
    /// <param name="workingDirectory">The working directory for the process. Defaults to current directory.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that completes when the process ends, containing the final process information.</returns>
    Task<ProcessInfo> ExecuteAsync(string command, string arguments, string? workingDirectory = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stops the currently running process, if any.
    /// </summary>
    /// <param name="graceful">Whether to attempt graceful shutdown before force-killing.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that completes when the process has been stopped.</returns>
    Task StopProcessAsync(bool graceful = true, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends input to the currently running process.
    /// </summary>
    /// <param name="input">The input to send to the process stdin.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task that completes when the input has been sent.</returns>
    Task SendInputAsync(string input, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a command is available on the system.
    /// </summary>
    /// <param name="command">The command to check for availability.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if the command is available, false otherwise.</returns>
    Task<bool> IsCommandAvailableAsync(string command, CancellationToken cancellationToken = default);
}