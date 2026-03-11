using gitlab_milestone_extensions.ApiService.Models;

namespace gitlab_milestone_extensions.ApiService.Services;

/// <summary>
/// Read-only data access contract for dashboard endpoints.
/// </summary>
public interface IDashboardDataService
{
    /// <summary>
    /// Gets dashboard summary metrics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Summary metrics.</returns>
    Task<SummaryDto> GetSummaryAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets issue rows.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Issue list.</returns>
    Task<IReadOnlyList<DashboardIssue>> GetIssuesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets milestone rows.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Milestone list.</returns>
    Task<IReadOnlyList<DashboardMilestone>> GetMilestonesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets gantt-like timeline rows.
    /// </summary>
    /// <param name="viewMode">Grouping mode.</param>
    /// <param name="milestone">Milestone title filter.</param>
    /// <param name="milestoneId">Milestone id filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Timeline list.</returns>
    Task<IReadOnlyList<GanttItemDto>> GetGanttAsync(
        string? viewMode,
        string? milestone,
        int? milestoneId,
        CancellationToken cancellationToken);
}
