using CodeUI.Core.Models;

namespace CodeUI.Core.Services;

/// <summary>
/// Secure file system provider that restricts access to designated workspace directories
/// </summary>
public interface ISandboxedFileSystemProvider : IDisposable
{
    /// <summary>
    /// Gets the allowed workspace directories
    /// </summary>
    IReadOnlyList<string> AllowedWorkspaces { get; }
    
    /// <summary>
    /// Sets the allowed workspace directories
    /// </summary>
    void SetAllowedWorkspaces(IEnumerable<string> workspacePaths);
    
    /// <summary>
    /// Validates if a path is within allowed workspace boundaries
    /// </summary>
    bool IsPathAllowed(string path);
    
    /// <summary>
    /// Normalizes and validates a path to prevent traversal attacks
    /// </summary>
    string NormalizePath(string path);
    
    /// <summary>
    /// Safely resolves symbolic links within workspace boundaries
    /// </summary>
    string ResolveSymbolicLink(string path);
    
    /// <summary>
    /// Gets directory contents within workspace boundaries
    /// </summary>
    Task<IEnumerable<FileSystemItem>> GetSecureDirectoryContentsAsync(string directoryPath);
    
    /// <summary>
    /// Creates a file within workspace boundaries
    /// </summary>
    Task<FileSystemItem> CreateSecureFileAsync(string directoryPath, string fileName);
    
    /// <summary>
    /// Creates a directory within workspace boundaries
    /// </summary>
    Task<FileSystemItem> CreateSecureDirectoryAsync(string parentPath, string directoryName);
    
    /// <summary>
    /// Deletes a file or directory within workspace boundaries
    /// </summary>
    Task<bool> DeleteSecureAsync(string path);
    
    /// <summary>
    /// Renames a file or directory within workspace boundaries
    /// </summary>
    Task<FileSystemItem> RenameSecureAsync(string currentPath, string newName);
    
    /// <summary>
    /// Starts watching for file system changes within workspaces
    /// </summary>
    void StartWatching();
    
    /// <summary>
    /// Stops watching for file system changes
    /// </summary>
    void StopWatching();
    
    /// <summary>
    /// Event raised when files or directories change within workspaces
    /// </summary>
    event EventHandler<FileSystemChangeEventArgs> FileSystemChanged;
}

/// <summary>
/// Event arguments for file system change notifications
/// </summary>
public class FileSystemChangeEventArgs : EventArgs
{
    public string FullPath { get; init; } = string.Empty;
    public string? OldPath { get; init; }
    public FileSystemChangeType ChangeType { get; init; }
    public bool IsDirectory { get; init; }
}

/// <summary>
/// Types of file system changes
/// </summary>
public enum FileSystemChangeType
{
    Created,
    Modified,
    Deleted,
    Renamed
}