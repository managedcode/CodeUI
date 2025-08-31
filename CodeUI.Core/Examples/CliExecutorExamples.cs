using System.Reactive.Linq;
using CodeUI.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CodeUI.Core.Examples;

/// <summary>
/// Example demonstrating how to use the CliExecutor service for various CLI tools.
/// </summary>
public static class CliExecutorExamples
{
    /// <summary>
    /// Example of setting up and using the CLI executor with dependency injection.
    /// </summary>
    public static async Task BasicUsageExample()
    {
        // Setup dependency injection (this would typically be done in Program.cs)
        var services = new ServiceCollection();
        services.AddScoped<ICliExecutor, CliExecutor>();
        var serviceProvider = services.BuildServiceProvider();

        // Get the CLI executor service
        using var cliExecutor = serviceProvider.GetRequiredService<ICliExecutor>();

        // Subscribe to output stream for real-time monitoring
        using var subscription = cliExecutor.Output
            .Where(line => !string.IsNullOrWhiteSpace(line.Text))
            .Subscribe(line =>
            {
                var prefix = line.IsStdOut ? "[OUT]" : "[ERR]";
                Console.WriteLine($"{prefix} {line.Timestamp:HH:mm:ss} {line.Text}");
            });

        // Execute a simple command
        var result = await cliExecutor.ExecuteAsync("echo", "Hello from CLI executor!");
        Console.WriteLine($"Command completed with exit code: {result.ExitCode}");
    }

    /// <summary>
    /// Example of using CLI executor with Git commands.
    /// </summary>
    public static async Task GitExample()
    {
        using var cliExecutor = new CliExecutor();

        // Check if git is available
        var gitAvailable = await cliExecutor.IsCommandAvailableAsync("git");
        if (!gitAvailable)
        {
            Console.WriteLine("Git is not available on this system");
            return;
        }

        // Subscribe to output
        using var subscription = cliExecutor.Output.Subscribe(line =>
        {
            Console.WriteLine($"Git: {line.Text}");
        });

        // Get git version
        var versionResult = await cliExecutor.ExecuteAsync("git", "--version");
        Console.WriteLine($"Git version command exit code: {versionResult.ExitCode}");

        // Get current directory git status (if in a git repo)
        try
        {
            var statusResult = await cliExecutor.ExecuteAsync("git", "status --porcelain");
            Console.WriteLine($"Git status command exit code: {statusResult.ExitCode}");
        }
        catch
        {
            Console.WriteLine("Not in a git repository or git status failed");
        }
    }

    /// <summary>
    /// Example of using CLI executor with long-running processes and cancellation.
    /// </summary>
    public static async Task LongRunningProcessExample()
    {
        using var cliExecutor = new CliExecutor();

        // Subscribe to output
        using var subscription = cliExecutor.Output.Subscribe(line =>
        {
            Console.WriteLine($"Process: {line.Text}");
        });

        // Start a long-running process
        Console.WriteLine("Starting long-running process...");
        var processInfo = await cliExecutor.StartProcessAsync("sleep", "30");
        Console.WriteLine($"Process started with ID: {processInfo.ProcessId}");

        // Let it run for a bit
        await Task.Delay(2000);

        // Stop the process
        Console.WriteLine("Stopping process...");
        await cliExecutor.StopProcessAsync(graceful: true);
        
        Console.WriteLine($"Process final state: {cliExecutor.CurrentProcess?.State}");
        Console.WriteLine($"Process exit code: {cliExecutor.CurrentProcess?.ExitCode}");
    }

    /// <summary>
    /// Example of using CLI executor with Claude Code CLI (if available).
    /// </summary>
    public static async Task ClaudeCodeExample()
    {
        using var cliExecutor = new CliExecutor();

        // Check if claude-code is available
        var claudeAvailable = await cliExecutor.IsCommandAvailableAsync("claude-code");
        if (!claudeAvailable)
        {
            Console.WriteLine("Claude Code CLI is not available on this system");
            return;
        }

        // Subscribe to output
        using var subscription = cliExecutor.Output.Subscribe(line =>
        {
            Console.WriteLine($"Claude: {line.Text}");
        });

        // Get Claude Code help
        var helpResult = await cliExecutor.ExecuteAsync("claude-code", "--help");
        Console.WriteLine($"Claude Code help command exit code: {helpResult.ExitCode}");
    }

    /// <summary>
    /// Example of using CLI executor with multiple output subscribers.
    /// </summary>
    public static async Task MultipleSubscribersExample()
    {
        using var cliExecutor = new CliExecutor();

        var allOutput = new List<string>();
        var errorOutput = new List<string>();

        // Subscribe to all output
        using var allSubscription = cliExecutor.Output.Subscribe(line =>
        {
            allOutput.Add($"[{(line.IsStdOut ? "OUT" : "ERR")}] {line.Text}");
        });

        // Subscribe only to error output
        using var errorSubscription = cliExecutor.Output
            .Where(line => !line.IsStdOut)
            .Subscribe(line =>
            {
                errorOutput.Add(line.Text);
            });

        // Execute a command that might produce both stdout and stderr
        await cliExecutor.ExecuteAsync("echo", "This goes to stdout");
        
        // Execute a command that produces stderr (trying to cat a non-existent file)
        try
        {
            await cliExecutor.ExecuteAsync("cat", "/non/existent/file");
        }
        catch
        {
            // Command will fail, but we'll still get the stderr output
        }

        Console.WriteLine($"Total output lines: {allOutput.Count}");
        Console.WriteLine($"Error output lines: {errorOutput.Count}");
    }
}