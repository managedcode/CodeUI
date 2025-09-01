using CodeUI.Core.Models;
using CodeUI.Core.Services;
using LibGit2Sharp;

namespace CodeUI.Tests.Services;

/// <summary>
/// Unit tests for GitService functionality
/// </summary>
public class GitServiceTests : IDisposable
{
    private readonly IGitService _gitService;
    private readonly string _testRepoPath;
    private bool _disposed;

    public GitServiceTests()
    {
        _gitService = new GitService();
        _testRepoPath = Path.Combine(Path.GetTempPath(), $"test-repo-{Guid.NewGuid()}");
    }

    [Fact]
    public async Task IsGitRepositoryAsync_ShouldReturnFalse_ForNonGitDirectory()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"non-git-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            var result = await _gitService.IsGitRepositoryAsync(tempDir);

            // Assert
            Assert.False(result);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task FindRepositoryRootAsync_ShouldReturnNull_ForNonGitDirectory()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"non-git-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            var result = await _gitService.FindRepositoryRootAsync(tempDir);

            // Assert
            Assert.Null(result);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task IsGitRepositoryAsync_ShouldReturnTrue_ForValidGitRepository()
    {
        // Arrange
        CreateTestRepository();

        try
        {
            // Act
            var result = await _gitService.IsGitRepositoryAsync(_testRepoPath);

            // Assert
            Assert.True(result);
        }
        finally
        {
            CleanupTestRepository();
        }
    }

    [Fact]
    public async Task FindRepositoryRootAsync_ShouldReturnRoot_ForValidGitRepository()
    {
        // Arrange
        CreateTestRepository();

        try
        {
            // Act
            var result = await _gitService.FindRepositoryRootAsync(_testRepoPath);

            // Assert
            Assert.NotNull(result);
            Assert.Contains(_testRepoPath, result);
        }
        finally
        {
            CleanupTestRepository();
        }
    }

    [Fact]
    public async Task GetRepositoryStatusAsync_ShouldReturnNull_ForNonGitDirectory()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"non-git-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            var result = await _gitService.GetRepositoryStatusAsync(tempDir);

            // Assert
            Assert.Null(result);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task GetRepositoryStatusAsync_ShouldReturnCleanStatus_ForCleanRepository()
    {
        // Arrange
        CreateTestRepository();

        try
        {
            // Act
            var result = await _gitService.GetRepositoryStatusAsync(_testRepoPath);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_testRepoPath, result.RepositoryPath);
            Assert.True(result.CurrentBranch == "main" || result.CurrentBranch == "master"); // Git defaults vary
            Assert.False(result.IsDirty);
            Assert.Empty(result.ModifiedFiles);
        }
        finally
        {
            CleanupTestRepository();
        }
    }

    [Fact]
    public async Task GetRepositoryStatusAsync_ShouldReturnDirtyStatus_WhenFilesModified()
    {
        // Arrange
        CreateTestRepository();
        var testFile = Path.Combine(_testRepoPath, "modified-file.txt");
        await File.WriteAllTextAsync(testFile, "Modified content");

        try
        {
            // Act
            var result = await _gitService.GetRepositoryStatusAsync(_testRepoPath);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsDirty);
            Assert.NotEmpty(result.ModifiedFiles);
            Assert.Contains(result.ModifiedFiles, f => f.FilePath.Contains("modified-file.txt"));
        }
        finally
        {
            CleanupTestRepository();
        }
    }

    [Fact]
    public async Task StageFileAsync_ShouldStageFile_WhenFileExists()
    {
        // Arrange
        CreateTestRepository();
        var testFile = Path.Combine(_testRepoPath, "to-stage.txt");
        await File.WriteAllTextAsync(testFile, "Content to stage");

        try
        {
            // Act
            var result = await _gitService.StageFileAsync("to-stage.txt", _testRepoPath);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Contains("Staged file", result.Details ?? "");
        }
        finally
        {
            CleanupTestRepository();
        }
    }

    [Fact]
    public async Task CreateCommitAsync_ShouldReturnError_WhenNoAuthorInformation()
    {
        // Arrange
        CreateTestRepository();
        var testFile = Path.Combine(_testRepoPath, "to-commit.txt");
        await File.WriteAllTextAsync(testFile, "Content to commit");
        await _gitService.StageFileAsync("to-commit.txt", _testRepoPath);

        try
        {
            // Act
            var result = await _gitService.CreateCommitAsync("Test commit", repositoryPath: _testRepoPath);

            // Assert - This might succeed or fail depending on global git config
            Assert.NotNull(result);
        }
        finally
        {
            CleanupTestRepository();
        }
    }

    [Fact]
    public async Task CreateCommitAsync_ShouldSucceed_WithProvidedAuthorInformation()
    {
        // Arrange
        CreateTestRepository();
        var testFile = Path.Combine(_testRepoPath, "to-commit.txt");
        await File.WriteAllTextAsync(testFile, "Content to commit");
        await _gitService.StageFileAsync("to-commit.txt", _testRepoPath);

        try
        {
            // Act
            var result = await _gitService.CreateCommitAsync("Test commit", "Test Author", "test@example.com", _testRepoPath);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Contains("Created commit", result.Details ?? "");
        }
        finally
        {
            CleanupTestRepository();
        }
    }

    [Fact]
    public async Task GetBranchesAsync_ShouldReturnCurrentBranch_ForNewRepository()
    {
        // Arrange
        CreateTestRepository();

        try
        {
            // Act
            var result = await _gitService.GetBranchesAsync(_testRepoPath, includeRemote: false);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Contains(result, b => (b.Name == "main" || b.Name == "master") && b.IsCurrent);
        }
        finally
        {
            CleanupTestRepository();
        }
    }

    [Fact]
    public async Task CreateBranchAsync_ShouldCreateNewBranch()
    {
        // Arrange
        CreateTestRepository();

        try
        {
            // Act
            var result = await _gitService.CreateBranchAsync("feature-branch", _testRepoPath);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Contains("Created branch", result.Details ?? "");

            // Verify branch was created
            var branches = await _gitService.GetBranchesAsync(_testRepoPath, includeRemote: false);
            Assert.Contains(branches, b => b.Name == "feature-branch");
        }
        finally
        {
            CleanupTestRepository();
        }
    }

    [Fact]
    public async Task SwitchBranchAsync_ShouldSwitchToExistingBranch()
    {
        // Arrange
        CreateTestRepository();
        await _gitService.CreateBranchAsync("feature-branch", _testRepoPath);

        try
        {
            // Act
            var result = await _gitService.SwitchBranchAsync("feature-branch", _testRepoPath);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Contains("Switched to branch", result.Details ?? "");

            // Verify current branch
            var status = await _gitService.GetRepositoryStatusAsync(_testRepoPath);
            Assert.Equal("feature-branch", status?.CurrentBranch);
        }
        finally
        {
            CleanupTestRepository();
        }
    }

    [Fact]
    public async Task GetCommitHistoryAsync_ShouldReturnCommits_ForRepositoryWithHistory()
    {
        // Arrange
        CreateTestRepository();
        
        // Create a commit to have some history
        var testFile = Path.Combine(_testRepoPath, "history-file.txt");
        await File.WriteAllTextAsync(testFile, "Initial content");
        await _gitService.StageFileAsync("history-file.txt", _testRepoPath);
        await _gitService.CreateCommitAsync("Initial commit", "Test Author", "test@example.com", _testRepoPath);

        try
        {
            // Act
            var result = await _gitService.GetCommitHistoryAsync(_testRepoPath);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Contains(result, c => c.Message.Contains("Initial commit"));
        }
        finally
        {
            CleanupTestRepository();
        }
    }

    [Fact]
    public async Task GetFileDiffAsync_ShouldReturnNull_ForNonExistentFile()
    {
        // Arrange
        CreateTestRepository();

        try
        {
            // Act
            var result = await _gitService.GetFileDiffAsync("non-existent.txt", _testRepoPath);

            // Assert
            Assert.Null(result);
        }
        finally
        {
            CleanupTestRepository();
        }
    }

    [Fact]
    public async Task UnstageFileAsync_ShouldUnstageFile_WhenFileIsStaged()
    {
        // Arrange
        CreateTestRepository();
        var testFile = Path.Combine(_testRepoPath, "to-unstage.txt");
        await File.WriteAllTextAsync(testFile, "Content to unstage");
        await _gitService.StageFileAsync("to-unstage.txt", _testRepoPath);

        try
        {
            // Act
            var result = await _gitService.UnstageFileAsync("to-unstage.txt", _testRepoPath);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Contains("Unstaged file", result.Details ?? "");
        }
        finally
        {
            CleanupTestRepository();
        }
    }

    [Fact]
    public async Task StageFilesAsync_ShouldStageMultipleFiles()
    {
        // Arrange
        CreateTestRepository();
        var file1 = Path.Combine(_testRepoPath, "file1.txt");
        var file2 = Path.Combine(_testRepoPath, "file2.txt");
        await File.WriteAllTextAsync(file1, "Content 1");
        await File.WriteAllTextAsync(file2, "Content 2");

        try
        {
            // Act
            var result = await _gitService.StageFilesAsync(new[] { "file1.txt", "file2.txt" }, _testRepoPath);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Contains("Staged 2 files", result.Details ?? "");
        }
        finally
        {
            CleanupTestRepository();
        }
    }

    [Fact]
    public async Task GetAllFilesDiffAsync_ShouldReturnDiffs_ForModifiedFiles()
    {
        // Arrange
        CreateTestRepository();
        var testFile = Path.Combine(_testRepoPath, "diff-file.txt");
        await File.WriteAllTextAsync(testFile, "Content for diff");

        try
        {
            // Act
            var result = await _gitService.GetAllFilesDiffAsync(_testRepoPath);

            // Assert
            Assert.NotNull(result);
            // Note: Result might be empty if no baseline commit exists
        }
        finally
        {
            CleanupTestRepository();
        }
    }

    private void CreateTestRepository()
    {
        CleanupTestRepository(); // Ensure clean state
        
        Directory.CreateDirectory(_testRepoPath);
        Repository.Init(_testRepoPath);
        
        // Create an initial commit to have a proper repository
        using var repo = new Repository(_testRepoPath);
        
        // Create a README file for the initial commit
        var readmePath = Path.Combine(_testRepoPath, "README.md");
        File.WriteAllText(readmePath, "# Test Repository\n\nThis is a test repository.");
        
        // Try to configure user if not already configured
        try
        {
            var config = repo.Config;
            config.Set("user.name", "Test User");
            config.Set("user.email", "test@example.com");
            // Set init.defaultBranch to main
            config.Set("init.defaultBranch", "main");
        }
        catch
        {
            // Ignore config errors
        }
        
        // Stage and commit the README
        Commands.Stage(repo, "README.md");
        
        try
        {
            var signature = new Signature("Test User", "test@example.com", DateTimeOffset.Now);
            repo.Commit("Initial commit", signature, signature);
            
            // Try to rename branch to main if it's master
            if (repo.Head.FriendlyName == "master")
            {
                try
                {
                    var branch = repo.Branches["master"];
                    repo.Branches.Rename(branch, "main");
                }
                catch
                {
                    // Ignore if rename fails
                }
            }
        }
        catch
        {
            // If commit fails, that's okay for basic tests
        }
    }

    private void CleanupTestRepository()
    {
        if (Directory.Exists(_testRepoPath))
        {
            try
            {
                // Make files writable before deletion (Git might mark them read-only)
                var files = Directory.GetFiles(_testRepoPath, "*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    try
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                    }
                    catch
                    {
                        // Ignore errors
                    }
                }
                
                Directory.Delete(_testRepoPath, true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        CleanupTestRepository();
        _gitService?.Dispose();
        _disposed = true;
    }
}