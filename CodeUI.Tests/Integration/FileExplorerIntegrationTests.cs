using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using CodeUI.Core.Services;

namespace CodeUI.Tests.Integration;

public class FileExplorerIntegrationTests(WebApplicationFactory<CodeUI.Web.Program> factory)
    : IClassFixture<WebApplicationFactory<CodeUI.Web.Program>>
{
    [Fact]
    public async Task FileExplorer_PageShouldLoad()
    {
        // Arrange
        var client = factory.CreateClient();

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
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        // Act
        var fileSystemService = services.GetService<IFileSystemService>();

        // Assert
        Assert.NotNull(fileSystemService);
        Assert.IsType<FileSystemService>(fileSystemService);
    }
}