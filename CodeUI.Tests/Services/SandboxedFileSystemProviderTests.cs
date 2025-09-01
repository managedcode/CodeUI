using CodeUI.Core.Services;
using Xunit;

namespace CodeUI.Tests.Services;

public class SandboxedFileSystemProviderTests : IDisposable
{
    private readonly SandboxedFileSystemProvider _provider;
    private readonly string _tempWorkspace;
    private readonly string _systemDirectory;

    public SandboxedFileSystemProviderTests()
    {
        _provider = new SandboxedFileSystemProvider();
        _tempWorkspace = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempWorkspace);
        
        // Use a system directory that should never be allowed
        _systemDirectory = Environment.OSVersion.Platform == PlatformID.Win32NT 
            ? @"C:\Windows\System32" 
            : "/etc";
    }

    public void Dispose()
    {
        _provider?.Dispose();
        if (Directory.Exists(_tempWorkspace))
        {
            try
            {
                Directory.Delete(_tempWorkspace, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public void SetAllowedWorkspaces_ShouldRestrictAccessToSpecifiedDirectories()
    {
        // Arrange & Act
        _provider.SetAllowedWorkspaces(new[] { _tempWorkspace });

        // Assert
        Assert.True(_provider.IsPathAllowed(_tempWorkspace));
        Assert.True(_provider.IsPathAllowed(Path.Combine(_tempWorkspace, "subdirectory")));
        Assert.False(_provider.IsPathAllowed(_systemDirectory));
        Assert.False(_provider.IsPathAllowed("/tmp"));
        Assert.False(_provider.IsPathAllowed(@"C:\"));
    }

    [Fact]
    public void IsPathAllowed_ShouldPreventAccessToSystemDirectories()
    {
        // Arrange
        _provider.SetAllowedWorkspaces(new[] { _tempWorkspace });

        // Act & Assert
        Assert.False(_provider.IsPathAllowed(_systemDirectory));
        Assert.False(_provider.IsPathAllowed("/"));
        Assert.False(_provider.IsPathAllowed(@"C:\"));
        Assert.False(_provider.IsPathAllowed("/usr"));
        Assert.False(_provider.IsPathAllowed("/var"));
        Assert.False(_provider.IsPathAllowed(@"C:\Program Files"));
    }

    [Fact]
    public void NormalizePath_ShouldPreventPathTraversalAttacks()
    {
        // Arrange
        var traversalPaths = new[]
        {
            "../../../etc/passwd",
            "..\\..\\..\\windows\\system32",
            _tempWorkspace + "/../../../etc/passwd",
            _tempWorkspace + "\\..\\..\\..\\windows\\system32"
        };

        // Act & Assert
        foreach (var path in traversalPaths)
        {
            try
            {
                var normalized = _provider.NormalizePath(path);
                Assert.DoesNotContain("..", normalized);
            }
            catch (ArgumentException)
            {
                // This is also acceptable - preventing the normalization
                Assert.True(true);
            }
            catch (System.Security.SecurityException)
            {
                // This is the expected behavior for path traversal detection
                Assert.True(true);
            }
        }
    }

    [Fact]
    public void NormalizePath_ShouldThrowOnNullOrEmpty()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _provider.NormalizePath(null!));
        Assert.Throws<ArgumentException>(() => _provider.NormalizePath(string.Empty));
    }

    [Fact]
    public async Task CreateSecureFileAsync_ShouldPreventAccessOutsideWorkspace()
    {
        // Arrange
        _provider.SetAllowedWorkspaces(new[] { _tempWorkspace });

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _provider.CreateSecureFileAsync(_systemDirectory, "test.txt"));
    }

    [Fact]
    public async Task CreateSecureFileAsync_ShouldPreventInvalidFilenames()
    {
        // Arrange
        _provider.SetAllowedWorkspaces(new[] { _tempWorkspace });
        var invalidFilenames = new[] { "../test.txt", "test/file.txt", "test\\file.txt", string.Empty };

        // Act & Assert
        foreach (var filename in invalidFilenames)
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => _provider.CreateSecureFileAsync(_tempWorkspace, filename));
        }
    }

    [Fact]
    public async Task CreateSecureDirectoryAsync_ShouldPreventAccessOutsideWorkspace()
    {
        // Arrange
        _provider.SetAllowedWorkspaces(new[] { _tempWorkspace });

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _provider.CreateSecureDirectoryAsync(_systemDirectory, "testdir"));
    }

    [Fact]
    public async Task CreateSecureDirectoryAsync_ShouldPreventInvalidDirectoryNames()
    {
        // Arrange
        _provider.SetAllowedWorkspaces(new[] { _tempWorkspace });
        var invalidDirNames = new[] { "../testdir", "test/dir", "test\\dir", string.Empty };

        // Act & Assert
        foreach (var dirName in invalidDirNames)
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => _provider.CreateSecureDirectoryAsync(_tempWorkspace, dirName));
        }
    }

    [Fact]
    public async Task DeleteSecureAsync_ShouldPreventAccessOutsideWorkspace()
    {
        // Arrange
        _provider.SetAllowedWorkspaces(new[] { _tempWorkspace });

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _provider.DeleteSecureAsync(_systemDirectory));
    }

    [Fact]
    public async Task RenameSecureAsync_ShouldPreventAccessOutsideWorkspace()
    {
        // Arrange
        _provider.SetAllowedWorkspaces(new[] { _tempWorkspace });
        var testFile = Path.Combine(_tempWorkspace, "test.txt");
        await File.WriteAllTextAsync(testFile, "test");

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _provider.RenameSecureAsync(_systemDirectory, "newname"));
    }

    [Fact]
    public async Task RenameSecureAsync_ShouldPreventInvalidNewNames()
    {
        // Arrange
        _provider.SetAllowedWorkspaces(new[] { _tempWorkspace });
        var testFile = Path.Combine(_tempWorkspace, "test.txt");
        await File.WriteAllTextAsync(testFile, "test");
        var invalidNames = new[] { "../newname", "new/name", "new\\name", string.Empty };

        // Act & Assert
        foreach (var newName in invalidNames)
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => _provider.RenameSecureAsync(testFile, newName));
        }
    }

    [Fact]
    public async Task GetSecureDirectoryContentsAsync_ShouldPreventAccessOutsideWorkspace()
    {
        // Arrange
        _provider.SetAllowedWorkspaces(new[] { _tempWorkspace });

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _provider.GetSecureDirectoryContentsAsync(_systemDirectory));
    }

    [Fact]
    public async Task GetSecureDirectoryContentsAsync_ShouldReturnOnlyAllowedItems()
    {
        // Arrange
        _provider.SetAllowedWorkspaces(new[] { _tempWorkspace });
        
        // Create test files and directories
        var testFile = Path.Combine(_tempWorkspace, "test.txt");
        var testDir = Path.Combine(_tempWorkspace, "testdir");
        await File.WriteAllTextAsync(testFile, "test");
        Directory.CreateDirectory(testDir);

        // Act
        var contents = await _provider.GetSecureDirectoryContentsAsync(_tempWorkspace);

        // Assert
        Assert.NotEmpty(contents);
        Assert.All(contents, item => Assert.True(_provider.IsPathAllowed(item.FullPath)));
    }

    [Fact]
    public void ResolveSymbolicLink_ShouldPreventAccessOutsideWorkspace()
    {
        // Arrange
        _provider.SetAllowedWorkspaces(new[] { _tempWorkspace });

        // Act & Assert
        Assert.Throws<UnauthorizedAccessException>(
            () => _provider.ResolveSymbolicLink(_systemDirectory));
    }

    [Fact]
    public void StartWatching_ShouldOnlyWatchAllowedWorkspaces()
    {
        // Arrange
        bool eventFired = false;
        _provider.SetAllowedWorkspaces(new[] { _tempWorkspace });
        _provider.FileSystemChanged += (sender, args) => eventFired = true;

        // Act
        _provider.StartWatching();
        
        // Create a file to trigger the event
        var testFile = Path.Combine(_tempWorkspace, "watch_test.txt");
        File.WriteAllText(testFile, "test");

        // Give the file watcher some time to detect the change
        Thread.Sleep(100);

        // Assert
        Assert.True(eventFired);
        
        // Cleanup
        if (File.Exists(testFile))
            File.Delete(testFile);
    }

    [Fact]
    public void StopWatching_ShouldStopFileSystemWatching()
    {
        // Arrange
        bool eventFired = false;
        _provider.SetAllowedWorkspaces(new[] { _tempWorkspace });
        _provider.FileSystemChanged += (sender, args) => eventFired = true;
        _provider.StartWatching();

        // Act
        _provider.StopWatching();
        
        // Create a file after stopping - should not trigger event
        var testFile = Path.Combine(_tempWorkspace, "watch_test.txt");
        File.WriteAllText(testFile, "test");
        
        // Give some time for potential event
        Thread.Sleep(100);

        // Assert
        Assert.False(eventFired);
        
        // Cleanup
        if (File.Exists(testFile))
            File.Delete(testFile);
    }

    [Fact]
    public void FileSystemWatcher_ShouldOnlyReportAllowedPaths()
    {
        // Arrange
        var reportedPaths = new List<string>();
        _provider.SetAllowedWorkspaces(new[] { _tempWorkspace });
        _provider.FileSystemChanged += (sender, args) => reportedPaths.Add(args.FullPath);
        _provider.StartWatching();

        // Act
        var testFile = Path.Combine(_tempWorkspace, "allowed_test.txt");
        File.WriteAllText(testFile, "test");
        
        // Give the file watcher some time
        Thread.Sleep(100);

        // Assert
        Assert.All(reportedPaths, path => Assert.True(_provider.IsPathAllowed(path)));
        
        // Cleanup
        if (File.Exists(testFile))
            File.Delete(testFile);
    }
}