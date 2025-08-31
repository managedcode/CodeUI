using System.Reactive.Linq;
using CodeUI.Core.Models;
using CodeUI.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CodeUI.Tests.Services;

/// <summary>
/// Integration tests demonstrating real-world CLI usage scenarios.
/// </summary>
public class CliExecutorIntegrationTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ICliExecutor _cliExecutor;

    public CliExecutorIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddScoped<ICliExecutor, CliExecutor>();
        _serviceProvider = services.BuildServiceProvider();
        _cliExecutor = _serviceProvider.GetRequiredService<ICliExecutor>();
    }

    [Fact]
    public async Task GitVersionCommand_ShouldExecuteSuccessfully()
    {
        // Arrange
        var outputLines = new List<OutputLine>();
        using var subscription = _cliExecutor.Output.Subscribe(outputLines.Add);

        // Try to get git version - this will work if git is installed
        try
        {
            // Act
            var result = await _cliExecutor.ExecuteAsync("git", "--version");

            // Assert
            Assert.Equal(ProcessState.Completed, result.State);
            Assert.Equal(0, result.ExitCode);
            Assert.Equal("git", result.Command);
            Assert.Equal("--version", result.Arguments);
            
            // Should have output containing "git version"
            Assert.Contains(outputLines, line => line.Text.ToLower().Contains("git version") && line.IsStdOut);
        }
        catch (Exception ex) when (ex.Message.Contains("No such file") || ex.Message.Contains("not found"))
        {
            // Git is not installed on this system - skip the test
            Assert.True(true, "Git not available on test system");
        }
    }

    [Fact]
    public async Task EchoCommand_ShouldStreamOutputInRealTime()
    {
        // Arrange
        var outputLines = new List<OutputLine>();
        var testMessage = "Integration test message";
        
        using var subscription = _cliExecutor.Output
            .Where(line => line.Text.Contains(testMessage))
            .Subscribe(outputLines.Add);

        // Act
        var result = await _cliExecutor.ExecuteAsync("echo", testMessage);

        // Wait for output to be processed
        await Task.Delay(100);

        // Assert
        Assert.Equal(ProcessState.Completed, result.State);
        Assert.Equal(0, result.ExitCode);
        Assert.NotEmpty(outputLines);
        Assert.Contains(outputLines, line => line.Text.Contains(testMessage) && line.IsStdOut);
        
        // Verify timestamp is recent
        var latestOutput = outputLines.OrderByDescending(l => l.Timestamp).First();
        Assert.True(latestOutput.Timestamp > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task LongRunningCommand_ShouldBeStoppable()
    {
        // Arrange - Start a long-running command
        var startTime = DateTime.UtcNow;
        
        // Act - Start and then stop a sleep command
        var processTask = _cliExecutor.StartProcessAsync("sleep", "10");
        var processInfo = await processTask;
        
        Assert.Equal(ProcessState.Running, processInfo.State);
        
        // Stop the process
        await _cliExecutor.StopProcessAsync();
        
        // Wait a bit for the process to be stopped
        await Task.Delay(100);
        
        // Assert
        var endTime = DateTime.UtcNow;
        var duration = endTime - startTime;
        
        Assert.True(duration < TimeSpan.FromSeconds(5), "Process should have been stopped quickly");
        Assert.Equal(ProcessState.Failed, _cliExecutor.CurrentProcess?.State);
    }

    [Fact]
    public void ServiceProvider_ShouldResolveCliExecutor()
    {
        // Arrange & Act
        var cliExecutor = _serviceProvider.GetRequiredService<ICliExecutor>();

        // Assert
        Assert.NotNull(cliExecutor);
        Assert.IsType<CliExecutor>(cliExecutor);
    }

    [Fact]
    public async Task MultipleSequentialCommands_ShouldExecuteCorrectly()
    {
        // Arrange
        var commands = new[]
        {
            ("echo", "First command"),
            ("echo", "Second command"),
            ("echo", "Third command")
        };

        // Act & Assert
        foreach (var (command, args) in commands)
        {
            var result = await _cliExecutor.ExecuteAsync(command, args);
            
            Assert.Equal(ProcessState.Completed, result.State);
            Assert.Equal(0, result.ExitCode);
            Assert.Equal(command, result.Command);
            Assert.Equal(args, result.Arguments);
        }
    }

    [Fact]
    public async Task OutputObservable_ShouldSupportMultipleSubscribers()
    {
        // Arrange
        var subscriber1Output = new List<OutputLine>();
        var subscriber2Output = new List<OutputLine>();
        
        using var subscription1 = _cliExecutor.Output.Subscribe(subscriber1Output.Add);
        using var subscription2 = _cliExecutor.Output.Subscribe(subscriber2Output.Add);

        // Act
        await _cliExecutor.ExecuteAsync("echo", "Multiple subscribers test");
        
        // Wait for output processing
        await Task.Delay(100);

        // Assert
        Assert.NotEmpty(subscriber1Output);
        Assert.NotEmpty(subscriber2Output);
        
        // Both subscribers should receive the same output
        Assert.Equal(subscriber1Output.Count, subscriber2Output.Count);
        
        for (int i = 0; i < subscriber1Output.Count; i++)
        {
            Assert.Equal(subscriber1Output[i].Text, subscriber2Output[i].Text);
            Assert.Equal(subscriber1Output[i].IsStdOut, subscriber2Output[i].IsStdOut);
        }
    }

    public void Dispose()
    {
        _cliExecutor?.Dispose();
        (_serviceProvider as IDisposable)?.Dispose();
    }
}