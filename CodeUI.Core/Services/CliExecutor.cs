using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using CliWrap;
using CliWrap.EventStream;
using CodeUI.Core.Models;
using System.IO.Pipes;

namespace CodeUI.Core.Services;

/// <summary>
/// Implementation of CLI process executor using CliWrap with real-time output streaming.
/// </summary>
public partial class CliExecutor : ICliExecutor
{
    private readonly Subject<OutputLine> _outputSubject = new();
    private readonly SemaphoreSlim _executionSemaphore = new(1, 1);
    private Command? _currentCommand;
    private CancellationTokenSource? _currentCancellationSource;
    private volatile ProcessInfo? _currentProcess;
    private AnonymousPipeServerStream? _stdinPipeServer;
    private StreamWriter? _stdinWriter;
    private bool _disposed;

    /// <summary>
    /// Regular expression for matching ANSI escape sequences.
    /// </summary>
    [GeneratedRegex(@"\x1B\[[0-?]*[ -/]*[@-~]", RegexOptions.Compiled)]
    private static partial Regex AnsiEscapeRegex();

    /// <inheritdoc />
    public IObservable<OutputLine> Output => _outputSubject.AsObservable();

    /// <inheritdoc />
    public ProcessInfo? CurrentProcess => _currentProcess;

    /// <inheritdoc />
    public async Task<ProcessInfo> StartProcessAsync(string command, string arguments, string? workingDirectory = null, CancellationToken cancellationToken = default)
    {
        return await StartProcessInternalAsync(command, arguments, workingDirectory, enableInteractiveInput: false, cancellationToken);
    }

    /// <summary>
    /// Starts an interactive CLI process that can receive stdin input.
    /// </summary>
    public async Task<ProcessInfo> StartInteractiveProcessAsync(string command, string arguments, string? workingDirectory = null, CancellationToken cancellationToken = default)
    {
        return await StartProcessInternalAsync(command, arguments, workingDirectory, enableInteractiveInput: true, cancellationToken);
    }

    private async Task<ProcessInfo> StartProcessInternalAsync(string command, string arguments, string? workingDirectory, bool enableInteractiveInput, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(CliExecutor));
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        
        await _executionSemaphore.WaitAsync(cancellationToken);
        
        try
        {
            // Stop any currently running process
            if (_currentProcess is { State: ProcessState.Running })
            {
                await StopProcessInternalAsync(graceful: true, CancellationToken.None);
            }

            _currentCancellationSource?.Dispose();
            _currentCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            workingDirectory ??= Directory.GetCurrentDirectory();

            var commandBuilder = Cli.Wrap(command)
                .WithArguments(arguments)
                .WithWorkingDirectory(workingDirectory)
                .WithValidation(CommandResultValidation.None);

            // Only set up interactive input if requested
            if (enableInteractiveInput)
            {
                // Create anonymous pipe for stdin
                _stdinPipeServer = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
                var stdinPipeClient = new AnonymousPipeClientStream(PipeDirection.In, _stdinPipeServer.GetClientHandleAsString());
                _stdinWriter = new StreamWriter(_stdinPipeServer) { AutoFlush = true };

                commandBuilder = commandBuilder.WithStandardInputPipe(PipeSource.FromStream(stdinPipeClient));
            }

            _currentCommand = commandBuilder;

            var processInfo = new ProcessInfo
            {
                ProcessId = 0, // Will be updated when process starts
                State = ProcessState.Running,
                Command = command,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                StartTime = DateTime.UtcNow
            };

            _currentProcess = processInfo;

            // Start the process in the background
            _ = Task.Run(async () =>
            {
                try
                {
                    await foreach (var cmdEvent in _currentCommand.ListenAsync(_currentCancellationSource.Token))
                    {
                        switch (cmdEvent)
                        {
                            case StartedCommandEvent started:
                                _currentProcess = _currentProcess with { ProcessId = started.ProcessId };
                                break;

                            case StandardOutputCommandEvent stdOut:
                                var stdOutLine = new OutputLine
                                {
                                    Text = ProcessAnsiCodes(stdOut.Text),
                                    IsStdOut = true
                                };
                                _outputSubject.OnNext(stdOutLine);
                                break;

                            case StandardErrorCommandEvent stdErr:
                                var stdErrLine = new OutputLine
                                {
                                    Text = ProcessAnsiCodes(stdErr.Text),
                                    IsStdOut = false
                                };
                                _outputSubject.OnNext(stdErrLine);
                                break;

                            case ExitedCommandEvent exited:
                                var finalState = exited.ExitCode == 0 ? ProcessState.Completed : ProcessState.Failed;
                                _currentProcess = _currentProcess with
                                {
                                    State = finalState,
                                    EndTime = DateTime.UtcNow,
                                    ExitCode = exited.ExitCode
                                };
                                break;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    _currentProcess = _currentProcess with
                    {
                        State = ProcessState.Failed,
                        EndTime = DateTime.UtcNow,
                        ExitCode = -1
                    };
                }
                catch (Exception ex)
                {
                    _currentProcess = _currentProcess with
                    {
                        State = ProcessState.Failed,
                        EndTime = DateTime.UtcNow,
                        ExitCode = -1
                    };
                    
                    var errorLine = new OutputLine
                    {
                        Text = $"Process execution error: {ex.Message}",
                        IsStdOut = false
                    };
                    _outputSubject.OnNext(errorLine);
                }
            }, _currentCancellationSource.Token);

            return processInfo;
        }
        finally
        {
            _executionSemaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<ProcessInfo> ExecuteAsync(string command, string arguments, string? workingDirectory = null, CancellationToken cancellationToken = default)
    {
        var processInfo = await StartProcessAsync(command, arguments, workingDirectory, cancellationToken);
        
        // Wait for the process to complete
        while (_currentProcess is { State: ProcessState.Running } && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(100, cancellationToken);
        }
        
        return _currentProcess ?? processInfo;
    }

    /// <inheritdoc />
    public async Task StopProcessAsync(bool graceful = true, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(CliExecutor));
        
        await _executionSemaphore.WaitAsync(cancellationToken);
        
        try
        {
            await StopProcessInternalAsync(graceful, cancellationToken);
        }
        finally
        {
            _executionSemaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task SendInputAsync(string input, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(CliExecutor));
        ArgumentNullException.ThrowIfNull(input);
        
        if (_currentCommand == null || _currentProcess?.State != ProcessState.Running)
        {
            throw new InvalidOperationException("No process is currently running to send input to.");
        }

        if (_stdinWriter == null)
        {
            throw new InvalidOperationException("Stdin stream is not available for the current process.");
        }

        try
        {
            await _stdinWriter.WriteAsync(input);
            await _stdinWriter.FlushAsync();
        }
        catch (Exception ex)
        {
            var errorLine = new OutputLine
            {
                Text = $"Error sending input to process: {ex.Message}\r\n",
                IsStdOut = false
            };
            _outputSubject.OnNext(errorLine);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsCommandAvailableAsync(string command, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(CliExecutor));
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        
        try
        {
            // Try using 'command -v' first (more portable)
            var result = await Cli.Wrap("/bin/sh")
                .WithArguments($"-c \"command -v {command}\"")
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync(cancellationToken);
            
            if (result.ExitCode == 0)
                return true;
        }
        catch
        {
            // Continue to next check
        }
        
        try
        {
            // Try 'which' command
            var result = await Cli.Wrap("which")
                .WithArguments(command)
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync(cancellationToken);
            
            if (result.ExitCode == 0)
                return true;
        }
        catch
        {
            // Continue to next check
        }
        
        try
        {
            // Fallback for Windows
            var result = await Cli.Wrap("where")
                .WithArguments(command)
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync(cancellationToken);
            
            return result.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Processes ANSI color codes and escape sequences from command output.
    /// </summary>
    /// <param name="text">The text containing potential ANSI codes.</param>
    /// <returns>The text with ANSI codes preserved (for now) or stripped if needed.</returns>
    private static string ProcessAnsiCodes(string text)
    {
        // For now, we preserve ANSI codes as they might be useful for colorized output
        // In the future, this could be enhanced to convert to HTML colors or strip completely
        return text;
    }

    /// <summary>
    /// Internal method to stop the current process without acquiring the semaphore.
    /// </summary>
    private async Task StopProcessInternalAsync(bool graceful, CancellationToken cancellationToken)
    {
        if (_currentCancellationSource != null && !_currentCancellationSource.Token.IsCancellationRequested)
        {
            if (graceful)
            {
                _currentCancellationSource.Cancel();
            }
            else
            {
                _currentCancellationSource.Cancel();
            }
            
            // Wait a bit for graceful shutdown
            if (graceful && _currentProcess?.State == ProcessState.Running)
            {
                await Task.Delay(2000, cancellationToken);
            }
        }
        
        // Close stdin to signal process termination
        if (_stdinWriter != null)
        {
            try
            {
                await _stdinWriter.DisposeAsync();
                _stdinWriter = null;
            }
            catch
            {
                // Ignore errors during cleanup
            }
        }
        
        if (_stdinPipeServer != null)
        {
            try
            {
                _stdinPipeServer.Dispose();
                _stdinPipeServer = null;
            }
            catch
            {
                // Ignore errors during cleanup
            }
        }
        
        if (_currentProcess?.State == ProcessState.Running)
        {
            _currentProcess = _currentProcess with
            {
                State = ProcessState.Failed,
                EndTime = DateTime.UtcNow,
                ExitCode = -1
            };
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        
        // Stop any running process
        try
        {
            StopProcessInternalAsync(graceful: false, CancellationToken.None).GetAwaiter().GetResult();
        }
        catch
        {
            // Ignore exceptions during disposal
        }
        
        _stdinWriter?.Dispose();
        _stdinPipeServer?.Dispose();
        _currentCancellationSource?.Dispose();
        _outputSubject.Dispose();
        _executionSemaphore.Dispose();
    }
}