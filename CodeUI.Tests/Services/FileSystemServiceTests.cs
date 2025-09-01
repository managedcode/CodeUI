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

    [Fact]
    public void FileSystemService_ShouldRestrictAccessToWorkspaceOnly()
    {
        // Arrange
        using var service = new FileSystemService();
        var workingDir = service.GetWorkingDirectory();

        // Act & Assert - Should allow access to working directory
        Assert.True(service.ExistsAsync(workingDir).Result);
        
        // Should not allow access to system directories
        var systemDir = Environment.OSVersion.Platform == PlatformID.Win32NT 
            ? @"C:\Windows\System32" 
            : "/etc";
            
        if (Directory.Exists(systemDir))
        {
            Assert.False(service.ExistsAsync(systemDir).Result);
        }
    }

    [Fact]
    public async Task FileSystemService_ShouldPreventPathTraversalInOperations()
    {
        // Arrange
        using var service = new FileSystemService();
        var workingDir = service.GetWorkingDirectory();

        // Act & Assert - Path traversal should be prevented
        var traversalPath = Path.Combine(workingDir, "..", "..", "etc", "passwd");
        var exists = await service.ExistsAsync(traversalPath);
        
        // Should return false for paths outside workspace
        Assert.False(exists);
    }

    [Fact]
    public async Task FileSystemService_ShouldHandleDirectoryContentsSecurely()
    {
        // Arrange
        using var service = new FileSystemService();
        var workingDir = service.GetWorkingDirectory();

        // Act
        var contents = await service.GetDirectoryContentsAsync(workingDir);

        // Assert
        Assert.NotNull(contents);
        // All returned items should be within the working directory
        Assert.All(contents, item => 
            Assert.True(item.FullPath.StartsWith(workingDir, StringComparison.OrdinalIgnoreCase)));
    }

    [Fact] 
    public async Task FileSystemService_ShouldPreventUnauthorizedDirectoryAccess()
    {
        // Arrange
        using var service = new FileSystemService();
        var systemDir = Environment.OSVersion.Platform == PlatformID.Win32NT 
            ? @"C:\Windows\System32" 
            : "/etc";

        // Act
        var contents = await service.GetDirectoryContentsAsync(systemDir);

        // Assert - Should return empty list for unauthorized directories
        Assert.Empty(contents);
    }

    [Fact]
    public async Task FileSystemService_SetWorkingDirectoryAsync_ShouldUpdateWorkspace()
    {
        // Arrange
        using var service = new FileSystemService();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            await service.SetWorkingDirectoryAsync(tempDir);

            // Assert
            Assert.Equal(tempDir, service.GetWorkingDirectory());
            
            // Should be able to access the new working directory
            Assert.True(await service.ExistsAsync(tempDir));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task FileSystemService_ShouldHandleSymbolicLinksSafely()
    {
        // This test is platform-specific and may not work on all systems
        // We'll test the basic functionality
        using var service = new FileSystemService();
        var workingDir = service.GetWorkingDirectory();
        
        // Even if we can't create symlinks, the service should handle them safely
        var info = await service.GetItemInfoAsync(workingDir);
        Assert.NotNull(info);
    }
}