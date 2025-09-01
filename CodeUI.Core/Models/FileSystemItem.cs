namespace CodeUI.Core.Models;

/// <summary>
/// Represents a file or directory in the file system
/// </summary>
public class FileSystemItem
{
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public bool IsDirectory { get; set; }
    public long Size { get; set; }
    public DateTime LastModified { get; set; }
    public DateTime Created { get; set; }
    public string Extension { get; set; } = string.Empty;
    public bool IsHidden { get; set; }
    public bool IsReadOnly { get; set; }
    public List<FileSystemItem> Children { get; set; } = new();
    public bool IsExpanded { get; set; }
    public bool IsLoaded { get; set; }
    
    /// <summary>
    /// Gets the icon class for this file system item based on type and extension
    /// </summary>
    public string IconClass => GetIconClass();
    
    /// <summary>
    /// Gets formatted file size for display
    /// </summary>
    public string FormattedSize => FormatFileSize(Size);
    
    /// <summary>
    /// Gets relative time string for last modification
    /// </summary>
    public string RelativeModifiedTime => GetRelativeTime(LastModified);

    private string GetIconClass()
    {
        if (IsDirectory)
            return IsExpanded ? "fas fa-folder-open" : "fas fa-folder";
            
        return Extension.ToLowerInvariant() switch
        {
            ".cs" => "fas fa-file-code text-info",
            ".razor" => "fas fa-file-code text-primary",
            ".html" or ".htm" => "fab fa-html5 text-warning",
            ".css" => "fab fa-css3-alt text-info",
            ".js" or ".ts" => "fab fa-js-square text-warning",
            ".json" => "fas fa-brackets-curly text-info",
            ".xml" => "fas fa-file-code text-success",
            ".md" => "fab fa-markdown text-secondary",
            ".txt" => "fas fa-file-alt text-secondary",
            ".pdf" => "fas fa-file-pdf text-danger",
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".svg" => "fas fa-file-image text-success",
            ".zip" or ".rar" or ".7z" => "fas fa-file-archive text-warning",
            ".exe" or ".dll" => "fas fa-cog text-info",
            ".config" => "fas fa-cogs text-warning",
            ".sln" or ".slnx" => "fas fa-project-diagram text-primary",
            ".csproj" or ".vbproj" or ".fsproj" => "fas fa-file-code text-success",
            _ => "fas fa-file text-muted"
        };
    }
    
    private static string FormatFileSize(long bytes)
    {
        if (bytes == 0) return "0 B";
        
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int suffixIndex = 0;
        double size = bytes;
        
        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }
        
        return $"{size:F1} {suffixes[suffixIndex]}";
    }
    
    private static string GetRelativeTime(DateTime dateTime)
    {
        var timeSpan = DateTime.Now - dateTime;
        
        return timeSpan.TotalDays switch
        {
            < 1 when timeSpan.TotalHours < 1 => $"{(int)timeSpan.TotalMinutes} minutes ago",
            < 1 => $"{(int)timeSpan.TotalHours} hours ago", 
            < 7 => $"{(int)timeSpan.TotalDays} days ago",
            _ => dateTime.ToString("MMM dd, yyyy")
        };
    }
}