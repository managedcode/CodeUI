using CodeUI.Core.Models;
using CodeUI.Core.Services;
using Xunit;

namespace CodeUI.Tests.Services;

public class FileSystemServiceTests
{
    [Fact]
    public void FileSystemItem_ShouldCalculateCorrectIconClass()
    {
        // Arrange & Act
        var csharpFile = new FileSystemItem { Name = "Test.cs", Extension = ".cs", IsDirectory = false };
        var razorFile = new FileSystemItem { Name = "Test.razor", Extension = ".razor", IsDirectory = false };
        var jsonFile = new FileSystemItem { Name = "Test.json", Extension = ".json", IsDirectory = false };
        var directory = new FileSystemItem { Name = "TestDir", IsDirectory = true, IsExpanded = false };
        var expandedDirectory = new FileSystemItem { Name = "TestDir", IsDirectory = true, IsExpanded = true };

        // Assert
        Assert.Equal("fas fa-file-code text-info", csharpFile.IconClass);
        Assert.Equal("fas fa-file-code text-primary", razorFile.IconClass);
        Assert.Equal("fas fa-brackets-curly text-info", jsonFile.IconClass);
        Assert.Equal("fas fa-folder", directory.IconClass);
        Assert.Equal("fas fa-folder-open", expandedDirectory.IconClass);
    }

    [Fact]
    public void FileSystemItem_ShouldFormatFileSizeCorrectly()
    {
        // Arrange & Act
        var smallFile = new FileSystemItem { Size = 512 };
        var mediumFile = new FileSystemItem { Size = 1536 }; // 1.5 KB
        var largeFile = new FileSystemItem { Size = 2097152 }; // 2 MB

        // Assert
        Assert.Equal("512.0 B", smallFile.FormattedSize);
        Assert.Equal("1.5 KB", mediumFile.FormattedSize);
        Assert.Equal("2.0 MB", largeFile.FormattedSize);
    }

    [Fact]
    public void FileSystemItem_ShouldCalculateRelativeTimeCorrectly()
    {
        // Arrange
        var recentFile = new FileSystemItem { LastModified = DateTime.Now.AddMinutes(-30) };
        var todayFile = new FileSystemItem { LastModified = DateTime.Now.AddHours(-5) };
        var oldFile = new FileSystemItem { LastModified = DateTime.Now.AddDays(-30) };

        // Act & Assert
        Assert.Contains("minutes ago", recentFile.RelativeModifiedTime);
        Assert.Contains("hours ago", todayFile.RelativeModifiedTime);
        Assert.Matches(@"\w{3} \d{2}, \d{4}", oldFile.RelativeModifiedTime); // MMM dd, yyyy format
    }

    [Fact]
    public async Task FileSystemService_ShouldReturnRootDirectories()
    {
        // Arrange
        var service = new FileSystemService();

        // Act
        var result = await service.GetRootDirectoriesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.True(result.All(item => item.IsDirectory));
    }

    [Fact]
    public void FileSystemService_ShouldReturnCurrentWorkingDirectory()
    {
        // Arrange
        var service = new FileSystemService();

        // Act
        var workingDir = service.GetWorkingDirectory();

        // Assert
        Assert.NotNull(workingDir);
        Assert.True(Directory.Exists(workingDir));
    }

    [Fact]
    public async Task FileSystemService_ShouldReturnTrueForExistingPath()
    {
        // Arrange
        var service = new FileSystemService();
        var currentDir = Directory.GetCurrentDirectory();

        // Act
        var exists = await service.ExistsAsync(currentDir);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task FileSystemService_ShouldReturnFalseForNonExistingPath()
    {
        // Arrange
        var service = new FileSystemService();
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act
        var exists = await service.ExistsAsync(nonExistentPath);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task FileSystemService_ShouldSearchFiles()
    {
        // Arrange
        var service = new FileSystemService();

        // Act
        var results = await service.SearchAsync("*.cs");

        // Assert
        Assert.NotNull(results);
        // Note: Results may be empty depending on the test environment
    }
}