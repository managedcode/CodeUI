using CodeUI.Core.Models;
using LibGit2Sharp;

namespace CodeUI.Core.Services;

/// <summary>
/// Implementation of Git operations using LibGit2Sharp
/// </summary>
public class GitService : IGitService
{
    private readonly string _defaultWorkingDirectory;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of GitService
    /// </summary>
    public GitService()
    {
        _defaultWorkingDirectory = Directory.GetCurrentDirectory();
    }

    /// <summary>
    /// Gets the current repository status including modified files and branch information
    /// </summary>
    public async Task<GitRepositoryStatus?> GetRepositoryStatusAsync(string? repositoryPath = null, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var repoPath = GetRepositoryPath(repositoryPath);
                if (repoPath == null) return null;

                using var repo = new Repository(repoPath);
                var status = repo.RetrieveStatus();

                var gitStatus = new GitRepositoryStatus
                {
                    RepositoryPath = repoPath,
                    CurrentBranch = repo.Head.FriendlyName,
                    IsDirty = status.IsDirty
                };

                // Convert LibGit2Sharp status entries to our model
                foreach (var statusEntry in status)
                {
                    var fileStatus = new GitFileStatus
                    {
                        FilePath = statusEntry.FilePath,
                        State = ConvertToGitFileState(statusEntry.State),
                        IndexState = ConvertToGitFileState(GetIndexState(statusEntry.State)),
                        WorkingDirectoryState = ConvertToGitFileState(GetWorkingDirectoryState(statusEntry.State))
                    };
                    
                    gitStatus.ModifiedFiles.Add(fileStatus);
                }

                // Get ahead/behind count if there's a tracking branch
                if (repo.Head.TrackedBranch != null)
                {
                    var divergence = repo.ObjectDatabase.CalculateHistoryDivergence(repo.Head.Tip, repo.Head.TrackedBranch.Tip);
                    gitStatus.AheadCount = divergence.AheadBy ?? 0;
                    gitStatus.BehindCount = divergence.BehindBy ?? 0;
                }

                return gitStatus;
            }
            catch (RepositoryNotFoundException)
            {
                return null;
            }
            catch (Exception)
            {
                // Log exception in real implementation
                return null;
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Gets the diff for a specific file
    /// </summary>
    public async Task<GitFileDiff?> GetFileDiffAsync(string filePath, string? repositoryPath = null, bool staged = false, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var repoPath = GetRepositoryPath(repositoryPath);
                if (repoPath == null) return null;

                using var repo = new Repository(repoPath);
                
                // For now, use a simplified approach for staged diffs
                var patch = repo.Diff.Compare<Patch>(repo.Head.Tip?.Tree, DiffTargets.WorkingDirectory, new[] { filePath });

                var patchEntry = patch.FirstOrDefault();
                if (patchEntry == null) return null;

                return new GitFileDiff
                {
                    Path = patchEntry.Path,
                    OldPath = patchEntry.OldPath != patchEntry.Path ? patchEntry.OldPath : null,
                    ChangeType = ConvertToGitChangeType(patchEntry.Status),
                    LinesAdded = patchEntry.LinesAdded,
                    LinesDeleted = patchEntry.LinesDeleted,
                    Patch = patchEntry.Patch
                };
            }
            catch (Exception)
            {
                // Log exception in real implementation
                return null;
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Gets diffs for all modified files
    /// </summary>
    public async Task<List<GitFileDiff>> GetAllFilesDiffAsync(string? repositoryPath = null, bool staged = false, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var repoPath = GetRepositoryPath(repositoryPath);
                if (repoPath == null) return new List<GitFileDiff>();

                using var repo = new Repository(repoPath);
                
                // For now, use a simplified approach for staged diffs
                var patch = repo.Diff.Compare<Patch>(repo.Head.Tip?.Tree, DiffTargets.WorkingDirectory);

                var diffs = new List<GitFileDiff>();
                foreach (var patchEntry in patch)
                {
                    var diff = new GitFileDiff
                    {
                        Path = patchEntry.Path,
                        OldPath = patchEntry.OldPath != patchEntry.Path ? patchEntry.OldPath : null,
                        ChangeType = ConvertToGitChangeType(patchEntry.Status),
                        LinesAdded = patchEntry.LinesAdded,
                        LinesDeleted = patchEntry.LinesDeleted,
                        Patch = patchEntry.Patch
                    };
                    
                    diffs.Add(diff);
                }

                return diffs;
            }
            catch (Exception)
            {
                // Log exception in real implementation
                return new List<GitFileDiff>();
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Stages (adds) a file to the index
    /// </summary>
    public async Task<GitOperationResult> StageFileAsync(string filePath, string? repositoryPath = null, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var repoPath = GetRepositoryPath(repositoryPath);
                if (repoPath == null) 
                    return GitOperationResult.Failure("Repository not found");

                using var repo = new Repository(repoPath);
                Commands.Stage(repo, filePath);
                
                return GitOperationResult.Success($"Staged file: {filePath}");
            }
            catch (Exception ex)
            {
                return GitOperationResult.Failure($"Failed to stage file: {ex.Message}");
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Stages multiple files to the index
    /// </summary>
    public async Task<GitOperationResult> StageFilesAsync(IEnumerable<string> filePaths, string? repositoryPath = null, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var repoPath = GetRepositoryPath(repositoryPath);
                if (repoPath == null) 
                    return GitOperationResult.Failure("Repository not found");

                using var repo = new Repository(repoPath);
                var fileList = filePaths.ToList();
                Commands.Stage(repo, fileList);
                
                return GitOperationResult.Success($"Staged {fileList.Count} files");
            }
            catch (Exception ex)
            {
                return GitOperationResult.Failure($"Failed to stage files: {ex.Message}");
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Unstages a file from the index
    /// </summary>
    public async Task<GitOperationResult> UnstageFileAsync(string filePath, string? repositoryPath = null, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var repoPath = GetRepositoryPath(repositoryPath);
                if (repoPath == null) 
                    return GitOperationResult.Failure("Repository not found");

                using var repo = new Repository(repoPath);
                Commands.Unstage(repo, filePath);
                
                return GitOperationResult.Success($"Unstaged file: {filePath}");
            }
            catch (Exception ex)
            {
                return GitOperationResult.Failure($"Failed to unstage file: {ex.Message}");
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Unstages multiple files from the index
    /// </summary>
    public async Task<GitOperationResult> UnstageFilesAsync(IEnumerable<string> filePaths, string? repositoryPath = null, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var repoPath = GetRepositoryPath(repositoryPath);
                if (repoPath == null) 
                    return GitOperationResult.Failure("Repository not found");

                using var repo = new Repository(repoPath);
                var fileList = filePaths.ToList();
                Commands.Unstage(repo, fileList);
                
                return GitOperationResult.Success($"Unstaged {fileList.Count} files");
            }
            catch (Exception ex)
            {
                return GitOperationResult.Failure($"Failed to unstage files: {ex.Message}");
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Creates a commit with the currently staged changes
    /// </summary>
    public async Task<GitOperationResult> CreateCommitAsync(string message, string? authorName = null, string? authorEmail = null, string? repositoryPath = null, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var repoPath = GetRepositoryPath(repositoryPath);
                if (repoPath == null) 
                    return GitOperationResult.Failure("Repository not found");

                using var repo = new Repository(repoPath);
                
                // Get author information
                var signature = GetSignature(repo, authorName, authorEmail);
                if (signature == null)
                    return GitOperationResult.Failure("Unable to determine author information. Please configure Git user.name and user.email.");

                var commit = repo.Commit(message, signature, signature);
                
                return GitOperationResult.Success($"Created commit: {commit.Sha[..8]}");
            }
            catch (Exception ex)
            {
                return GitOperationResult.Failure($"Failed to create commit: {ex.Message}");
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Gets all branches in the repository
    /// </summary>
    public async Task<List<GitBranch>> GetBranchesAsync(string? repositoryPath = null, bool includeRemote = true, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var repoPath = GetRepositoryPath(repositoryPath);
                if (repoPath == null) return new List<GitBranch>();

                using var repo = new Repository(repoPath);
                var branches = new List<GitBranch>();

                foreach (var branch in repo.Branches)
                {
                    if (!includeRemote && branch.IsRemote) continue;

                    var gitBranch = new GitBranch
                    {
                        Name = branch.FriendlyName,
                        IsCurrent = branch.IsCurrentRepositoryHead,
                        IsRemote = branch.IsRemote,
                        TipSha = branch.Tip?.Sha ?? string.Empty
                    };
                    
                    branches.Add(gitBranch);
                }

                return branches;
            }
            catch (Exception)
            {
                // Log exception in real implementation
                return new List<GitBranch>();
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Switches to the specified branch
    /// </summary>
    public async Task<GitOperationResult> SwitchBranchAsync(string branchName, string? repositoryPath = null, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var repoPath = GetRepositoryPath(repositoryPath);
                if (repoPath == null) 
                    return GitOperationResult.Failure("Repository not found");

                using var repo = new Repository(repoPath);
                var branch = repo.Branches[branchName];
                
                if (branch == null)
                    return GitOperationResult.Failure($"Branch '{branchName}' not found");

                Commands.Checkout(repo, branch);
                
                return GitOperationResult.Success($"Switched to branch: {branchName}");
            }
            catch (Exception ex)
            {
                return GitOperationResult.Failure($"Failed to switch branch: {ex.Message}");
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Creates a new branch
    /// </summary>
    public async Task<GitOperationResult> CreateBranchAsync(string branchName, string? repositoryPath = null, bool switchToBranch = false, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var repoPath = GetRepositoryPath(repositoryPath);
                if (repoPath == null) 
                    return GitOperationResult.Failure("Repository not found");

                using var repo = new Repository(repoPath);
                var branch = repo.CreateBranch(branchName);
                
                if (switchToBranch)
                {
                    Commands.Checkout(repo, branch);
                    return GitOperationResult.Success($"Created and switched to branch: {branchName}");
                }
                
                return GitOperationResult.Success($"Created branch: {branchName}");
            }
            catch (Exception ex)
            {
                return GitOperationResult.Failure($"Failed to create branch: {ex.Message}");
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Gets the commit history for the repository
    /// </summary>
    public async Task<List<GitCommit>> GetCommitHistoryAsync(string? repositoryPath = null, int maxCount = 100, string? branchName = null, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var repoPath = GetRepositoryPath(repositoryPath);
                if (repoPath == null) return new List<GitCommit>();

                using var repo = new Repository(repoPath);
                
                var startCommit = string.IsNullOrEmpty(branchName) 
                    ? repo.Head.Tip 
                    : repo.Branches[branchName]?.Tip;
                
                if (startCommit == null) return new List<GitCommit>();

                var filter = new CommitFilter
                {
                    IncludeReachableFrom = startCommit,
                    FirstParentOnly = false
                };

                var commits = new List<GitCommit>();
                var count = 0;
                
                foreach (var commit in repo.Commits.QueryBy(filter))
                {
                    if (maxCount > 0 && count >= maxCount) break;

                    var gitCommit = new GitCommit
                    {
                        Sha = commit.Sha,
                        Message = commit.Message,
                        AuthorName = commit.Author.Name,
                        AuthorEmail = commit.Author.Email,
                        Date = commit.Author.When.DateTime,
                        ParentShas = commit.Parents.Select(p => p.Sha).ToList()
                    };
                    
                    commits.Add(gitCommit);
                    count++;
                }

                return commits;
            }
            catch (Exception)
            {
                // Log exception in real implementation
                return new List<GitCommit>();
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Gets details of a specific commit
    /// </summary>
    public async Task<GitCommit?> GetCommitAsync(string commitSha, string? repositoryPath = null, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var repoPath = GetRepositoryPath(repositoryPath);
                if (repoPath == null) return null;

                using var repo = new Repository(repoPath);
                var commit = repo.Lookup<Commit>(commitSha);
                
                if (commit == null) return null;

                return new GitCommit
                {
                    Sha = commit.Sha,
                    Message = commit.Message,
                    AuthorName = commit.Author.Name,
                    AuthorEmail = commit.Author.Email,
                    Date = commit.Author.When.DateTime,
                    ParentShas = commit.Parents.Select(p => p.Sha).ToList()
                };
            }
            catch (Exception)
            {
                // Log exception in real implementation
                return null;
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Checks if the specified path is a Git repository
    /// </summary>
    public async Task<bool> IsGitRepositoryAsync(string? repositoryPath = null, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var path = repositoryPath ?? _defaultWorkingDirectory;
                return Repository.IsValid(path);
            }
            catch
            {
                return false;
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Finds the Git repository root directory starting from the specified path
    /// </summary>
    public async Task<string?> FindRepositoryRootAsync(string? startPath = null, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var path = startPath ?? _defaultWorkingDirectory;
                return Repository.Discover(path);
            }
            catch
            {
                return null;
            }
        }, cancellationToken);
    }

    private string? GetRepositoryPath(string? repositoryPath)
    {
        var path = repositoryPath ?? _defaultWorkingDirectory;
        return Repository.IsValid(path) ? path : Repository.Discover(path);
    }

    private static Signature? GetSignature(Repository repo, string? authorName, string? authorEmail)
    {
        try
        {
            // Use provided values if available
            if (!string.IsNullOrEmpty(authorName) && !string.IsNullOrEmpty(authorEmail))
            {
                return new Signature(authorName, authorEmail, DateTimeOffset.Now);
            }

            // Try to get from repository configuration
            var config = repo.Config;
            var configName = config.Get<string>("user.name")?.Value;
            var configEmail = config.Get<string>("user.email")?.Value;

            if (!string.IsNullOrEmpty(configName) && !string.IsNullOrEmpty(configEmail))
            {
                return new Signature(configName, configEmail, DateTimeOffset.Now);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static GitFileState ConvertToGitFileState(FileStatus status)
    {
        return status switch
        {
            FileStatus.NewInIndex or FileStatus.NewInWorkdir => GitFileState.Added,
            FileStatus.ModifiedInIndex or FileStatus.ModifiedInWorkdir => GitFileState.Modified,
            FileStatus.DeletedFromIndex or FileStatus.DeletedFromWorkdir => GitFileState.Deleted,
            FileStatus.RenamedInIndex or FileStatus.RenamedInWorkdir => GitFileState.Renamed,
            FileStatus.TypeChangeInIndex or FileStatus.TypeChangeInWorkdir => GitFileState.Modified,
            FileStatus.Ignored => GitFileState.Ignored,
            FileStatus.Nonexistent => GitFileState.Untracked,
            _ => GitFileState.Unmodified
        };
    }

    private static FileStatus GetIndexState(FileStatus status)
    {
        // Extract index-related status
        return status & (FileStatus.NewInIndex | FileStatus.ModifiedInIndex | 
                        FileStatus.DeletedFromIndex | FileStatus.RenamedInIndex | 
                        FileStatus.TypeChangeInIndex);
    }

    private static FileStatus GetWorkingDirectoryState(FileStatus status)
    {
        // Extract working directory-related status
        return status & (FileStatus.NewInWorkdir | FileStatus.ModifiedInWorkdir | 
                        FileStatus.DeletedFromWorkdir | FileStatus.RenamedInWorkdir | 
                        FileStatus.TypeChangeInWorkdir);
    }

    private static GitChangeType ConvertToGitChangeType(ChangeKind changeKind)
    {
        return changeKind switch
        {
            ChangeKind.Added => GitChangeType.Added,
            ChangeKind.Deleted => GitChangeType.Deleted,
            ChangeKind.Modified => GitChangeType.Modified,
            ChangeKind.Renamed => GitChangeType.Renamed,
            ChangeKind.Copied => GitChangeType.Copied,
            _ => GitChangeType.Unmodified
        };
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}