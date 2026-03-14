using gitlab_milestone_extensions.ApiService.Models;

namespace gitlab_milestone_extensions.ApiService.Services;

/// <summary>
/// Read-only data access contract for dashboard endpoints.
/// </summary>
public interface IDashboardDataService
{
    Task<SelectionOptionsDto> GetSelectionOptionsAsync(
        int? groupId,
        int? memberId,
        int? projectId,
        int? milestoneId,
        CancellationToken cancellationToken);

    Task<MilestoneDashboardDto?> GetDashboardAsync(int groupId, int milestoneId, CancellationToken cancellationToken);

    Task<IReadOnlyList<DashboardIssue>> GetIssuesAsync(int groupId, int milestoneId, CancellationToken cancellationToken);

    Task<IReadOnlyList<GanttItemDto>> GetGanttAsync(int groupId, int milestoneId, CancellationToken cancellationToken);
}
