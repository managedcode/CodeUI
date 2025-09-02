using CodeUI.Core.Models;
using CodeUI.Core.Services;
using Xunit;

namespace CodeUI.Tests.Services;

/// <summary>
/// Tests for the DiffService functionality
/// </summary>
public class DiffServiceTests
{
    private readonly DiffService _diffService;

    public DiffServiceTests()
    {
        _diffService = new DiffService();
    }

    [Fact]
    public async Task ProcessDiffAsync_ShouldCreateEnhancedDiff_FromBasicDiff()
    {
        // Arrange
        var basicDiff = new GitFileDiff
        {
            Path = "test.cs",
            ChangeType = GitChangeType.Modified,
            LinesAdded = 2,
            LinesDeleted = 1,
            Patch = @"@@ -1,4 +1,5 @@
 namespace Test
 {
+    using System;
     public class Test
     {
-        private int value;
+        private string value;
     }
 }"
        };

        // Act
        var enhancedDiff = await _diffService.ProcessDiffAsync(basicDiff);

        // Assert
        Assert.NotNull(enhancedDiff);
        Assert.Equal("test.cs", enhancedDiff.Path);
        Assert.Equal("csharp", enhancedDiff.Language);
        Assert.Equal(GitChangeType.Modified, enhancedDiff.ChangeType);
        Assert.True(enhancedDiff.Chunks.Count > 0);
    }

    [Fact]
    public async Task ProcessDiffAsync_ShouldDetectCorrectLanguage_FromFileExtension()
    {
        // Arrange
        var testCases = new[]
        {
            ("test.cs", "csharp"),
            ("script.js", "javascript"),
            ("component.ts", "typescript"),
            ("page.html", "html"),
            ("style.css", "css"),
            ("data.json", "json"),
            ("config.xml", "xml"),
            ("readme.md", "markdown"),
            ("component.razor", "razor"),
            ("unknown.xyz", "plaintext")
        };

        foreach (var (path, expectedLanguage) in testCases)
        {
            var diff = new GitFileDiff { Path = path, Patch = "" };

            // Act
            var enhancedDiff = await _diffService.ProcessDiffAsync(diff);

            // Assert
            Assert.Equal(expectedLanguage, enhancedDiff.Language);
        }
    }

    [Fact]
    public async Task ProcessDiffAsync_ShouldParseUnifiedDiff_IntoChunks()
    {
        // Arrange
        var diff = new GitFileDiff
        {
            Path = "example.cs",
            Patch = @"@@ -1,4 +1,5 @@
 using System;
+using System.Linq;
 
 namespace Example
 {
@@ -7,3 +8,4 @@
     {
         return 42;
     }
+    public void NewMethod() { }
 }"
        };

        // Act
        var enhancedDiff = await _diffService.ProcessDiffAsync(diff);

        // Assert
        Assert.Equal(2, enhancedDiff.Chunks.Count);
        
        var firstChunk = enhancedDiff.Chunks[0];
        Assert.Equal(1, firstChunk.OldStartLine);
        Assert.Equal(4, firstChunk.OldLineCount);
        Assert.Equal(1, firstChunk.NewStartLine);
        Assert.Equal(5, firstChunk.NewLineCount);

        var secondChunk = enhancedDiff.Chunks[1];
        Assert.Equal(7, secondChunk.OldStartLine);
        Assert.Equal(3, secondChunk.OldLineCount);
        Assert.Equal(8, secondChunk.NewStartLine);
        Assert.Equal(4, secondChunk.NewLineCount);
    }

    [Fact]
    public async Task ProcessDiffAsync_ShouldParseLines_WithCorrectTypes()
    {
        // Arrange
        var diff = new GitFileDiff
        {
            Path = "test.txt",
            Patch = @"@@ -1,3 +1,3 @@
 unchanged line
-deleted line
+added line"
        };

        // Act
        var enhancedDiff = await _diffService.ProcessDiffAsync(diff);

        // Assert
        var chunk = enhancedDiff.Chunks[0];
        Assert.Equal(3, chunk.Lines.Count);
        
        Assert.Equal(DiffLineType.Unchanged, chunk.Lines[0].Type);
        Assert.Equal("unchanged line", chunk.Lines[0].Content);
        
        Assert.Equal(DiffLineType.Deleted, chunk.Lines[1].Type);
        Assert.Equal("deleted line", chunk.Lines[1].Content);
        
        Assert.Equal(DiffLineType.Added, chunk.Lines[2].Type);
        Assert.Equal("added line", chunk.Lines[2].Content);
    }

    [Fact]
    public async Task ApplyLineChangesAsync_ShouldExcludeRejectedLines()
    {
        // Arrange
        var enhancedDiff = new EnhancedGitFileDiff
        {
            Path = "test.txt",
            Chunks = new List<DiffChunk>
            {
                new DiffChunk
                {
                    Lines = new List<DiffLine>
                    {
                        new DiffLine { Content = "line 1", Type = DiffLineType.Unchanged, IsAccepted = false, IsRejected = false },
                        new DiffLine { Content = "line 2", Type = DiffLineType.Added, IsAccepted = true, IsRejected = false },
                        new DiffLine { Content = "line 3", Type = DiffLineType.Added, IsAccepted = false, IsRejected = true },
                        new DiffLine { Content = "line 4", Type = DiffLineType.Deleted, IsAccepted = false, IsRejected = false }
                    }
                }
            }
        };

        // Act
        var result = await _diffService.ApplyLineChangesAsync(enhancedDiff);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.ProcessedLines.Count);
        Assert.Contains("line 1", result.ProcessedLines); // unchanged line included
        Assert.Contains("line 2", result.ProcessedLines); // accepted added line included
        Assert.DoesNotContain("line 3", result.ProcessedLines); // rejected line excluded
        Assert.DoesNotContain("line 4", result.ProcessedLines); // deleted line not explicitly accepted
    }

    [Fact]
    public async Task ProcessDiffsAsync_ShouldProcessMultipleDiffs_InParallel()
    {
        // Arrange
        var diffs = new[]
        {
            new GitFileDiff { Path = "file1.cs", Patch = "@@ -1,1 +1,1 @@\n-old\n+new" },
            new GitFileDiff { Path = "file2.js", Patch = "@@ -1,1 +1,1 @@\n-old\n+new" },
            new GitFileDiff { Path = "file3.html", Patch = "@@ -1,1 +1,1 @@\n-old\n+new" }
        };

        // Act
        var enhancedDiffs = await _diffService.ProcessDiffsAsync(diffs);

        // Assert
        Assert.Equal(3, enhancedDiffs.Count);
        Assert.Equal("csharp", enhancedDiffs[0].Language);
        Assert.Equal("javascript", enhancedDiffs[1].Language);
        Assert.Equal("html", enhancedDiffs[2].Language);
    }

    [Fact]
    public async Task ProcessDiffAsync_ShouldHandleEmptyPatch()
    {
        // Arrange
        var diff = new GitFileDiff
        {
            Path = "empty.txt",
            Patch = string.Empty
        };

        // Act
        var enhancedDiff = await _diffService.ProcessDiffAsync(diff);

        // Assert
        Assert.NotNull(enhancedDiff);
        Assert.Empty(enhancedDiff.Chunks);
        Assert.Equal("plaintext", enhancedDiff.Language);
    }
}