using gitlab_milestone_extensions.ApiService.Models;

namespace gitlab_milestone_extensions.ApiService.Services;

/// <summary>
/// Read-only dashboard data provider backed by a cached GitLab snapshot.
/// </summary>
public sealed class GitLabDashboardDataService(IGitLabDataSnapshotService snapshotService) : IDashboardDataService
{
    public async Task<SelectionOptionsDto> GetSelectionOptionsAsync(
        int? groupId,
        int? memberId,
        int? projectId,
        int? milestoneId,
        CancellationToken cancellationToken)
    {
        var snapshot = await snapshotService.GetSnapshotAsync(cancellationToken);
        var groups = snapshot.Groups
            .Select(g => new SelectorGroupDto(g.GroupId, g.GroupName))
            .OrderBy(g => g.GroupName)
            .ToList();
        var issues = BuildDashboardIssues(snapshot.Issues);
        var hasValidGroup = !groupId.HasValue || groups.Any(g => g.GroupId == groupId.Value);
        if (!hasValidGroup)
        {
            return new SelectionOptionsDto(groups, [], [], []);
        }

        IEnumerable<DashboardIssue> FilterIssues(
            int? selectedMemberId,
            int? selectedProjectId,
            int? selectedMilestoneId)
        {
            var scoped = issues.AsEnumerable();
            if (selectedMemberId.HasValue)
            {
                scoped = scoped.Where(i => i.AssigneeId == selectedMemberId.Value);
            }

            if (selectedProjectId.HasValue)
            {
                scoped = scoped.Where(i => i.ProjectId == selectedProjectId.Value);
            }

            if (selectedMilestoneId.HasValue)
            {
                scoped = scoped.Where(i => i.MilestoneId == selectedMilestoneId.Value);
            }

            return scoped;
        }

        var memberSource = FilterIssues(null, projectId, milestoneId)
            .Where(i => i.AssigneeId.HasValue);
        var members = memberSource
            .GroupBy(i => i.AssigneeId!.Value)
            .Select(g => new SelectorMemberDto(
                g.Key,
                g.Select(i => i.AssigneeName).FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)) ?? $"Member {g.Key}"))
            .OrderBy(m => m.MemberName)
            .ToList();

        var projectIds = FilterIssues(memberId, null, milestoneId)
            .Select(i => i.ProjectId)
            .Distinct()
            .ToHashSet();
        var projects = snapshot.Projects
            .Where(p => projectIds.Contains(p.ProjectId))
            .Select(p => new SelectorProjectDto(p.ProjectId, p.ProjectName))
            .OrderBy(p => p.ProjectName)
            .ToList();

        var milestoneIds = FilterIssues(memberId, projectId, null)
            .Where(i => i.MilestoneId.HasValue)
            .Select(i => i.MilestoneId!.Value)
            .Distinct()
            .ToHashSet();
        var milestones = snapshot.Milestones
            .Where(m => milestoneIds.Contains(m.MilestoneId))
            .GroupBy(m => m.MilestoneId)
            .Select(g => g.First())
            .Select(m => new SelectorMilestoneDto(
                m.MilestoneId,
                m.Title,
                m.ProjectId,
                m.ProjectName,
                m.StartDate,
                m.DueDate))
            .OrderBy(m => m.MilestoneTitle)
            .ToList();

        return new SelectionOptionsDto(groups, members, projects, milestones);
    }

    public async Task<MilestoneDashboardDto?> GetDashboardAsync(int milestoneId, CancellationToken cancellationToken)
    {
        var snapshot = await snapshotService.GetSnapshotAsync(cancellationToken);
        var issues = BuildDashboardIssues(snapshot.Issues)
            .Where(i => i.MilestoneId == milestoneId)
            .ToList();

        if (issues.Count == 0)
        {
            return null;
        }

        var milestone = snapshot.Milestones.FirstOrDefault(m => m.MilestoneId == milestoneId);
        var today = DateOnly.FromDateTime(DateTime.Today);

        return new MilestoneDashboardDto(
            MilestoneId: milestoneId,
            MilestoneTitle: milestone?.Title ?? issues[0].MilestoneTitle,
            MilestoneWebUrl: milestone?.WebUrl,
            TotalIssues: issues.Count,
            OpenIssues: issues.Count(i => i.State.Equals("opened", StringComparison.OrdinalIgnoreCase)),
            ClosedIssues: issues.Count(i => i.State.Equals("closed", StringComparison.OrdinalIgnoreCase)),
            OverdueIssues: issues.Count(i =>
                i.State.Equals("opened", StringComparison.OrdinalIgnoreCase) &&
                i.DueDate.HasValue &&
                i.DueDate.Value < today),
            StartDate: milestone?.StartDate,
            DueDate: milestone?.DueDate,
            EstimateSeconds: issues.Sum(i => i.TimeEstimateSeconds),
            ActualSeconds: issues.Sum(i => i.TotalTimeSpentSeconds));
    }

    public async Task<IReadOnlyList<DashboardIssue>> GetIssuesAsync(int milestoneId, CancellationToken cancellationToken)
    {
        var snapshot = await snapshotService.GetSnapshotAsync(cancellationToken);
        return BuildDashboardIssues(snapshot.Issues)
            .Where(i => i.MilestoneId == milestoneId)
            .ToList();
    }

    public async Task<IReadOnlyList<GanttItemDto>> GetGanttAsync(int milestoneId, CancellationToken cancellationToken)
    {
        var snapshot = await snapshotService.GetSnapshotAsync(cancellationToken);
        var milestone = snapshot.Milestones.FirstOrDefault(m => m.MilestoneId == milestoneId);
        var today = DateOnly.FromDateTime(DateTime.Today);

        return BuildDashboardIssues(snapshot.Issues)
            .Where(i => i.MilestoneId == milestoneId)
            .Select((issue, index) =>
            {
                var endDate = issue.DueDate ?? milestone?.DueDate ?? today;
                var startDate = milestone?.StartDate ?? endDate.AddDays(-7);
                var progress = issue.State.Equals("closed", StringComparison.OrdinalIgnoreCase) ? 100 : 0;

                return new GanttItemDto(
                    Id: issue.Id == 0 ? index + 1 : issue.Id,
                    Title: issue.Title,
                    ViewMode: "milestone",
                    Assignee: issue.AssigneeName,
                    StartDate: startDate,
                    EndDate: endDate,
                    Progress: progress,
                    MilestoneId: issue.MilestoneId,
                    MilestoneTitle: issue.MilestoneTitle,
                    TimeEstimateSeconds: issue.TimeEstimateSeconds,
                    TotalTimeSpentSeconds: issue.TotalTimeSpentSeconds);
            })
            .ToList();
    }

    private static List<DashboardIssue> BuildDashboardIssues(IReadOnlyList<GitLabIssueDto> gitLabIssues)
    {
        return gitLabIssues
            .Select(i => new DashboardIssue(
                ProjectName: i.ProjectName,
                ProjectId: i.ProjectId,
                ProjectUrl: i.ProjectWebUrl,
                MilestoneId: i.MilestoneId,
                MilestoneTitle: i.MilestoneTitle ?? string.Empty,
                Title: i.Title,
                IssueUrl: i.WebUrl,
                State: i.State,
                AssigneeId: i.AssigneeId,
                AssigneeName: i.AssigneeName ?? string.Empty,
                DueDate: i.DueDate,
                TimeEstimateSeconds: i.TimeEstimateSeconds,
                TotalTimeSpentSeconds: i.TotalTimeSpentSeconds,
                HumanTimeEstimate: CoalesceHumanReadableDuration(i.HumanTimeEstimate, i.TimeEstimateSeconds),
                HumanTotalTimeSpent: CoalesceHumanReadableDuration(i.HumanTotalTimeSpent, i.TotalTimeSpentSeconds),
                Id: i.Iid))
            .ToList();
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
