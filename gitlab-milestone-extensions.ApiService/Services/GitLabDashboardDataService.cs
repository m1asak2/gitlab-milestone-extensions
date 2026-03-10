using gitlab_milestone_extensions.ApiService.Models;

namespace gitlab_milestone_extensions.ApiService.Services;

/// <summary>
/// Read-only dashboard data provider backed by GitLab project milestones/issues.
/// </summary>
public sealed class GitLabDashboardDataService(GitLabApiClient gitLabApiClient) : IDashboardDataService
{
    public async Task<SummaryDto> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var issues = await BuildDashboardIssuesAsync(cancellationToken);
        var milestones = await BuildDashboardMilestonesAsync(issues, cancellationToken);

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
        return await BuildDashboardIssuesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DashboardMilestone>> GetMilestonesAsync(CancellationToken cancellationToken)
    {
        var issues = await BuildDashboardIssuesAsync(cancellationToken);
        return await BuildDashboardMilestonesAsync(issues, cancellationToken);
    }

    public async Task<IReadOnlyList<GanttItemDto>> GetGanttAsync(string? viewMode, CancellationToken cancellationToken)
    {
        var mode = string.IsNullOrWhiteSpace(viewMode) ? "project" : viewMode.Trim().ToLowerInvariant();
        var issues = await BuildDashboardIssuesAsync(cancellationToken);
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

    private async Task<List<DashboardIssue>> BuildDashboardIssuesAsync(CancellationToken cancellationToken)
    {
        var gitLabIssues = await gitLabApiClient.GetProjectIssuesAsync(cancellationToken);

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

    private async Task<IReadOnlyList<DashboardMilestone>> BuildDashboardMilestonesAsync(
        IReadOnlyList<DashboardIssue> dashboardIssues,
        CancellationToken cancellationToken)
    {
        var gitLabMilestones = await gitLabApiClient.GetProjectMilestonesAsync(cancellationToken);

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
