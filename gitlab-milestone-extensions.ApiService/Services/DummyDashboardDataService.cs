using gitlab_milestone_extensions.ApiService.Models;

namespace gitlab_milestone_extensions.ApiService.Services;

/// <summary>
/// Dummy read-only data provider used before real GitLab integration.
/// </summary>
public sealed class DummyDashboardDataService : IDashboardDataService
{
    /// <inheritdoc />
    public Task<SummaryDto> GetSummaryAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(new SummaryDto(
            TotalIssues: 18,
            OpenIssues: 11,
            ClosedIssues: 7,
            OverdueIssues: 3));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<DashboardIssue>> GetIssuesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<DashboardIssue> items =
        [
            new DashboardIssue("Dashboard.Api", 201, "MVP Sprint 1", "API endpoint skeleton", "opened", "Alice", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)), 7200, 1800, "2h", "30m", 101),
            new DashboardIssue("Dashboard.Web", 201, "MVP Sprint 1", "Blazor dashboard shell", "opened", "Bob", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(4)), 10800, 3600, "3h", "1h", 102),
            new DashboardIssue("Dashboard.Web", 201, "MVP Sprint 1", "Milestone progress card", "closed", "Carla", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), 5400, 5400, "1h 30m", "1h 30m", 103),
            new DashboardIssue("Dashboard.Api", 202, "MVP Sprint 2", "Dummy gantt API data", "opened", "Dan", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(6)), 14400, 1800, "4h", "30m", 104)
        ];

        return Task.FromResult(items);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<DashboardMilestone>> GetMilestonesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<DashboardMilestone> items =
        [
            new DashboardMilestone("Dashboard", "MVP Sprint 1", "Project", "active", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)), 3, 5, 23400, 10800, 201),
            new DashboardMilestone("Dashboard", "MVP Sprint 2", "Group", "active", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(8)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(21)), 11, 1, 14400, 1800, 202)
        ];

        return Task.FromResult(items);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<GanttItemDto>> GetGanttAsync(
        string? viewMode,
        string? milestone,
        int? milestoneId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var mode = string.IsNullOrWhiteSpace(viewMode) ? "project" : viewMode.Trim().ToLowerInvariant();

        IReadOnlyList<GanttItemDto> items =
        [
            new GanttItemDto(301, "MVP Sprint 1", mode, "Dashboard.Web", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)), 62, 201, "MVP Sprint 1", 23400, 10800),
            new GanttItemDto(302, "MVP Sprint 2", mode, "Dashboard.Api", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(8)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(21)), 8, 202, "MVP Sprint 2", 14400, 1800)
        ];

        if (!string.IsNullOrWhiteSpace(milestone))
        {
            items = items.Where(i => string.Equals(i.MilestoneTitle, milestone, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (milestoneId.HasValue)
        {
            items = items.Where(i => i.MilestoneId == milestoneId.Value).ToList();
        }

        return Task.FromResult(items);
    }
}
