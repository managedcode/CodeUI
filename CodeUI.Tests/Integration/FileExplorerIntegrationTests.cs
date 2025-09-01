using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using CodeUI.Core.Services;

namespace CodeUI.Tests.Integration;

public class FileExplorerIntegrationTests : IClassFixture<WebApplicationFactory<CodeUI.Web.Program>>
{
    private readonly WebApplicationFactory<CodeUI.Web.Program> _factory;

    public FileExplorerIntegrationTests(WebApplicationFactory<CodeUI.Web.Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FileExplorer_PageShouldLoad()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/fileexplorer");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("File Explorer", content);
        Assert.Contains("Current Directory:", content);
    }

    [Fact]
    public void FileSystemService_ShouldBeRegisteredInDI()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        // Act
        var fileSystemService = services.GetService<IFileSystemService>();

        // Assert
        Assert.NotNull(fileSystemService);
        Assert.IsType<FileSystemService>(fileSystemService);
    }
}