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
    private AnonymousPipeClientStream? _stdinPipeClient;
    private StreamWriter? _stdinWriter;
    
    // PTY-related fields (simplified implementation for now)
    private bool _isPtyProcess;
    private (int Columns, int Rows)? _terminalSize;
    
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
                _stdinPipeClient = new AnonymousPipeClientStream(PipeDirection.In, _stdinPipeServer.GetClientHandleAsString());
                _stdinWriter = new StreamWriter(_stdinPipeServer) { AutoFlush = true };

                commandBuilder = commandBuilder.WithStandardInputPipe(PipeSource.FromStream(_stdinPipeClient));
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
        
        if (_currentProcess?.State != ProcessState.Running)
        {
            throw new InvalidOperationException("No process is currently running to send input to.");
        }

        try
        {
            if (_isPtyProcess && _stdinWriter != null)
            {
                // For PTY processes, send input through the stdin writer
                await _stdinWriter.WriteAsync(input);
                await _stdinWriter.FlushAsync();
            }
            else if (_stdinWriter != null)
            {
                // Send input to regular process
                await _stdinWriter.WriteAsync(input);
                await _stdinWriter.FlushAsync();
            }
            else
            {
                throw new InvalidOperationException("Stdin stream is not available for the current process.");
            }
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
        
        if (_stdinPipeClient != null)
        {
            try
            {
                _stdinPipeClient.Dispose();
                _stdinPipeClient = null;
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
    public async Task<ProcessInfo> StartPtyProcessAsync(string command, string arguments, string? workingDirectory = null, 
        (int Columns, int Rows)? terminalSize = null, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(CliExecutor));
        
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        
        await _executionSemaphore.WaitAsync(cancellationToken);
        
        try
        {
            // Stop any existing process
            if (_currentProcess?.State == ProcessState.Running)
            {
                await StopProcessInternalAsync(graceful: false, cancellationToken);
            }
            
            _currentCancellationSource = new CancellationTokenSource();
            _terminalSize = terminalSize ?? (80, 24);
            _isPtyProcess = true;
            
            // For PTY processes, we use enhanced CliWrap with environment variables for terminal size
            var envVars = new Dictionary<string, string?>
            {
                ["COLUMNS"] = _terminalSize.Value.Columns.ToString(),
                ["LINES"] = _terminalSize.Value.Rows.ToString(),
                ["TERM"] = "xterm-256color"
            };
            
            // Create pipes for stdin communication
            _stdinPipeServer = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
            _stdinPipeClient = new AnonymousPipeClientStream(PipeDirection.In, _stdinPipeServer.GetClientHandleAsString());
            _stdinWriter = new StreamWriter(_stdinPipeServer) { AutoFlush = true };
            
            var cmd = Cli.Wrap(command)
                .WithArguments(arguments)
                .WithWorkingDirectory(workingDirectory ?? Environment.CurrentDirectory)
                .WithStandardInputPipe(PipeSource.FromStream(_stdinPipeClient))
                .WithEnvironmentVariables(envVars)
                .WithValidation(CommandResultValidation.None);
            
            _currentCommand = cmd;
            
            var processInfo = new ProcessInfo
            {
                ProcessId = 0, // Will be updated when process starts
                State = ProcessState.Running,
                Command = command,
                Arguments = arguments,
                WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
                StartTime = DateTime.UtcNow
            };
            
            _currentProcess = processInfo;
            
            // Start the process in the background with PTY-like behavior
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
                                var outputLine = new OutputLine
                                {
                                    Text = stdOut.Text,
                                    IsStdOut = true
                                };
                                _outputSubject.OnNext(outputLine);
                                break;

                            case StandardErrorCommandEvent stdErr:
                                var errorLine = new OutputLine
                                {
                                    Text = stdErr.Text,
                                    IsStdOut = false
                                };
                                _outputSubject.OnNext(errorLine);
                                break;

                            case ExitedCommandEvent exited:
                                _currentProcess = _currentProcess with
                                {
                                    State = exited.ExitCode == 0 ? ProcessState.Completed : ProcessState.Failed,
                                    EndTime = DateTime.UtcNow,
                                    ExitCode = exited.ExitCode
                                };
                                return;
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
                    var errorLine = new OutputLine
                    {
                        Text = $"PTY Process Error: {ex.Message}\r\n",
                        IsStdOut = false
                    };
                    _outputSubject.OnNext(errorLine);
                    
                    _currentProcess = _currentProcess with
                    {
                        State = ProcessState.Failed,
                        EndTime = DateTime.UtcNow,
                        ExitCode = -1
                    };
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
    public async Task ResizeTerminalAsync(int columns, int rows, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(CliExecutor));
            
        if (!_isPtyProcess)
        {
            throw new InvalidOperationException("No PTY process is currently running to resize.");
        }
        
        // Update the stored terminal size
        _terminalSize = (columns, rows);
        
        // For now, we store the size and it will be used for new processes
        // In a full PTY implementation, this would send a SIGWINCH signal
        await Task.CompletedTask; // Keep async signature for consistency
        
        // Send a notification about the resize
        var resizeNotification = new OutputLine
        {
            Text = $"\r\n[Terminal resized to {columns}x{rows}]\r\n",
            IsStdOut = true
        };
        _outputSubject.OnNext(resizeNotification);
    }
    
    /// <inheritdoc />
    public async Task SendSignalAsync(ProcessSignal signal, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(CliExecutor));
            
        if (_currentProcess?.State != ProcessState.Running)
        {
            throw new InvalidOperationException("No process is currently running to send signal to.");
        }
        
        try
        {
            switch (signal)
            {
                case ProcessSignal.Interrupt:
                    // Send Ctrl+C equivalent
                    if (_isPtyProcess && _stdinWriter != null)
                    {
                        await _stdinWriter.WriteAsync("\u0003"); // ASCII 3 (Ctrl+C)
                        await _stdinWriter.FlushAsync();
                    }
                    else if (_currentCancellationSource != null)
                    {
                        _currentCancellationSource.Cancel();
                    }
                    break;
                    
                case ProcessSignal.Quit:
                    // Send Ctrl+\ equivalent
                    if (_isPtyProcess && _stdinWriter != null)
                    {
                        await _stdinWriter.WriteAsync("\u001c"); // ASCII 28 (Ctrl+\)
                        await _stdinWriter.FlushAsync();
                    }
                    else
                    {
                        _currentCancellationSource?.Cancel();
                    }
                    break;
                    
                case ProcessSignal.Stop:
                    // Send Ctrl+Z equivalent
                    if (_isPtyProcess && _stdinWriter != null)
                    {
                        await _stdinWriter.WriteAsync("\u001a"); // ASCII 26 (Ctrl+Z)
                        await _stdinWriter.FlushAsync();
                    }
                    break;
                    
                case ProcessSignal.Terminate:
                case ProcessSignal.Kill:
                    await StopProcessInternalAsync(graceful: signal == ProcessSignal.Terminate, cancellationToken);
                    break;
                    
                default:
                    throw new NotSupportedException($"Signal {signal} is not supported.");
            }
        }
        catch (Exception ex)
        {
            var errorLine = new OutputLine
            {
                Text = $"Error sending signal {signal}: {ex.Message}\r\n",
                IsStdOut = false
            };
            _outputSubject.OnNext(errorLine);
            throw;
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
        
        // Clean up PTY resources
        try
        {
            if (_isPtyProcess)
            {
                _isPtyProcess = false;
                _terminalSize = null;
            }
        }
        catch
        {
            // Ignore exceptions during disposal
        }
        
        _stdinWriter?.Dispose();
        _stdinPipeClient?.Dispose();
        _stdinPipeServer?.Dispose();
        _currentCancellationSource?.Dispose();
        _outputSubject.Dispose();
        _executionSemaphore.Dispose();
    }
}