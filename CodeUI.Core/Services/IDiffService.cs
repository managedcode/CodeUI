using CodeUI.Core.Models;
using System.Text.RegularExpressions;

namespace CodeUI.Core.Services;

/// <summary>
/// Service for processing Git diffs into enhanced diff models
/// </summary>
public interface IDiffService
{
    /// <summary>
    /// Converts a basic GitFileDiff into an enhanced diff with line-by-line details
    /// </summary>
    /// <param name="gitFileDiff">Basic diff information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Enhanced diff with line details</returns>
    Task<EnhancedGitFileDiff> ProcessDiffAsync(GitFileDiff gitFileDiff, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Processes multiple diffs in parallel
    /// </summary>
    /// <param name="gitFileDiffs">List of basic diffs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of enhanced diffs</returns>
    Task<List<EnhancedGitFileDiff>> ProcessDiffsAsync(IEnumerable<GitFileDiff> gitFileDiffs, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Applies accepted/rejected changes to create a modified patch
    /// </summary>
    /// <param name="enhancedDiff">Enhanced diff with line decisions</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Modified patch content</returns>
    Task<DiffOperationResult> ApplyLineChangesAsync(EnhancedGitFileDiff enhancedDiff, CancellationToken cancellationToken = default);
}