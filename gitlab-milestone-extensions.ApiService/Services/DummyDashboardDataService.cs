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
            new DashboardIssue("Dashboard.Api", "MVP Sprint 1", "API endpoint skeleton", "opened", "Alice", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)), 101),
            new DashboardIssue("Dashboard.Web", "MVP Sprint 1", "Blazor dashboard shell", "opened", "Bob", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(4)), 102),
            new DashboardIssue("Dashboard.Web", "MVP Sprint 1", "Milestone progress card", "closed", "Carla", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), 103),
            new DashboardIssue("Dashboard.Api", "MVP Sprint 2", "Dummy gantt API data", "opened", "Dan", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(6)), 104)
        ];

        return Task.FromResult(items);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<DashboardMilestone>> GetMilestonesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<DashboardMilestone> items =
        [
            new DashboardMilestone("Dashboard", "MVP Sprint 1", "active", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)), 3, 5, 201),
            new DashboardMilestone("Dashboard", "MVP Sprint 2", "active", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(8)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(21)), 11, 1, 202)
        ];

        return Task.FromResult(items);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<GanttItemDto>> GetGanttAsync(string? viewMode, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var mode = string.IsNullOrWhiteSpace(viewMode) ? "project" : viewMode.Trim().ToLowerInvariant();

        IReadOnlyList<GanttItemDto> items =
        [
            new GanttItemDto(301, "MVP Sprint 1", mode, "Dashboard.Web", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)), 62),
            new GanttItemDto(302, "MVP Sprint 2", mode, "Dashboard.Api", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(8)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(21)), 8)
        ];

        return Task.FromResult(items);
    }
}
