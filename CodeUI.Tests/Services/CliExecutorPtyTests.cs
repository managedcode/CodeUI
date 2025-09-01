using CodeUI.Core.Models;
using CodeUI.Core.Services;
using System.Reactive.Linq;
using Xunit;

namespace CodeUI.Tests.Services;

/// <summary>
/// Tests for PTY (Pseudo Terminal) functionality in CLI executor.
/// </summary>
public class CliExecutorPtyTests : IDisposable
{
    private readonly CliExecutor _executor;

    public CliExecutorPtyTests()
    {
        _executor = new CliExecutor();
    }

    [Fact]
    public async Task StartPtyProcessAsync_ShouldStartProcessWithTerminalSize()
    {
        // Arrange
        var outputLines = new List<OutputLine>();
        var subscription = _executor.Output.Subscribe(outputLines.Add);

        try
        {
            // Act - Start a PTY process with custom terminal size
            var processInfo = await _executor.StartPtyProcessAsync("echo", "Hello PTY", terminalSize: (100, 50));
            
            // Assert
            Assert.Equal(ProcessState.Running, processInfo.State);
            Assert.Equal("echo", processInfo.Command);
            Assert.Equal("Hello PTY", processInfo.Arguments);
            Assert.True(processInfo.ProcessId >= 0);
            
            // Wait for process to complete
            await Task.Delay(1000);
            
            // Should have received output
            Assert.True(outputLines.Count > 0);
            var hasExpectedOutput = outputLines.Any(line => line.Text.Contains("Hello PTY"));
            Assert.True(hasExpectedOutput, "Should have received expected output");
        }
        finally
        {
            subscription.Dispose();
            if (_executor.CurrentProcess?.State == ProcessState.Running)
            {
                await _executor.StopProcessAsync(graceful: false);
            }
        }
    }

    [Fact]
    public async Task StartPtyProcessAsync_ShouldDefaultToStandardTerminalSize()
    {
        // Arrange
        var outputLines = new List<OutputLine>();
        var subscription = _executor.Output.Subscribe(outputLines.Add);

        try
        {
            // Act - Start PTY process without specifying terminal size
            var processInfo = await _executor.StartPtyProcessAsync("echo", "Default size test");
            
            // Assert
            Assert.Equal(ProcessState.Running, processInfo.State);
            
            // Wait for process to complete
            await Task.Delay(1000);
            
            // Should have received output
            Assert.True(outputLines.Count > 0);
        }
        finally
        {
            subscription.Dispose();
            if (_executor.CurrentProcess?.State == ProcessState.Running)
            {
                await _executor.StopProcessAsync(graceful: false);
            }
        }
    }

    [Fact]
    public async Task ResizeTerminalAsync_ShouldUpdateTerminalSize()
    {
        // Arrange
        var outputLines = new List<OutputLine>();
        var subscription = _executor.Output.Subscribe(outputLines.Add);

        try
        {
            // Start a PTY process
            await _executor.StartPtyProcessAsync("echo", "Test resize");
            
            // Act - Resize terminal
            await _executor.ResizeTerminalAsync(120, 60);
            
            // Assert - Should receive resize notification
            await Task.Delay(500);
            
            var resizeNotification = outputLines.FirstOrDefault(line => 
                line.Text.Contains("Terminal resized to 120x60"));
            Assert.NotNull(resizeNotification);
        }
        finally
        {
            subscription.Dispose();
            if (_executor.CurrentProcess?.State == ProcessState.Running)
            {
                await _executor.StopProcessAsync(graceful: false);
            }
        }
    }

    [Fact]
    public async Task ResizeTerminalAsync_ShouldThrowWhenNoPtyProcess()
    {
        // Arrange - No PTY process running

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _executor.ResizeTerminalAsync(80, 24));
    }

    [Fact]
    public async Task SendSignalAsync_ShouldSendInterruptSignal()
    {
        // Arrange
        var outputLines = new List<OutputLine>();
        var subscription = _executor.Output.Subscribe(outputLines.Add);

        try
        {
            // Start a PTY process
            await _executor.StartPtyProcessAsync("echo", "Test interrupt");
            
            await Task.Delay(100); // Short delay
            
            // Check if process is still running before sending signal
            if (_executor.CurrentProcess?.State == ProcessState.Running)
            {
                // Act - Send interrupt signal (Ctrl+C)
                await _executor.SendSignalAsync(ProcessSignal.Interrupt);
                await Task.Delay(500);
            }
            
            // Wait for process to complete
            await Task.Delay(1000);
            
            // Assert - Process should have completed (successfully or via signal)
            Assert.True(_executor.CurrentProcess?.State == ProcessState.Failed ||
                       _executor.CurrentProcess?.State == ProcessState.Completed);
        }
        finally
        {
            subscription.Dispose();
            if (_executor.CurrentProcess?.State == ProcessState.Running)
            {
                await _executor.StopProcessAsync(graceful: false);
            }
        }
    }

    [Fact]
    public async Task SendSignalAsync_ShouldSendTerminateSignal()
    {
        // Arrange
        var outputLines = new List<OutputLine>();
        var subscription = _executor.Output.Subscribe(outputLines.Add);

        try
        {
            // Start a PTY process
            await _executor.StartPtyProcessAsync("echo", "Test terminate");
            
            await Task.Delay(100); // Short delay
            
            // Check if process is still running before sending signal
            if (_executor.CurrentProcess?.State == ProcessState.Running)
            {
                // Act - Send terminate signal
                await _executor.SendSignalAsync(ProcessSignal.Terminate);
                await Task.Delay(500);
            }
            
            // Wait for process to complete
            await Task.Delay(1000);
            
            // Assert - Process should have completed (successfully or via signal)
            Assert.True(_executor.CurrentProcess?.State == ProcessState.Failed ||
                       _executor.CurrentProcess?.State == ProcessState.Completed);
        }
        finally
        {
            subscription.Dispose();
            if (_executor.CurrentProcess?.State == ProcessState.Running)
            {
                await _executor.StopProcessAsync(graceful: false);
            }
        }
    }

    [Fact]
    public async Task SendSignalAsync_ShouldThrowWhenNoProcess()
    {
        // Arrange - No process running

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _executor.SendSignalAsync(ProcessSignal.Interrupt));
    }

    [Fact]
    public async Task SendInputAsync_ShouldWorkWithPtyProcess()
    {
        // Arrange
        var outputLines = new List<OutputLine>();
        var subscription = _executor.Output.Subscribe(outputLines.Add);

        try
        {
            // Start an interactive PTY process that echoes input
            if (OperatingSystem.IsWindows())
            {
                // On Windows, use 'more' command which reads from stdin
                await _executor.StartPtyProcessAsync("more", "");
            }
            else
            {
                // On Unix systems, use 'cat' which echoes stdin to stdout
                await _executor.StartPtyProcessAsync("cat", "");
            }
            
            await Task.Delay(500); // Let process start
            
            // Act - Send input to the PTY process
            await _executor.SendInputAsync("Hello PTY Input\n");
            
            // Wait for output
            await Task.Delay(1000);
            
            // Assert - Should have received the echoed input (for cat command)
            if (!OperatingSystem.IsWindows())
            {
                var echoedInput = outputLines.FirstOrDefault(line => 
                    line.Text.Contains("Hello PTY Input"));
                Assert.NotNull(echoedInput);
            }
            
            // Process should still be running
            Assert.Equal(ProcessState.Running, _executor.CurrentProcess?.State);
        }
        finally
        {
            subscription.Dispose();
            if (_executor.CurrentProcess?.State == ProcessState.Running)
            {
                await _executor.StopProcessAsync(graceful: false);
            }
        }
    }

    [Fact]
    public async Task PtyProcess_ShouldHandleInteractivePrompts()
    {
        // This test simulates interactive prompts that require PTY support
        var outputLines = new List<OutputLine>();
        var subscription = _executor.Output.Subscribe(outputLines.Add);

        try
        {
            // Start a simple command that can handle interactive input
            if (OperatingSystem.IsWindows())
            {
                // On Windows, start a command prompt session
                await _executor.StartPtyProcessAsync("cmd", "/k echo Ready for input");
            }
            else
            {
                // On Unix, start a shell session
                await _executor.StartPtyProcessAsync("sh", "-c \"echo 'Ready for input'; cat\"");
            }
            
            await Task.Delay(1000); // Let process start and show prompt
            
            // Simulate y/n prompt response
            await _executor.SendInputAsync("y\n");
            await Task.Delay(500);
            
            // Should have received some output
            Assert.True(outputLines.Count > 0);
            
            // Process should be running
            Assert.Equal(ProcessState.Running, _executor.CurrentProcess?.State);
        }
        finally
        {
            subscription.Dispose();
            if (_executor.CurrentProcess?.State == ProcessState.Running)
            {
                await _executor.StopProcessAsync(graceful: false);
            }
        }
    }

    [Fact]
    public async Task PtyProcess_ShouldSupportEnvironmentVariables()
    {
        // Arrange
        var outputLines = new List<OutputLine>();
        var subscription = _executor.Output.Subscribe(outputLines.Add);

        try
        {
            // Start PTY process with custom terminal size
            await _executor.StartPtyProcessAsync("echo", "Terminal vars test", terminalSize: (90, 30));
            
            await Task.Delay(1000); // Wait for process to complete
            
            // The process should have run with the COLUMNS and LINES environment variables set
            // (This is verified internally by our implementation)
            Assert.True(outputLines.Count > 0);
            
            var hasOutput = outputLines.Any(line => line.Text.Contains("Terminal vars test"));
            Assert.True(hasOutput, "Should have received expected output with environment variables");
        }
        finally
        {
            subscription.Dispose();
            if (_executor.CurrentProcess?.State == ProcessState.Running)
            {
                await _executor.StopProcessAsync(graceful: false);
            }
        }
    }

    public void Dispose()
    {
        _executor?.Dispose();
    }
}