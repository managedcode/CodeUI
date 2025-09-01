using CodeUI.Core.Models;
using CodeUI.Core.Services;
using System.Reactive.Linq;
using Xunit;

namespace CodeUI.Tests.Services;

/// <summary>
/// Tests for interactive CLI process functionality.
/// </summary>
public class CliExecutorInteractiveTests : IDisposable
{
    private readonly CliExecutor _executor;

    public CliExecutorInteractiveTests()
    {
        _executor = new CliExecutor();
    }

    public void Dispose()
    {
        _executor.Dispose();
    }

    [Fact]
    public async Task SendInputAsync_ShouldThrowWhenNoProcessRunning()
    {
        // Arrange & Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _executor.SendInputAsync("test input"));
    }

    [Fact]
    public async Task SendInputAsync_ShouldThrowWhenNonInteractiveProcessRunning()
    {
        // Arrange
        var processInfo = await _executor.StartProcessAsync("echo", "test");
        
        // Wait a moment for process to start
        await Task.Delay(100);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _executor.SendInputAsync("test input"));
    }

    [Fact]
    public async Task StartInteractiveProcessAsync_ShouldAllowSendInput()
    {
        // Arrange
        var outputLines = new List<OutputLine>();
        var subscription = _executor.Output.Subscribe(outputLines.Add);

        try
        {
            // Start an interactive cat process (reads stdin and echoes to stdout)
            var processInfo = await _executor.StartInteractiveProcessAsync("cat", "");
            
            // Assert process started
            Assert.Equal(ProcessState.Running, processInfo.State);
            Assert.Equal("cat", processInfo.Command);

            // Wait a longer time for process to initialize
            await Task.Delay(500);

            // Verify process is still running
            Assert.NotNull(_executor.CurrentProcess);
            Assert.Equal(ProcessState.Running, _executor.CurrentProcess.State);

            // Act - Send input to the process
            await _executor.SendInputAsync("Hello World\n");

            // Wait for output
            await Task.Delay(200);

            // Assert - Should not throw exception and process should still be running
            Assert.NotNull(_executor.CurrentProcess);
            Assert.Equal(ProcessState.Running, _executor.CurrentProcess.State);

            // Stop the process
            await _executor.StopProcessAsync(graceful: false);
        }
        finally
        {
            subscription.Dispose();
        }
    }

    [Fact]
    public async Task InteractiveProcess_ShouldReceiveMultipleInputs()
    {
        // Arrange
        var outputLines = new List<OutputLine>();
        var subscription = _executor.Output.Subscribe(outputLines.Add);

        try
        {
            // Start interactive cat process
            var processInfo = await _executor.StartInteractiveProcessAsync("cat", "");
            
            // Wait longer for process to start
            await Task.Delay(500);

            // Verify process is running before sending input
            Assert.NotNull(_executor.CurrentProcess);
            Assert.Equal(ProcessState.Running, _executor.CurrentProcess.State);

            // Act - Send multiple inputs
            await _executor.SendInputAsync("First line\n");
            await Task.Delay(100);
            await _executor.SendInputAsync("Second line\n");
            await Task.Delay(100);

            // Wait for all outputs
            await Task.Delay(300);

            // Assert - Should have received outputs
            Assert.True(outputLines.Count > 0, "Should have received some output from cat command");

            // Stop the process
            await _executor.StopProcessAsync(graceful: false);
        }
        finally
        {
            subscription.Dispose();
        }
    }
}