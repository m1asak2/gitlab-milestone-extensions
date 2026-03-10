using gitlab_milestone_extensions.ApiService.Models;

namespace gitlab_milestone_extensions.ApiService.Services;

/// <summary>
/// Read-only dashboard data provider backed by a cached GitLab snapshot.
/// </summary>
public sealed class GitLabDashboardDataService(IGitLabDataSnapshotService snapshotService) : IDashboardDataService
{
    public async Task<SummaryDto> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var snapshot = await snapshotService.GetSnapshotAsync(cancellationToken);
        var issues = BuildDashboardIssues(snapshot.Issues);
        var milestones = BuildDashboardMilestones(snapshot.Milestones, issues);

        var openIssues = issues.Count(i => i.State.Equals("opened", StringComparison.OrdinalIgnoreCase));
        var closedIssues = issues.Count(i => i.State.Equals("closed", StringComparison.OrdinalIgnoreCase));
        var overdueIssues = issues.Count(i =>
            i.State.Equals("opened", StringComparison.OrdinalIgnoreCase) &&
            i.DueDate.HasValue &&
            i.DueDate.Value < DateOnly.FromDateTime(DateTime.Today));

        return new SummaryDto(
            TotalIssues: issues.Count,
            OpenIssues: openIssues,
            ClosedIssues: closedIssues,
            OverdueIssues: overdueIssues,
            TotalMilestones: milestones.Count);
    }

    public async Task<IReadOnlyList<DashboardIssue>> GetIssuesAsync(CancellationToken cancellationToken)
    {
        var snapshot = await snapshotService.GetSnapshotAsync(cancellationToken);
        return BuildDashboardIssues(snapshot.Issues);
    }

    public async Task<IReadOnlyList<DashboardMilestone>> GetMilestonesAsync(CancellationToken cancellationToken)
    {
        var snapshot = await snapshotService.GetSnapshotAsync(cancellationToken);
        var issues = BuildDashboardIssues(snapshot.Issues);
        return BuildDashboardMilestones(snapshot.Milestones, issues);
    }

    public async Task<IReadOnlyList<GanttItemDto>> GetGanttAsync(string? viewMode, CancellationToken cancellationToken)
    {
        var mode = string.IsNullOrWhiteSpace(viewMode) ? "project" : viewMode.Trim().ToLowerInvariant();
        var snapshot = await snapshotService.GetSnapshotAsync(cancellationToken);
        var issues = BuildDashboardIssues(snapshot.Issues);
        var today = DateOnly.FromDateTime(DateTime.Today);

        var items = issues
            .Select((issue, index) =>
            {
                var endDate = issue.DueDate ?? today;
                var startDate = issue.DueDate?.AddDays(-7) ?? today;
                var milestoneTitle = string.IsNullOrWhiteSpace(issue.MilestoneTitle) ? "(No milestone)" : issue.MilestoneTitle;
                var progress = issue.State.Equals("closed", StringComparison.OrdinalIgnoreCase) ? 100 : 0;

                return new GanttItemDto(
                    Id: issue.Id == 0 ? index + 1 : issue.Id,
                    Title: $"{milestoneTitle} | {issue.Title}",
                    ViewMode: mode,
                    Owner: issue.ProjectName,
                    StartDate: startDate,
                    EndDate: endDate,
                    Progress: progress);
            })
            .ToList();

        return items;
    }

    private static List<DashboardIssue> BuildDashboardIssues(IReadOnlyList<GitLabIssueDto> gitLabIssues)
    {
        return gitLabIssues
            .Select(i => new DashboardIssue(
                ProjectName: i.ProjectName,
                MilestoneTitle: i.MilestoneTitle ?? string.Empty,
                Title: i.Title,
                State: i.State,
                AssigneeName: i.AssigneeName ?? string.Empty,
                DueDate: i.DueDate,
                Id: i.Iid))
            .ToList();
    }

    private static IReadOnlyList<DashboardMilestone> BuildDashboardMilestones(
        IReadOnlyList<GitLabMilestoneDto> gitLabMilestones,
        IReadOnlyList<DashboardIssue> dashboardIssues)
    {
        var issueCountByMilestone = dashboardIssues
            .Where(i => !string.IsNullOrWhiteSpace(i.MilestoneTitle))
            .GroupBy(i => new { i.ProjectName, i.MilestoneTitle })
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    OpenIssues = g.Count(i => i.State.Equals("opened", StringComparison.OrdinalIgnoreCase)),
                    ClosedIssues = g.Count(i => i.State.Equals("closed", StringComparison.OrdinalIgnoreCase))
                });

        var milestones = gitLabMilestones
            .Select(m =>
            {
                var key = new { m.ProjectName, MilestoneTitle = m.Title };
                var counts = issueCountByMilestone.GetValueOrDefault(key);

                return new DashboardMilestone(
                    ProjectName: m.ProjectName,
                    Title: m.Title,
                    State: m.State,
                    StartDate: m.StartDate,
                    DueDate: m.DueDate,
                    OpenIssues: counts?.OpenIssues ?? 0,
                    ClosedIssues: counts?.ClosedIssues ?? 0,
                    Id: m.MilestoneId);
            })
            .ToList();

        return milestones;
    }
}
