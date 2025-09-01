namespace CodeUI.Core.Models;

/// <summary>
/// Represents the status of a Git repository
/// </summary>
public class GitRepositoryStatus
{
    /// <summary>
    /// Path to the repository
    /// </summary>
    public string RepositoryPath { get; set; } = string.Empty;
    
    /// <summary>
    /// Current branch name
    /// </summary>
    public string CurrentBranch { get; set; } = string.Empty;
    
    /// <summary>
    /// Is the repository dirty (has unstaged/uncommitted changes)
    /// </summary>
    public bool IsDirty { get; set; }
    
    /// <summary>
    /// Files with changes
    /// </summary>
    public List<GitFileStatus> ModifiedFiles { get; set; } = new();
    
    /// <summary>
    /// Number of commits ahead of remote
    /// </summary>
    public int AheadCount { get; set; }
    
    /// <summary>
    /// Number of commits behind remote
    /// </summary>
    public int BehindCount { get; set; }
}

/// <summary>
/// Represents the status of a file in Git
/// </summary>
public class GitFileStatus
{
    /// <summary>
    /// File path relative to repository root
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
    
    /// <summary>
    /// Current status of the file
    /// </summary>
    public GitFileState State { get; set; }
    
    /// <summary>
    /// File status in the index (staged area)
    /// </summary>
    public GitFileState IndexState { get; set; }
    
    /// <summary>
    /// File status in the working directory
    /// </summary>
    public GitFileState WorkingDirectoryState { get; set; }
}

/// <summary>
/// Represents possible Git file states
/// </summary>
public enum GitFileState
{
    Unmodified,
    Added,
    Modified,
    Deleted,
    Renamed,
    Copied,
    Untracked,
    Ignored
}

/// <summary>
/// Represents a Git commit
/// </summary>
public class GitCommit
{
    /// <summary>
    /// Commit SHA hash
    /// </summary>
    public string Sha { get; set; } = string.Empty;
    
    /// <summary>
    /// Commit message
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Author name
    /// </summary>
    public string AuthorName { get; set; } = string.Empty;
    
    /// <summary>
    /// Author email
    /// </summary>
    public string AuthorEmail { get; set; } = string.Empty;
    
    /// <summary>
    /// Commit date
    /// </summary>
    public DateTime Date { get; set; }
    
    /// <summary>
    /// Parent commit SHAs
    /// </summary>
    public List<string> ParentShas { get; set; } = new();
}

/// <summary>
/// Represents a Git branch
/// </summary>
public class GitBranch
{
    /// <summary>
    /// Branch name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Is this the current branch
    /// </summary>
    public bool IsCurrent { get; set; }
    
    /// <summary>
    /// Is this a remote branch
    /// </summary>
    public bool IsRemote { get; set; }
    
    /// <summary>
    /// Tip commit SHA
    /// </summary>
    public string TipSha { get; set; } = string.Empty;
}

/// <summary>
/// Represents a Git diff for a file
/// </summary>
public class GitFileDiff
{
    /// <summary>
    /// Path of the file
    /// </summary>
    public string Path { get; set; } = string.Empty;
    
    /// <summary>
    /// Old file path (for renames)
    /// </summary>
    public string? OldPath { get; set; }
    
    /// <summary>
    /// Change type
    /// </summary>
    public GitChangeType ChangeType { get; set; }
    
    /// <summary>
    /// Number of lines added
    /// </summary>
    public int LinesAdded { get; set; }
    
    /// <summary>
    /// Number of lines deleted
    /// </summary>
    public int LinesDeleted { get; set; }
    
    /// <summary>
    /// Patch content (unified diff)
    /// </summary>
    public string Patch { get; set; } = string.Empty;
}

/// <summary>
/// Represents the type of change in a Git diff
/// </summary>
public enum GitChangeType
{
    Added,
    Deleted,
    Modified,
    Renamed,
    Copied,
    Unmodified
}

/// <summary>
/// Result of a Git operation
/// </summary>
public class GitOperationResult
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// Error message if operation failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Additional details about the operation
    /// </summary>
    public string? Details { get; set; }
    
    /// <summary>
    /// Create a successful result
    /// </summary>
    public static GitOperationResult Success(string? details = null) => new()
    {
        IsSuccess = true,
        Details = details
    };
    
    /// <summary>
    /// Create a failed result
    /// </summary>
    public static GitOperationResult Failure(string errorMessage, string? details = null) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage,
        Details = details
    };
}