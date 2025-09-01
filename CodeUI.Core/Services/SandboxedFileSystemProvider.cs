using CodeUI.Core.Models;
using System.Security;

namespace CodeUI.Core.Services;

/// <summary>
/// Secure implementation of file system provider that restricts access to designated workspace directories
/// </summary>
public class SandboxedFileSystemProvider : ISandboxedFileSystemProvider, IDisposable
{
    private readonly List<string> _allowedWorkspaces = new();
    private readonly List<FileSystemWatcher> _watchers = new();
    private readonly string[] _hiddenDirectories = { ".git", ".vs", ".vscode", "bin", "obj", "node_modules", ".next" };
    private bool _disposed = false;

    public IReadOnlyList<string> AllowedWorkspaces => _allowedWorkspaces.AsReadOnly();

    public event EventHandler<FileSystemChangeEventArgs>? FileSystemChanged;

    public void SetAllowedWorkspaces(IEnumerable<string> workspacePaths)
    {
        StopWatching();
        _allowedWorkspaces.Clear();
        
        foreach (var path in workspacePaths)
        {
            if (Directory.Exists(path))
            {
                var normalizedPath = Path.GetFullPath(path);
                if (!_allowedWorkspaces.Contains(normalizedPath))
                {
                    _allowedWorkspaces.Add(normalizedPath);
                }
            }
        }
        
        StartWatching();
    }

    public bool IsPathAllowed(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        try
        {
            var normalizedPath = NormalizePath(path);
            
            // Check if path is within any allowed workspace
            return _allowedWorkspaces.Any(workspace => 
                normalizedPath.StartsWith(workspace, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }

    public string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));

        try
        {
            // Normalize path separators - convert backslashes to forward slashes on non-Windows systems
            var normalizedPath = path;
            if (!OperatingSystem.IsWindows())
            {
                normalizedPath = normalizedPath.Replace('\\', '/');
            }
            
            // Check for path traversal attempts before normalization
            if (normalizedPath.Contains(".."))
            {
                throw new SecurityException($"Path traversal detected in path: {path}");
            }
            
            // Get full path to resolve any relative components
            var fullPath = Path.GetFullPath(normalizedPath);
            
            // Additional safety check after normalization
            if (fullPath.Contains(".."))
            {
                throw new SecurityException($"Path traversal detected in normalized path: {fullPath}");
            }
            
            return fullPath;
        }
        catch (SecurityException)
        {
            throw; // Re-throw security exceptions
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Invalid path: {path}", nameof(path), ex);
        }
    }

    public string ResolveSymbolicLink(string path)
    {
        if (!IsPathAllowed(path))
            throw new UnauthorizedAccessException($"Path not allowed: {path}");

        try
        {
            var normalizedPath = NormalizePath(path);
            
            // Resolve symbolic link if it exists
            if (File.Exists(normalizedPath) || Directory.Exists(normalizedPath))
            {
                var info = new FileInfo(normalizedPath);
                if (info.Attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    var resolvedPath = info.ResolveLinkTarget(true)?.FullName ?? normalizedPath;
                    
                    // Ensure resolved path is still within allowed workspaces
                    if (!IsPathAllowed(resolvedPath))
                        throw new UnauthorizedAccessException($"Symbolic link target not allowed: {resolvedPath}");
                        
                    return resolvedPath;
                }
            }
            
            return normalizedPath;
        }
        catch (Exception ex) when (!(ex is UnauthorizedAccessException))
        {
            // Return original path if symbolic link resolution fails
            return path;
        }
    }

    public async Task<IEnumerable<FileSystemItem>> GetSecureDirectoryContentsAsync(string directoryPath)
    {
        if (!IsPathAllowed(directoryPath))
            throw new UnauthorizedAccessException($"Directory not allowed: {directoryPath}");

        var items = new List<FileSystemItem>();
        var resolvedPath = ResolveSymbolicLink(directoryPath);

        try
        {
            if (!Directory.Exists(resolvedPath))
                return items;

            var directoryInfo = new DirectoryInfo(resolvedPath);

            // Get subdirectories
            var subdirectories = directoryInfo.GetDirectories()
                .Where(d => !_hiddenDirectories.Contains(d.Name) && !d.Attributes.HasFlag(FileAttributes.Hidden))
                .Where(d => IsPathAllowed(d.FullName)) // Ensure subdirectories are also allowed
                .OrderBy(d => d.Name);

            foreach (var dir in subdirectories)
            {
                try
                {
                    var item = await CreateFileSystemItemAsync(dir, directoryPath);
                    items.Add(item);
                }
                catch
                {
                    // Skip directories we can't access
                    continue;
                }
            }

            // Get files
            var files = directoryInfo.GetFiles()
                .Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden))
                .Where(f => IsPathAllowed(f.FullName)) // Ensure files are also allowed
                .OrderBy(f => f.Name);

            foreach (var file in files)
            {
                try
                {
                    var item = await CreateFileSystemItemAsync(file, directoryPath);
                    items.Add(item);
                }
                catch
                {
                    // Skip files we can't access
                    continue;
                }
            }
        }
        catch (Exception ex)
        {
            throw new UnauthorizedAccessException($"Error reading directory {directoryPath}: {ex.Message}", ex);
        }

        return items;
    }

    public async Task<FileSystemItem> CreateSecureFileAsync(string directoryPath, string fileName)
    {
        if (!IsPathAllowed(directoryPath))
            throw new UnauthorizedAccessException($"Directory not allowed: {directoryPath}");

        // Validate filename
        if (string.IsNullOrEmpty(fileName) || fileName.Contains("..") || fileName.Contains("/") || fileName.Contains("\\") ||
            Path.GetInvalidFileNameChars().Any(c => fileName.Contains(c)))
            throw new ArgumentException("Invalid filename", nameof(fileName));

        var filePath = Path.Combine(directoryPath, fileName);
        
        if (!IsPathAllowed(filePath))
            throw new UnauthorizedAccessException($"File path not allowed: {filePath}");

        await Task.Yield();

        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

        if (File.Exists(filePath))
            throw new InvalidOperationException($"File already exists: {fileName}");

        await File.WriteAllTextAsync(filePath, string.Empty);

        var fileInfo = new FileInfo(filePath);
        return await CreateFileSystemItemAsync(fileInfo, directoryPath);
    }

    public async Task<FileSystemItem> CreateSecureDirectoryAsync(string parentPath, string directoryName)
    {
        if (!IsPathAllowed(parentPath))
            throw new UnauthorizedAccessException($"Parent directory not allowed: {parentPath}");

        // Validate directory name
        if (string.IsNullOrEmpty(directoryName) || directoryName.Contains("..") || directoryName.Contains("/") || directoryName.Contains("\\") ||
            Path.GetInvalidFileNameChars().Any(c => directoryName.Contains(c)))
            throw new ArgumentException("Invalid directory name", nameof(directoryName));

        var directoryPath = Path.Combine(parentPath, directoryName);
        
        if (!IsPathAllowed(directoryPath))
            throw new UnauthorizedAccessException($"Directory path not allowed: {directoryPath}");

        await Task.Yield();

        if (!Directory.Exists(parentPath))
            throw new DirectoryNotFoundException($"Parent directory not found: {parentPath}");

        if (Directory.Exists(directoryPath))
            throw new InvalidOperationException($"Directory already exists: {directoryName}");

        var directoryInfo = Directory.CreateDirectory(directoryPath);
        return await CreateFileSystemItemAsync(directoryInfo, parentPath);
    }

    public async Task<bool> DeleteSecureAsync(string path)
    {
        if (!IsPathAllowed(path))
            throw new UnauthorizedAccessException($"Path not allowed: {path}");

        await Task.Yield();

        try
        {
            var resolvedPath = ResolveSymbolicLink(path);
            
            if (File.Exists(resolvedPath))
            {
                File.Delete(resolvedPath);
                return true;
            }

            if (Directory.Exists(resolvedPath))
            {
                Directory.Delete(resolvedPath, recursive: true);
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<FileSystemItem> RenameSecureAsync(string currentPath, string newName)
    {
        if (!IsPathAllowed(currentPath))
            throw new UnauthorizedAccessException($"Path not allowed: {currentPath}");

        // Validate new name
        if (string.IsNullOrEmpty(newName) || newName.Contains("..") || newName.Contains("/") || newName.Contains("\\") ||
            Path.GetInvalidFileNameChars().Any(c => newName.Contains(c)))
            throw new ArgumentException("Invalid new name", nameof(newName));

        await Task.Yield();

        var resolvedPath = ResolveSymbolicLink(currentPath);
        
        if (!File.Exists(resolvedPath) && !Directory.Exists(resolvedPath))
            throw new FileNotFoundException($"Path not found: {currentPath}");

        var directory = Path.GetDirectoryName(resolvedPath) ?? throw new InvalidOperationException("Invalid path");
        var newPath = Path.Combine(directory, newName);

        if (!IsPathAllowed(newPath))
            throw new UnauthorizedAccessException($"New path not allowed: {newPath}");

        if (File.Exists(newPath) || Directory.Exists(newPath))
            throw new InvalidOperationException($"Target already exists: {newName}");

        if (File.Exists(resolvedPath))
        {
            File.Move(resolvedPath, newPath);
            var fileInfo = new FileInfo(newPath);
            return await CreateFileSystemItemAsync(fileInfo, directory);
        }
        else
        {
            Directory.Move(resolvedPath, newPath);
            var directoryInfo = new DirectoryInfo(newPath);
            return await CreateFileSystemItemAsync(directoryInfo, directory);
        }
    }

    public void StartWatching()
    {
        StopWatching();

        foreach (var workspace in _allowedWorkspaces)
        {
            try
            {
                var watcher = new FileSystemWatcher(workspace)
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | 
                                 NotifyFilters.Size | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                    IncludeSubdirectories = true,
                    EnableRaisingEvents = true
                };

                watcher.Created += OnFileSystemEvent;
                watcher.Changed += OnFileSystemEvent;
                watcher.Deleted += OnFileSystemEvent;
                watcher.Renamed += OnFileSystemRenamed;

                _watchers.Add(watcher);
            }
            catch
            {
                // Skip workspaces that can't be watched
                continue;
            }
        }
    }

    public void StopWatching()
    {
        foreach (var watcher in _watchers)
        {
            try
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
        }
        _watchers.Clear();
    }

    private void OnFileSystemEvent(object sender, FileSystemEventArgs e)
    {
        if (!IsPathAllowed(e.FullPath))
            return;

        var changeType = e.ChangeType switch
        {
            WatcherChangeTypes.Created => FileSystemChangeType.Created,
            WatcherChangeTypes.Changed => FileSystemChangeType.Modified,
            WatcherChangeTypes.Deleted => FileSystemChangeType.Deleted,
            _ => FileSystemChangeType.Modified
        };

        var args = new FileSystemChangeEventArgs
        {
            FullPath = e.FullPath,
            ChangeType = changeType,
            IsDirectory = Directory.Exists(e.FullPath)
        };

        FileSystemChanged?.Invoke(this, args);
    }

    private void OnFileSystemRenamed(object sender, RenamedEventArgs e)
    {
        if (!IsPathAllowed(e.FullPath) && !IsPathAllowed(e.OldFullPath))
            return;

        var args = new FileSystemChangeEventArgs
        {
            FullPath = e.FullPath,
            OldPath = e.OldFullPath,
            ChangeType = FileSystemChangeType.Renamed,
            IsDirectory = Directory.Exists(e.FullPath)
        };

        FileSystemChanged?.Invoke(this, args);
    }

    private async Task<FileSystemItem> CreateFileSystemItemAsync(FileSystemInfo info, string workingDirectory)
    {
        await Task.Yield();
        
        var isDirectory = info is DirectoryInfo;
        var relativePath = Path.GetRelativePath(workingDirectory, info.FullName);

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
            StopWatching();
            _disposed = true;
        }
    }
}