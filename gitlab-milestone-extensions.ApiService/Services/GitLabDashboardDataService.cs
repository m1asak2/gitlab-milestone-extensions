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

    public async Task<IReadOnlyList<GanttItemDto>> GetGanttAsync(
        string? viewMode,
        string? milestone,
        int? milestoneId,
        CancellationToken cancellationToken)
    {
        var mode = string.IsNullOrWhiteSpace(viewMode) ? "project" : viewMode.Trim().ToLowerInvariant();
        var snapshot = await snapshotService.GetSnapshotAsync(cancellationToken);
        var issues = BuildDashboardIssues(snapshot.Issues);
        var today = DateOnly.FromDateTime(DateTime.Today);

        if (!string.IsNullOrWhiteSpace(milestone))
        {
            issues = issues
                .Where(i => i.MilestoneTitle.Equals(milestone, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (milestoneId.HasValue)
        {
            issues = issues
                .Where(i => i.MilestoneId == milestoneId.Value)
                .ToList();
        }

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
                    Progress: progress,
                    MilestoneId: issue.MilestoneId,
                    MilestoneTitle: issue.MilestoneTitle,
                    TimeEstimateSeconds: issue.TimeEstimateSeconds,
                    TotalTimeSpentSeconds: issue.TotalTimeSpentSeconds);
            })
            .ToList();

        return items;
    }

    private static List<DashboardIssue> BuildDashboardIssues(IReadOnlyList<GitLabIssueDto> gitLabIssues)
    {
        return gitLabIssues
            .Select(i => new DashboardIssue(
                ProjectName: i.ProjectName,
                MilestoneId: i.MilestoneId,
                MilestoneTitle: i.MilestoneTitle ?? string.Empty,
                Title: i.Title,
                State: i.State,
                AssigneeName: i.AssigneeName ?? string.Empty,
                DueDate: i.DueDate,
                TimeEstimateSeconds: i.TimeEstimateSeconds,
                TotalTimeSpentSeconds: i.TotalTimeSpentSeconds,
                HumanTimeEstimate: CoalesceHumanReadableDuration(i.HumanTimeEstimate, i.TimeEstimateSeconds),
                HumanTotalTimeSpent: CoalesceHumanReadableDuration(i.HumanTotalTimeSpent, i.TotalTimeSpentSeconds),
                Id: i.Iid))
            .ToList();
    }

    private static IReadOnlyList<DashboardMilestone> BuildDashboardMilestones(
        IReadOnlyList<GitLabMilestoneDto> gitLabMilestones,
        IReadOnlyList<DashboardIssue> dashboardIssues)
    {
        var issueStatsByMilestoneId = dashboardIssues
            .Where(i => i.MilestoneId.HasValue)
            .GroupBy(i => i.MilestoneId!.Value)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    OpenIssues = g.Count(i => i.State.Equals("opened", StringComparison.OrdinalIgnoreCase)),
                    ClosedIssues = g.Count(i => i.State.Equals("closed", StringComparison.OrdinalIgnoreCase)),
                    TimeEstimateSeconds = g.Sum(i => i.TimeEstimateSeconds),
                    TotalTimeSpentSeconds = g.Sum(i => i.TotalTimeSpentSeconds)
                });

        var milestones = gitLabMilestones
            .Select(m =>
            {
                var counts = issueStatsByMilestoneId.GetValueOrDefault(m.MilestoneId);

                return new DashboardMilestone(
                    ProjectName: m.ProjectName,
                    Title: m.Title,
                    Scope: m.Scope,
                    State: m.State,
                    StartDate: m.StartDate,
                    DueDate: m.DueDate,
                    OpenIssues: counts?.OpenIssues ?? 0,
                    ClosedIssues: counts?.ClosedIssues ?? 0,
                    TimeEstimateSeconds: counts?.TimeEstimateSeconds ?? 0,
                    TotalTimeSpentSeconds: counts?.TotalTimeSpentSeconds ?? 0,
                    Id: m.MilestoneId);
            })
            .ToList();

        return milestones;
    }

    private static string CoalesceHumanReadableDuration(string? humanDuration, int seconds)
    {
        if (!string.IsNullOrWhiteSpace(humanDuration))
        {
            return humanDuration;
        }

        if (seconds <= 0)
        {
            return "0m";
        }

        var span = TimeSpan.FromSeconds(seconds);
        var days = (int)span.TotalDays;
        var hours = span.Hours;
        var minutes = span.Minutes;

        if (days > 0)
        {
            return $"{days}d {hours}h";
        }

        if (hours > 0)
        {
            return $"{hours}h {minutes}m";
        }

        return $"{minutes}m";
    }
}
