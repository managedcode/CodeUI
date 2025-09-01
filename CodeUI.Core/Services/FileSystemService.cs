using CodeUI.Core.Models;
using System.Text.RegularExpressions;

namespace CodeUI.Core.Services;

/// <summary>
/// Implementation of file system service for exploring and managing files and directories
/// </summary>
public class FileSystemService : IFileSystemService
{
    private string _workingDirectory;
    private readonly string[] _hiddenDirectories = { ".git", ".vs", ".vscode", "bin", "obj", "node_modules", ".next" };
    
    public FileSystemService()
    {
        _workingDirectory = Directory.GetCurrentDirectory();
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
        var items = new List<FileSystemItem>();
        
        try
        {
            if (!Directory.Exists(directoryPath))
                return items;
                
            var directoryInfo = new DirectoryInfo(directoryPath);
            
            // Get subdirectories
            var subdirectories = directoryInfo.GetDirectories()
                .Where(d => !_hiddenDirectories.Contains(d.Name) && !d.Attributes.HasFlag(FileAttributes.Hidden))
                .OrderBy(d => d.Name);
                
            foreach (var dir in subdirectories)
            {
                try
                {
                    var item = await CreateFileSystemItemAsync(dir);
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
                .OrderBy(f => f.Name);
                
            foreach (var file in files)
            {
                try
                {
                    var item = await CreateFileSystemItemAsync(file);
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
            Console.WriteLine($"Error reading directory {directoryPath}: {ex.Message}");
        }
        
        return items;
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
                        .Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden));
                        
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
                        .Where(d => !_hiddenDirectories.Contains(d.Name) && !d.Attributes.HasFlag(FileAttributes.Hidden));
                        
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
        await Task.Yield();
        
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
            
        var filePath = Path.Combine(directoryPath, fileName);
        
        if (File.Exists(filePath))
            throw new InvalidOperationException($"File already exists: {fileName}");
            
        await File.WriteAllTextAsync(filePath, string.Empty);
        
        var fileInfo = new FileInfo(filePath);
        return await CreateFileSystemItemAsync(fileInfo);
    }
    
    public async Task<FileSystemItem> CreateDirectoryAsync(string parentPath, string directoryName)
    {
        await Task.Yield();
        
        if (!Directory.Exists(parentPath))
            throw new DirectoryNotFoundException($"Parent directory not found: {parentPath}");
            
        var directoryPath = Path.Combine(parentPath, directoryName);
        
        if (Directory.Exists(directoryPath))
            throw new InvalidOperationException($"Directory already exists: {directoryName}");
            
        var directoryInfo = Directory.CreateDirectory(directoryPath);
        return await CreateFileSystemItemAsync(directoryInfo);
    }
    
    public async Task<FileSystemItem> RenameAsync(string currentPath, string newName)
    {
        await Task.Yield();
        
        if (!File.Exists(currentPath) && !Directory.Exists(currentPath))
            throw new FileNotFoundException($"Path not found: {currentPath}");
            
        var directory = Path.GetDirectoryName(currentPath) ?? throw new InvalidOperationException("Invalid path");
        var newPath = Path.Combine(directory, newName);
        
        if (File.Exists(newPath) || Directory.Exists(newPath))
            throw new InvalidOperationException($"Target already exists: {newName}");
            
        if (File.Exists(currentPath))
        {
            File.Move(currentPath, newPath);
            var fileInfo = new FileInfo(newPath);
            return await CreateFileSystemItemAsync(fileInfo);
        }
        else
        {
            Directory.Move(currentPath, newPath);
            var directoryInfo = new DirectoryInfo(newPath);
            return await CreateFileSystemItemAsync(directoryInfo);
        }
    }
    
    public async Task<bool> DeleteAsync(string path)
    {
        await Task.Yield();
        
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                return true;
            }
            
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
                return true;
            }
            
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
            if (File.Exists(path))
            {
                var fileInfo = new FileInfo(path);
                return await CreateFileSystemItemAsync(fileInfo);
            }
            
            if (Directory.Exists(path))
            {
                var directoryInfo = new DirectoryInfo(path);
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
        return File.Exists(path) || Directory.Exists(path);
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
            
        _workingDirectory = Path.GetFullPath(path);
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
}