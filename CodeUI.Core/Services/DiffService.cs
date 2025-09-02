using CodeUI.Core.Models;
using System.Text.RegularExpressions;

namespace CodeUI.Core.Services;

/// <summary>
/// Implementation of diff processing service
/// </summary>
public class DiffService : IDiffService
{
    private static readonly Regex ChunkHeaderRegex = new(@"^@@\s*-(\d+)(?:,(\d+))?\s*\+(\d+)(?:,(\d+))?\s*@@", RegexOptions.Compiled);
    
    /// <summary>
    /// Converts a basic GitFileDiff into an enhanced diff with line-by-line details
    /// </summary>
    public async Task<EnhancedGitFileDiff> ProcessDiffAsync(GitFileDiff gitFileDiff, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var enhancedDiff = EnhancedGitFileDiff.FromGitFileDiff(gitFileDiff);
            
            if (string.IsNullOrEmpty(gitFileDiff.Patch))
            {
                return enhancedDiff;
            }
            
            // Parse the unified diff format
            var lines = gitFileDiff.Patch.Split('\n', StringSplitOptions.None);
            var chunks = ParseDiffChunks(lines);
            enhancedDiff.Chunks = chunks;
            
            return enhancedDiff;
        }, cancellationToken);
    }
    
    /// <summary>
    /// Processes multiple diffs in parallel
    /// </summary>
    public async Task<List<EnhancedGitFileDiff>> ProcessDiffsAsync(IEnumerable<GitFileDiff> gitFileDiffs, CancellationToken cancellationToken = default)
    {
        var tasks = gitFileDiffs.Select(diff => ProcessDiffAsync(diff, cancellationToken));
        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }
    
    /// <summary>
    /// Applies accepted/rejected changes to create a modified patch
    /// </summary>
    public async Task<DiffOperationResult> ApplyLineChangesAsync(EnhancedGitFileDiff enhancedDiff, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var processedLines = new List<string>();
                
                foreach (var chunk in enhancedDiff.Chunks)
                {
                    var acceptedLines = new List<string>();
                    
                    foreach (var line in chunk.Lines)
                    {
                        // Only include lines that haven't been rejected
                        if (!line.IsRejected)
                        {
                            // For accepted lines or unchanged lines, include them
                            if (line.IsAccepted || line.Type == DiffLineType.Unchanged)
                            {
                                acceptedLines.Add(line.Content);
                            }
                            // For added lines that aren't explicitly rejected, include them
                            else if (line.Type == DiffLineType.Added && !line.IsRejected)
                            {
                                acceptedLines.Add(line.Content);
                            }
                            // Skip deleted lines unless explicitly accepted
                            else if (line.Type == DiffLineType.Deleted && line.IsAccepted)
                            {
                                acceptedLines.Add(line.Content);
                            }
                        }
                    }
                    
                    processedLines.AddRange(acceptedLines);
                }
                
                return DiffOperationResult.Success(processedLines);
            }
            catch (Exception ex)
            {
                return DiffOperationResult.Failure($"Failed to apply line changes: {ex.Message}");
            }
        }, cancellationToken);
    }
    
    /// <summary>
    /// Parses unified diff format into chunks and lines
    /// </summary>
    private static List<DiffChunk> ParseDiffChunks(string[] lines)
    {
        var chunks = new List<DiffChunk>();
        DiffChunk? currentChunk = null;
        var oldLineNumber = 0;
        var newLineNumber = 0;
        
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            
            // Skip diff headers (file paths, etc.)
            if (line.StartsWith("diff --git") || line.StartsWith("index ") || 
                line.StartsWith("---") || line.StartsWith("+++"))
            {
                continue;
            }
            
            // Check for chunk header
            var chunkMatch = ChunkHeaderRegex.Match(line);
            if (chunkMatch.Success)
            {
                // Save previous chunk if exists
                if (currentChunk != null)
                {
                    chunks.Add(currentChunk);
                }
                
                // Parse chunk header
                var oldStart = int.Parse(chunkMatch.Groups[1].Value);
                var oldCount = chunkMatch.Groups[2].Success ? int.Parse(chunkMatch.Groups[2].Value) : 1;
                var newStart = int.Parse(chunkMatch.Groups[3].Value);
                var newCount = chunkMatch.Groups[4].Success ? int.Parse(chunkMatch.Groups[4].Value) : 1;
                
                currentChunk = new DiffChunk
                {
                    OldStartLine = oldStart,
                    OldLineCount = oldCount,
                    NewStartLine = newStart,
                    NewLineCount = newCount,
                    Header = line
                };
                
                oldLineNumber = oldStart;
                newLineNumber = newStart;
                continue;
            }
            
            // Process content lines
            if (currentChunk != null && !string.IsNullOrEmpty(line))
            {
                var diffLine = ParseDiffLine(line, ref oldLineNumber, ref newLineNumber);
                if (diffLine != null)
                {
                    currentChunk.Lines.Add(diffLine);
                }
            }
        }
        
        // Add the last chunk
        if (currentChunk != null)
        {
            chunks.Add(currentChunk);
        }
        
        return chunks;
    }
    
    /// <summary>
    /// Parses a single diff line
    /// </summary>
    private static DiffLine? ParseDiffLine(string line, ref int oldLineNumber, ref int newLineNumber)
    {
        if (string.IsNullOrEmpty(line))
        {
            return null;
        }
        
        var prefix = line[0];
        var content = line.Length > 1 ? line[1..] : string.Empty;
        
        return prefix switch
        {
            ' ' => new DiffLine
            {
                OldLineNumber = oldLineNumber++,
                NewLineNumber = newLineNumber++,
                Content = content,
                Type = DiffLineType.Unchanged
            },
            '+' => new DiffLine
            {
                OldLineNumber = null,
                NewLineNumber = newLineNumber++,
                Content = content,
                Type = DiffLineType.Added
            },
            '-' => new DiffLine
            {
                OldLineNumber = oldLineNumber++,
                NewLineNumber = null,
                Content = content,
                Type = DiffLineType.Deleted
            },
            _ => null
        };
    }
}