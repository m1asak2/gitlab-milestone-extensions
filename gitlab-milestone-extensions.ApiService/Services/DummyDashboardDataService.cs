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
    public Task<IReadOnlyList<IssueDto>> GetIssuesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<IssueDto> items =
        [
            new IssueDto(101, "API endpoint skeleton", "Dashboard.Api", "Alice", "opened", "MVP Sprint 1", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2))),
            new IssueDto(102, "Blazor dashboard shell", "Dashboard.Web", "Bob", "opened", "MVP Sprint 1", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(4))),
            new IssueDto(103, "Milestone progress card", "Dashboard.Web", "Carla", "closed", "MVP Sprint 1", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))),
            new IssueDto(104, "Dummy gantt API data", "Dashboard.Api", "Dan", "opened", "MVP Sprint 2", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(6)))
        ];

        return Task.FromResult(items);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<MilestoneDto>> GetMilestonesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<MilestoneDto> items =
        [
            new MilestoneDto(201, "MVP Sprint 1", "Dashboard", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)), 62),
            new MilestoneDto(202, "MVP Sprint 2", "Dashboard", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(8)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(21)), 8)
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
