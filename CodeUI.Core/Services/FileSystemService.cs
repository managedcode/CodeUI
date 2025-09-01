using CodeUI.Core.Models;
using System.Text.RegularExpressions;

namespace CodeUI.Core.Services;

/// <summary>
/// Implementation of file system service for exploring and managing files and directories with sandboxed access
/// </summary>
public class FileSystemService : IFileSystemService, IDisposable
{
    private string _workingDirectory;
    private readonly string[] _hiddenDirectories = { ".git", ".vs", ".vscode", "bin", "obj", "node_modules", ".next" };
    private readonly ISandboxedFileSystemProvider _sandboxedProvider;
    private bool _disposed = false;
    
    public FileSystemService() : this(new SandboxedFileSystemProvider())
    {
    }
    
    public FileSystemService(ISandboxedFileSystemProvider sandboxedProvider)
    {
        _sandboxedProvider = sandboxedProvider ?? throw new ArgumentNullException(nameof(sandboxedProvider));
        _workingDirectory = Directory.GetCurrentDirectory();
        
        // Initialize with current working directory as the allowed workspace
        _sandboxedProvider.SetAllowedWorkspaces(new[] { _workingDirectory });
    }
    
    public async Task<IEnumerable<FileSystemItem>> GetRootDirectoriesAsync()
    {
        await Task.Yield(); // Make it async for consistency
        
        var items = new List<FileSystemItem>();
        
        // Add current working directory as root
        try
        {
            var workingDirInfo = new DirectoryInfo(_workingDirectory);
            var workingDirItem = await CreateFileSystemItemAsync(workingDirInfo);
            items.Add(workingDirItem);
        }
        catch (Exception ex)
        {
            // Log error but continue
            Console.WriteLine($"Error accessing working directory: {ex.Message}");
        }
        
        return items;
    }
    
    public async Task<IEnumerable<FileSystemItem>> GetDirectoryContentsAsync(string directoryPath)
    {
        try
        {
            return await _sandboxedProvider.GetSecureDirectoryContentsAsync(directoryPath);
        }
        catch (UnauthorizedAccessException)
        {
            // Return empty list for unauthorized access instead of throwing
            return new List<FileSystemItem>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading directory {directoryPath}: {ex.Message}");
            return new List<FileSystemItem>();
        }
    }
    
    public async Task<IEnumerable<FileSystemItem>> SearchAsync(string searchPattern, string? basePath = null)
    {
        var items = new List<FileSystemItem>();
        var searchPath = basePath ?? _workingDirectory;
        
        if (string.IsNullOrWhiteSpace(searchPattern))
            return items;
            
        try
        {
            await Task.Run(() =>
            {
                var searchOptions = new EnumerationOptions
                {
                    IgnoreInaccessible = true,
                    RecurseSubdirectories = true,
                    MatchCasing = MatchCasing.CaseInsensitive
                };
                
                // Search for matching files
                var pattern = $"*{searchPattern}*";
                
                try
                {
                    var matchingFiles = Directory.EnumerateFiles(searchPath, pattern, searchOptions)
                        .Take(100) // Limit results for performance
                        .Select(f => new FileInfo(f))
                        .Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden))
                        .Where(f => _sandboxedProvider.IsPathAllowed(f.FullName)); // Only include allowed files
                        
                    foreach (var file in matchingFiles)
                    {
                        try
                        {
                            var item = CreateFileSystemItemSync(file);
                            items.Add(item);
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }
                catch { }
                
                try
                {
                    var matchingDirs = Directory.EnumerateDirectories(searchPath, pattern, searchOptions)
                        .Take(50) // Limit results for performance
                        .Select(d => new DirectoryInfo(d))
                        .Where(d => !_hiddenDirectories.Contains(d.Name) && !d.Attributes.HasFlag(FileAttributes.Hidden))
                        .Where(d => _sandboxedProvider.IsPathAllowed(d.FullName)); // Only include allowed directories
                        
                    foreach (var dir in matchingDirs)
                    {
                        try
                        {
                            var item = CreateFileSystemItemSync(dir);
                            items.Add(item);
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }
                catch { }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error searching for pattern {searchPattern}: {ex.Message}");
        }
        
        return items.OrderBy(i => i.IsDirectory ? 0 : 1).ThenBy(i => i.Name);
    }
    
    public async Task<FileSystemItem> CreateFileAsync(string directoryPath, string fileName)
    {
        return await _sandboxedProvider.CreateSecureFileAsync(directoryPath, fileName);
    }
    
    public async Task<FileSystemItem> CreateDirectoryAsync(string parentPath, string directoryName)
    {
        return await _sandboxedProvider.CreateSecureDirectoryAsync(parentPath, directoryName);
    }
    
    public async Task<FileSystemItem> RenameAsync(string currentPath, string newName)
    {
        return await _sandboxedProvider.RenameSecureAsync(currentPath, newName);
    }
    
    public async Task<bool> DeleteAsync(string path)
    {
        try
        {
            return await _sandboxedProvider.DeleteSecureAsync(path);
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }
    
    public async Task<FileSystemItem?> GetItemInfoAsync(string path)
    {
        await Task.Yield();
        
        try
        {
            // Use sandboxed provider to check if path is allowed
            if (!_sandboxedProvider.IsPathAllowed(path))
                return null;
                
            var resolvedPath = _sandboxedProvider.ResolveSymbolicLink(path);
            
            if (File.Exists(resolvedPath))
            {
                var fileInfo = new FileInfo(resolvedPath);
                return await CreateFileSystemItemAsync(fileInfo);
            }
            
            if (Directory.Exists(resolvedPath))
            {
                var directoryInfo = new DirectoryInfo(resolvedPath);
                return await CreateFileSystemItemAsync(directoryInfo);
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }
    
    public async Task<bool> ExistsAsync(string path)
    {
        await Task.Yield();
        
        try
        {
            if (!_sandboxedProvider.IsPathAllowed(path))
                return false;
                
            var resolvedPath = _sandboxedProvider.ResolveSymbolicLink(path);
            return File.Exists(resolvedPath) || Directory.Exists(resolvedPath);
        }
        catch
        {
            return false;
        }
    }
    
    public string GetWorkingDirectory()
    {
        return _workingDirectory;
    }
    
    public async Task SetWorkingDirectoryAsync(string path)
    {
        await Task.Yield();
        
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Directory not found: {path}");
        
        var normalizedPath = _sandboxedProvider.NormalizePath(path);
        
        // Update working directory and allowed workspaces
        _workingDirectory = normalizedPath;
        _sandboxedProvider.SetAllowedWorkspaces(new[] { _workingDirectory });
    }
    
    private async Task<FileSystemItem> CreateFileSystemItemAsync(FileSystemInfo info)
    {
        await Task.Yield();
        return CreateFileSystemItemSync(info);
    }
    
    private FileSystemItem CreateFileSystemItemSync(FileSystemInfo info)
    {
        var isDirectory = info is DirectoryInfo;
        var relativePath = Path.GetRelativePath(_workingDirectory, info.FullName);
        
        return new FileSystemItem
        {
            Name = info.Name,
            FullPath = info.FullName,
            RelativePath = relativePath,
            IsDirectory = isDirectory,
            Size = isDirectory ? 0 : ((FileInfo)info).Length,
            LastModified = info.LastWriteTime,
            Created = info.CreationTime,
            Extension = isDirectory ? string.Empty : info.Extension,
            IsHidden = info.Attributes.HasFlag(FileAttributes.Hidden),
            IsReadOnly = info.Attributes.HasFlag(FileAttributes.ReadOnly),
            IsLoaded = false
        };
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            _sandboxedProvider?.Dispose();
            _disposed = true;
        }
    }
}