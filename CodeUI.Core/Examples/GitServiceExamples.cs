using CodeUI.Core.Models;
using CodeUI.Core.Services;

namespace CodeUI.Core.Examples;

/// <summary>
/// Example demonstrating how to use the GitService for various Git operations.
/// </summary>
public static class GitServiceExamples
{
    /// <summary>
    /// Example of basic Git repository operations
    /// </summary>
    public static async Task BasicGitOperationsExample()
    {
        using var gitService = new GitService();

        Console.WriteLine("=== Git Repository Operations Example ===");

        // Check if current directory is a Git repository
        var isRepo = await gitService.IsGitRepositoryAsync();
        Console.WriteLine($"Is Git Repository: {isRepo}");

        if (!isRepo)
        {
            Console.WriteLine("Current directory is not a Git repository.");
            
            // Try to find repository root
            var repoRoot = await gitService.FindRepositoryRootAsync();
            if (repoRoot != null)
            {
                Console.WriteLine($"Found Git repository at: {repoRoot}");
            }
            else
            {
                Console.WriteLine("No Git repository found in parent directories.");
                return;
            }
        }

        // Get repository status
        var status = await gitService.GetRepositoryStatusAsync();
        if (status != null)
        {
            Console.WriteLine($"\nRepository Status:");
            Console.WriteLine($"  Current Branch: {status.CurrentBranch}");
            Console.WriteLine($"  Is Dirty: {status.IsDirty}");
            Console.WriteLine($"  Modified Files: {status.ModifiedFiles.Count}");
            Console.WriteLine($"  Ahead by: {status.AheadCount} commits");
            Console.WriteLine($"  Behind by: {status.BehindCount} commits");

            // Show modified files
            if (status.ModifiedFiles.Any())
            {
                Console.WriteLine("\nModified Files:");
                foreach (var file in status.ModifiedFiles.Take(10)) // Show first 10
                {
                    Console.WriteLine($"  {file.State}: {file.FilePath}");
                }
            }
        }

        // Get branches
        var branches = await gitService.GetBranchesAsync(includeRemote: false);
        Console.WriteLine($"\nLocal Branches ({branches.Count}):");
        foreach (var branch in branches.Take(10)) // Show first 10
        {
            var marker = branch.IsCurrent ? "* " : "  ";
            Console.WriteLine($"{marker}{branch.Name} ({branch.TipSha[..8]})");
        }
    }

    /// <summary>
    /// Example of Git commit history operations
    /// </summary>
    public static async Task GitHistoryExample()
    {
        using var gitService = new GitService();

        Console.WriteLine("\n=== Git Commit History Example ===");

        // Get recent commit history
        var commits = await gitService.GetCommitHistoryAsync(maxCount: 5);
        
        if (commits.Any())
        {
            Console.WriteLine("Recent Commits:");
            foreach (var commit in commits)
            {
                Console.WriteLine($"\nCommit: {commit.Sha[..8]}");
                Console.WriteLine($"Author: {commit.AuthorName} <{commit.AuthorEmail}>");
                Console.WriteLine($"Date: {commit.Date:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"Message: {commit.Message.Split('\n')[0]}"); // First line only
            }
        }
        else
        {
            Console.WriteLine("No commits found in repository.");
        }
    }

    /// <summary>
    /// Example of Git file operations (staging, committing)
    /// </summary>
    public static async Task GitFileOperationsExample()
    {
        using var gitService = new GitService();

        Console.WriteLine("\n=== Git File Operations Example ===");

        // Get repository status
        var status = await gitService.GetRepositoryStatusAsync();
        if (status == null)
        {
            Console.WriteLine("No Git repository found.");
            return;
        }

        // Show unstaged changes
        var unstagedFiles = status.ModifiedFiles
            .Where(f => f.WorkingDirectoryState != GitFileState.Unmodified)
            .Take(5)
            .ToList();

        if (unstagedFiles.Any())
        {
            Console.WriteLine("Unstaged Changes:");
            foreach (var file in unstagedFiles)
            {
                Console.WriteLine($"  {file.WorkingDirectoryState}: {file.FilePath}");
                
                // Get diff for the file
                var diff = await gitService.GetFileDiffAsync(file.FilePath, staged: false);
                if (diff != null)
                {
                    Console.WriteLine($"    Lines Added: {diff.LinesAdded}, Deleted: {diff.LinesDeleted}");
                }
            }

            // Example: Stage the first modified file
            var firstFile = unstagedFiles.First();
            Console.WriteLine($"\nStaging file: {firstFile.FilePath}");
            var stageResult = await gitService.StageFileAsync(firstFile.FilePath);
            
            if (stageResult.IsSuccess)
            {
                Console.WriteLine($"✓ {stageResult.Details}");
                
                // Show how to unstage it
                Console.WriteLine($"Unstaging file: {firstFile.FilePath}");
                var unstageResult = await gitService.UnstageFileAsync(firstFile.FilePath);
                
                if (unstageResult.IsSuccess)
                {
                    Console.WriteLine($"✓ {unstageResult.Details}");
                }
                else
                {
                    Console.WriteLine($"✗ {unstageResult.ErrorMessage}");
                }
            }
            else
            {
                Console.WriteLine($"✗ {stageResult.ErrorMessage}");
            }
        }
        else
        {
            Console.WriteLine("No unstaged changes found.");
        }
    }

    /// <summary>
    /// Example of Git branch operations
    /// </summary>
    public static async Task GitBranchOperationsExample()
    {
        using var gitService = new GitService();

        Console.WriteLine("\n=== Git Branch Operations Example ===");

        // Get current status
        var status = await gitService.GetRepositoryStatusAsync();
        if (status == null)
        {
            Console.WriteLine("No Git repository found.");
            return;
        }

        Console.WriteLine($"Current branch: {status.CurrentBranch}");

        // Get all branches
        var branches = await gitService.GetBranchesAsync();
        Console.WriteLine($"\nAll branches ({branches.Count}):");
        
        var localBranches = branches.Where(b => !b.IsRemote).ToList();
        var remoteBranches = branches.Where(b => b.IsRemote).ToList();
        
        Console.WriteLine($"Local branches ({localBranches.Count}):");
        foreach (var branch in localBranches)
        {
            var marker = branch.IsCurrent ? "* " : "  ";
            Console.WriteLine($"{marker}{branch.Name}");
        }
        
        if (remoteBranches.Any())
        {
            Console.WriteLine($"\nRemote branches ({remoteBranches.Count}):");
            foreach (var branch in remoteBranches.Take(5)) // Show first 5
            {
                Console.WriteLine($"  {branch.Name}");
            }
        }

        // Example: Create a new branch (but don't actually do it to avoid side effects)
        Console.WriteLine("\nExample operations (not executed):");
        Console.WriteLine("- Create branch: await gitService.CreateBranchAsync(\"feature/new-feature\")");
        Console.WriteLine("- Switch branch: await gitService.SwitchBranchAsync(\"main\")");
        Console.WriteLine("- Create and switch: await gitService.CreateBranchAsync(\"feature/demo\", switchToBranch: true)");
    }

    /// <summary>
    /// Example showing error handling in Git operations
    /// </summary>
    public static async Task GitErrorHandlingExample()
    {
        using var gitService = new GitService();

        Console.WriteLine("\n=== Git Error Handling Example ===");

        // Try operations that might fail
        Console.WriteLine("Testing error conditions:");

        // Try to get status from a non-existent directory
        var invalidStatus = await gitService.GetRepositoryStatusAsync("/non/existent/path");
        Console.WriteLine($"Status from invalid path: {(invalidStatus == null ? "null (expected)" : "unexpected result")}");

        // Try to stage a non-existent file
        var stageResult = await gitService.StageFileAsync("non-existent-file.txt");
        Console.WriteLine($"Stage non-existent file: {(stageResult.IsSuccess ? "unexpected success" : "failed as expected")}");
        if (!stageResult.IsSuccess)
        {
            Console.WriteLine($"  Error: {stageResult.ErrorMessage}");
        }

        // Try to switch to a non-existent branch
        var switchResult = await gitService.SwitchBranchAsync("non-existent-branch");
        Console.WriteLine($"Switch to invalid branch: {(switchResult.IsSuccess ? "unexpected success" : "failed as expected")}");
        if (!switchResult.IsSuccess)
        {
            Console.WriteLine($"  Error: {switchResult.ErrorMessage}");
        }

        // Try to get commit that doesn't exist
        var invalidCommit = await gitService.GetCommitAsync("invalid-sha");
        Console.WriteLine($"Get invalid commit: {(invalidCommit == null ? "null (expected)" : "unexpected result")}");

        Console.WriteLine("\nAll error conditions handled gracefully ✓");
    }

    /// <summary>
    /// Comprehensive example showing integration of multiple Git operations
    /// </summary>
    public static async Task ComprehensiveGitWorkflowExample()
    {
        using var gitService = new GitService();

        Console.WriteLine("\n=== Comprehensive Git Workflow Example ===");

        // Step 1: Check repository state
        var isRepo = await gitService.IsGitRepositoryAsync();
        if (!isRepo)
        {
            Console.WriteLine("Not in a Git repository. Skipping workflow example.");
            return;
        }

        // Step 2: Get overall repository status
        var status = await gitService.GetRepositoryStatusAsync();
        if (status == null)
        {
            Console.WriteLine("Could not retrieve repository status.");
            return;
        }

        Console.WriteLine($"Repository: {status.RepositoryPath}");
        Console.WriteLine($"Current branch: {status.CurrentBranch}");
        Console.WriteLine($"Repository is {(status.IsDirty ? "dirty" : "clean")}");

        // Step 3: Show file changes
        if (status.ModifiedFiles.Any())
        {
            Console.WriteLine($"\nFound {status.ModifiedFiles.Count} modified files:");
            
            var unstagedFiles = status.ModifiedFiles.Where(f => 
                f.WorkingDirectoryState != GitFileState.Unmodified).ToList();
            var stagedFiles = status.ModifiedFiles.Where(f => 
                f.IndexState != GitFileState.Unmodified).ToList();

            if (unstagedFiles.Any())
            {
                Console.WriteLine($"  Unstaged changes: {unstagedFiles.Count}");
                foreach (var file in unstagedFiles.Take(3))
                {
                    Console.WriteLine($"    {file.WorkingDirectoryState}: {file.FilePath}");
                }
            }

            if (stagedFiles.Any())
            {
                Console.WriteLine($"  Staged changes: {stagedFiles.Count}");
                foreach (var file in stagedFiles.Take(3))
                {
                    Console.WriteLine($"    {file.IndexState}: {file.FilePath}");
                }
            }

            // Step 4: Show diffs for first few files
            Console.WriteLine("\nDiffs for changed files:");
            var allDiffs = await gitService.GetAllFilesDiffAsync(staged: false);
            foreach (var diff in allDiffs.Take(2))
            {
                Console.WriteLine($"\n--- {diff.Path} ---");
                Console.WriteLine($"Change type: {diff.ChangeType}");
                Console.WriteLine($"+{diff.LinesAdded} -{diff.LinesDeleted}");
                
                // Show first few lines of patch
                var patchLines = diff.Patch.Split('\n').Take(10);
                foreach (var line in patchLines)
                {
                    if (line.StartsWith("@@")) break;
                    Console.WriteLine(line);
                }
            }
        }

        // Step 5: Show recent history
        Console.WriteLine("\nRecent commit history:");
        var commits = await gitService.GetCommitHistoryAsync(maxCount: 3);
        foreach (var commit in commits)
        {
            Console.WriteLine($"\n{commit.Sha[..8]} - {commit.AuthorName}");
            Console.WriteLine($"  {commit.Date:yyyy-MM-dd HH:mm}");
            Console.WriteLine($"  {commit.Message.Split('\n')[0]}");
        }

        // Step 6: Show branch information
        var branches = await gitService.GetBranchesAsync(includeRemote: false);
        Console.WriteLine($"\nLocal branches ({branches.Count}):");
        foreach (var branch in branches)
        {
            var marker = branch.IsCurrent ? "* " : "  ";
            Console.WriteLine($"{marker}{branch.Name}");
        }

        Console.WriteLine("\nWorkflow example completed ✓");
    }
}