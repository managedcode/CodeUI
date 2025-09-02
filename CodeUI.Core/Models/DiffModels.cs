using System.IO;

namespace CodeUI.Core.Models;

/// <summary>
/// Represents a diff view mode
/// </summary>
public enum DiffViewMode
{
    /// <summary>
    /// Side-by-side view showing old and new versions side by side
    /// </summary>
    SideBySide,
    
    /// <summary>
    /// Unified view showing changes in a single column
    /// </summary>
    Unified
}

/// <summary>
/// Represents a single line in a diff view
/// </summary>
public class DiffLine
{
    /// <summary>
    /// Line number in the old file (null for added lines)
    /// </summary>
    public int? OldLineNumber { get; set; }
    
    /// <summary>
    /// Line number in the new file (null for deleted lines)
    /// </summary>
    public int? NewLineNumber { get; set; }
    
    /// <summary>
    /// Content of the line
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of change for this line
    /// </summary>
    public DiffLineType Type { get; set; }
    
    /// <summary>
    /// Whether this line has been accepted by the user
    /// </summary>
    public bool IsAccepted { get; set; }
    
    /// <summary>
    /// Whether this line has been rejected by the user
    /// </summary>
    public bool IsRejected { get; set; }
    
    /// <summary>
    /// Unique identifier for this line
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
}

/// <summary>
/// Type of change for a diff line
/// </summary>
public enum DiffLineType
{
    /// <summary>
    /// Line exists in both old and new files (no change)
    /// </summary>
    Unchanged,
    
    /// <summary>
    /// Line was added in the new file
    /// </summary>
    Added,
    
    /// <summary>
    /// Line was deleted from the old file
    /// </summary>
    Deleted,
    
    /// <summary>
    /// Line was modified between old and new files
    /// </summary>
    Modified
}

/// <summary>
/// A chunk of related diff lines
/// </summary>
public class DiffChunk
{
    /// <summary>
    /// Starting line number in the old file
    /// </summary>
    public int OldStartLine { get; set; }
    
    /// <summary>
    /// Number of lines in the old file
    /// </summary>
    public int OldLineCount { get; set; }
    
    /// <summary>
    /// Starting line number in the new file
    /// </summary>
    public int NewStartLine { get; set; }
    
    /// <summary>
    /// Number of lines in the new file
    /// </summary>
    public int NewLineCount { get; set; }
    
    /// <summary>
    /// Header text for this chunk (e.g., @@ -1,4 +1,6 @@)
    /// </summary>
    public string Header { get; set; } = string.Empty;
    
    /// <summary>
    /// Lines in this chunk
    /// </summary>
    public List<DiffLine> Lines { get; set; } = new();
}

/// <summary>
/// Enhanced diff model with line-by-line details
/// </summary>
public class EnhancedGitFileDiff : GitFileDiff
{
    /// <summary>
    /// Programming language detected for syntax highlighting
    /// </summary>
    public string Language { get; set; } = "plaintext";
    
    /// <summary>
    /// Diff chunks with line-by-line details
    /// </summary>
    public List<DiffChunk> Chunks { get; set; } = new();
    
    /// <summary>
    /// Content of the old file
    /// </summary>
    public string? OldContent { get; set; }
    
    /// <summary>
    /// Content of the new file
    /// </summary>
    public string? NewContent { get; set; }
    
    /// <summary>
    /// Whether this diff can be interactively accepted/rejected
    /// </summary>
    public bool IsInteractive { get; set; } = true;
    
    /// <summary>
    /// Create an enhanced diff from a basic GitFileDiff
    /// </summary>
    public static EnhancedGitFileDiff FromGitFileDiff(GitFileDiff baseDiff)
    {
        return new EnhancedGitFileDiff
        {
            Path = baseDiff.Path,
            OldPath = baseDiff.OldPath,
            ChangeType = baseDiff.ChangeType,
            LinesAdded = baseDiff.LinesAdded,
            LinesDeleted = baseDiff.LinesDeleted,
            Patch = baseDiff.Patch,
            Language = DetectLanguageFromPath(baseDiff.Path)
        };
    }
    
    /// <summary>
    /// Detect programming language from file path
    /// </summary>
    private static string DetectLanguageFromPath(string path)
    {
        var extension = System.IO.Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".cs" => "csharp",
            ".js" => "javascript",
            ".ts" => "typescript",
            ".html" => "html",
            ".css" => "css",
            ".json" => "json",
            ".xml" => "xml",
            ".yaml" or ".yml" => "yaml",
            ".md" => "markdown",
            ".sql" => "sql",
            ".py" => "python",
            ".java" => "java",
            ".cpp" or ".cc" or ".cxx" => "cpp",
            ".c" => "c",
            ".h" or ".hpp" => "cpp",
            ".razor" => "razor",
            ".cshtml" => "razor",
            ".sh" => "shell",
            ".ps1" => "powershell",
            ".dockerfile" => "dockerfile",
            _ => "plaintext"
        };
    }
}

/// <summary>
/// Result of processing a diff operation
/// </summary>
public class DiffOperationResult
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
    /// Lines that were processed
    /// </summary>
    public List<string> ProcessedLines { get; set; } = new();
    
    /// <summary>
    /// Create a successful result
    /// </summary>
    public static DiffOperationResult Success(List<string>? processedLines = null) => new()
    {
        IsSuccess = true,
        ProcessedLines = processedLines ?? new()
    };
    
    /// <summary>
    /// Create a failed result
    /// </summary>
    public static DiffOperationResult Failure(string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage
    };
}