using CodeUI.Core.Models;

namespace CodeUI.Core.Services;

/// <summary>
/// Service for Git operations using LibGit2Sharp
/// </summary>
public interface IGitService : IDisposable
{
    /// <summary>
    /// Gets the current repository status including modified files and branch information
    /// </summary>
    /// <param name="repositoryPath">Path to the Git repository. If null, uses current working directory.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Repository status or null if not a valid Git repository</returns>
    Task<GitRepositoryStatus?> GetRepositoryStatusAsync(string? repositoryPath = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the diff for a specific file
    /// </summary>
    /// <param name="filePath">Path to the file relative to repository root</param>
    /// <param name="repositoryPath">Path to the Git repository. If null, uses current working directory.</param>
    /// <param name="staged">Whether to get staged diff (true) or working directory diff (false)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File diff information</returns>
    Task<GitFileDiff?> GetFileDiffAsync(string filePath, string? repositoryPath = null, bool staged = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets diffs for all modified files
    /// </summary>
    /// <param name="repositoryPath">Path to the Git repository. If null, uses current working directory.</param>
    /// <param name="staged">Whether to get staged diffs (true) or working directory diffs (false)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of file diffs</returns>
    Task<List<GitFileDiff>> GetAllFilesDiffAsync(string? repositoryPath = null, bool staged = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stages (adds) a file to the index
    /// </summary>
    /// <param name="filePath">Path to the file relative to repository root</param>
    /// <param name="repositoryPath">Path to the Git repository. If null, uses current working directory.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<GitOperationResult> StageFileAsync(string filePath, string? repositoryPath = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stages multiple files to the index
    /// </summary>
    /// <param name="filePaths">Paths to the files relative to repository root</param>
    /// <param name="repositoryPath">Path to the Git repository. If null, uses current working directory.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<GitOperationResult> StageFilesAsync(IEnumerable<string> filePaths, string? repositoryPath = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Unstages a file from the index
    /// </summary>
    /// <param name="filePath">Path to the file relative to repository root</param>
    /// <param name="repositoryPath">Path to the Git repository. If null, uses current working directory.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<GitOperationResult> UnstageFileAsync(string filePath, string? repositoryPath = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Unstages multiple files from the index
    /// </summary>
    /// <param name="filePaths">Paths to the files relative to repository root</param>
    /// <param name="repositoryPath">Path to the Git repository. If null, uses current working directory.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<GitOperationResult> UnstageFilesAsync(IEnumerable<string> filePaths, string? repositoryPath = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a commit with the currently staged changes
    /// </summary>
    /// <param name="message">Commit message</param>
    /// <param name="authorName">Author name (if null, uses Git config)</param>
    /// <param name="authorEmail">Author email (if null, uses Git config)</param>
    /// <param name="repositoryPath">Path to the Git repository. If null, uses current working directory.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result with commit SHA if successful</returns>
    Task<GitOperationResult> CreateCommitAsync(string message, string? authorName = null, string? authorEmail = null, string? repositoryPath = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all branches in the repository
    /// </summary>
    /// <param name="repositoryPath">Path to the Git repository. If null, uses current working directory.</param>
    /// <param name="includeRemote">Whether to include remote branches</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of branches</returns>
    Task<List<GitBranch>> GetBranchesAsync(string? repositoryPath = null, bool includeRemote = true, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Switches to the specified branch
    /// </summary>
    /// <param name="branchName">Name of the branch to switch to</param>
    /// <param name="repositoryPath">Path to the Git repository. If null, uses current working directory.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<GitOperationResult> SwitchBranchAsync(string branchName, string? repositoryPath = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new branch
    /// </summary>
    /// <param name="branchName">Name of the new branch</param>
    /// <param name="repositoryPath">Path to the Git repository. If null, uses current working directory.</param>
    /// <param name="switchToBranch">Whether to switch to the new branch after creating it</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<GitOperationResult> CreateBranchAsync(string branchName, string? repositoryPath = null, bool switchToBranch = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the commit history for the repository
    /// </summary>
    /// <param name="repositoryPath">Path to the Git repository. If null, uses current working directory.</param>
    /// <param name="maxCount">Maximum number of commits to retrieve (0 for all)</param>
    /// <param name="branchName">Branch to get history for (null for current branch)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of commits ordered by date (newest first)</returns>
    Task<List<GitCommit>> GetCommitHistoryAsync(string? repositoryPath = null, int maxCount = 100, string? branchName = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets details of a specific commit
    /// </summary>
    /// <param name="commitSha">SHA of the commit</param>
    /// <param name="repositoryPath">Path to the Git repository. If null, uses current working directory.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Commit details or null if not found</returns>
    Task<GitCommit?> GetCommitAsync(string commitSha, string? repositoryPath = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if the specified path is a Git repository
    /// </summary>
    /// <param name="repositoryPath">Path to check. If null, uses current working directory.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if it's a valid Git repository</returns>
    Task<bool> IsGitRepositoryAsync(string? repositoryPath = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds the Git repository root directory starting from the specified path
    /// </summary>
    /// <param name="startPath">Path to start searching from. If null, uses current working directory.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Path to the Git repository root or null if not found</returns>
    Task<string?> FindRepositoryRootAsync(string? startPath = null, CancellationToken cancellationToken = default);
}