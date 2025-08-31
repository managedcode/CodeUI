using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using CodeUI.Core.Services;

namespace CodeUI.Tests.Components;

/// <summary>
/// Tests for the Terminal component functionality.
/// </summary>
public class TerminalComponentTests : IClassFixture<WebApplicationFactory<CodeUI.Web.Program>>
{
    private readonly WebApplicationFactory<CodeUI.Web.Program> _factory;

    public TerminalComponentTests(WebApplicationFactory<CodeUI.Web.Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public void ServiceProvider_ShouldResolveCliExecutor()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        // Act
        var cliExecutor = serviceProvider.GetService<ICliExecutor>();

        // Assert
        Assert.NotNull(cliExecutor);
        Assert.IsType<CliExecutor>(cliExecutor);
    }

    [Fact]
    public async Task TerminalPage_ShouldLoadSuccessfully()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/terminal");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        
        // Verify terminal-related content is present
        Assert.Contains("CodeUI Terminal", content);
        Assert.Contains("terminal-element", content);
    }

    [Fact]
    public async Task App_ShouldIncludeXTermJavaScript()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - Check the main page which includes all the JS references
        var response = await client.GetAsync("/");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        
        // Verify XTerm.js resources are included in the layout
        Assert.Contains("xterm@5.3.0", content);
        Assert.Contains("xterm-addon-fit@0.8.0", content);
        Assert.Contains("terminal.js", content);
    }
}