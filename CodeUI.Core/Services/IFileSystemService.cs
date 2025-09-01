using CodeUI.Core.Models;

namespace CodeUI.Core.Services;

/// <summary>
/// Service for file system operations and file exploration
/// </summary>
public interface IFileSystemService : IDisposable
{
    /// <summary>
    /// Gets the root directories available for exploration
    /// </summary>
    Task<IEnumerable<FileSystemItem>> GetRootDirectoriesAsync();
    
    /// <summary>
    /// Gets the children of a specific directory
    /// </summary>
    Task<IEnumerable<FileSystemItem>> GetDirectoryContentsAsync(string directoryPath);
    
    /// <summary>
    /// Searches for files and directories matching the given pattern
    /// </summary>
    Task<IEnumerable<FileSystemItem>> SearchAsync(string searchPattern, string? basePath = null);
    
    /// <summary>
    /// Creates a new file in the specified directory
    /// </summary>
    Task<FileSystemItem> CreateFileAsync(string directoryPath, string fileName);
    
    /// <summary>
    /// Creates a new directory in the specified path
    /// </summary>
    Task<FileSystemItem> CreateDirectoryAsync(string parentPath, string directoryName);
    
    /// <summary>
    /// Renames a file or directory
    /// </summary>
    Task<FileSystemItem> RenameAsync(string currentPath, string newName);
    
    /// <summary>
    /// Deletes a file or directory
    /// </summary>
    Task<bool> DeleteAsync(string path);
    
    /// <summary>
    /// Gets detailed information about a file or directory
    /// </summary>
    Task<FileSystemItem?> GetItemInfoAsync(string path);
    
    /// <summary>
    /// Checks if a path exists and is accessible
    /// </summary>
    Task<bool> ExistsAsync(string path);
    
    /// <summary>
    /// Gets the working directory for file operations
    /// </summary>
    string GetWorkingDirectory();
    
    /// <summary>
    /// Sets the working directory for file operations
    /// </summary>
    Task SetWorkingDirectoryAsync(string path);
}