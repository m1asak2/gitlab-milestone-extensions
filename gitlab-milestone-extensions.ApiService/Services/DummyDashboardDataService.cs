using gitlab_milestone_extensions.ApiService.Models;

namespace gitlab_milestone_extensions.ApiService.Services;

/// <summary>
/// Dummy read-only data provider used before real GitLab integration.
/// </summary>
public sealed class DummyDashboardDataService : IDashboardDataService
{
    private static readonly IReadOnlyList<DashboardIssue> Issues =
    [
        new DashboardIssue("Dashboard.Api", 10, "https://gitlab.example.local/dashboard/api", 201, "MVP Sprint 1", "API endpoint skeleton", "https://gitlab.example.local/dashboard/api/-/issues/101", "opened", 1, "Alice", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)), 7200, 1800, "2h", "30m", 101),
        new DashboardIssue("Dashboard.Web", 11, "https://gitlab.example.local/dashboard/web", 201, "MVP Sprint 1", "Blazor dashboard shell", "https://gitlab.example.local/dashboard/web/-/issues/102", "opened", 2, "Bob", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(4)), 10800, 3600, "3h", "1h", 102),
        new DashboardIssue("Dashboard.Web", 11, "https://gitlab.example.local/dashboard/web", 201, "MVP Sprint 1", "Milestone progress card", "https://gitlab.example.local/dashboard/web/-/issues/103", "closed", 3, "Carla", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), 5400, 5400, "1h 30m", "1h 30m", 103),
        new DashboardIssue("Dashboard.Api", 10, "https://gitlab.example.local/dashboard/api", 202, "MVP Sprint 2", "Dummy gantt API data", "https://gitlab.example.local/dashboard/api/-/issues/104", "opened", 1, "Alice", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(6)), 14400, 1800, "4h", "30m", 104),
        new DashboardIssue("Default Group", 4, "https://gitlab.example.local/groups/default", 301, "Group Planning", "Cross-project kickoff", "https://gitlab.example.local/groups/default/-/issues/105", "opened", 4, "Dylan", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)), 3600, 600, "1h", "10m", 105),
        new DashboardIssue("Standalone.Tools", 12, "https://gitlab.example.local/standalone/tools", 401, "Standalone Milestone", "Non-group project task", "https://gitlab.example.local/standalone/tools/-/issues/106", "opened", 5, "Eve", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), 5400, 900, "1h 30m", "15m", 106)
    ];

    private static readonly IReadOnlyList<SelectorGroupDto> Groups =
    [
        new SelectorGroupDto(4, "Default Group")
    ];

    private static readonly IReadOnlyList<SelectorProjectDto> Projects =
    [
        new SelectorProjectDto(10, "Dashboard.Api"),
        new SelectorProjectDto(11, "Dashboard.Web"),
        new SelectorProjectDto(12, "Standalone.Tools")
    ];

    private static readonly IReadOnlyList<SelectorMilestoneDto> Milestones =
    [
        new SelectorMilestoneDto(201, "MVP Sprint 1", 10, "Dashboard.Api", "active", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))),
        new SelectorMilestoneDto(202, "MVP Sprint 2", 10, "Dashboard.Api", "closed", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(8)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(21))),
        new SelectorMilestoneDto(301, "Group Planning", 4, "Default Group", "active", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10))),
        new SelectorMilestoneDto(401, "Standalone Milestone", 12, "Standalone.Tools", "closed", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)))
    ];

    public Task<SelectionOptionsDto> GetSelectionOptionsAsync(
        int? groupId,
        int? memberId,
        int? projectId,
        int? milestoneId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (groupId.HasValue && Groups.All(g => g.GroupId != groupId.Value))
        {
            return Task.FromResult(new SelectionOptionsDto(Groups, [], [], []));
        }

        var availableProjects = groupId.HasValue
            ? Projects.Where(project => project.ProjectId is 10 or 11).ToList()
            : Projects;
        var availableMilestones = groupId.HasValue
            ? Milestones.Where(milestone => milestone.ProjectId is 4 or 10 or 11).ToList()
            : Milestones;

        IEnumerable<DashboardIssue> FilterIssues(
            int? selectedMemberId,
            int? selectedProjectId,
            int? selectedMilestoneId)
        {
            var allowedProjectIds = availableProjects.Select(project => project.ProjectId).ToHashSet();
            var allowedMilestoneIds = availableMilestones.Select(milestone => milestone.MilestoneId).ToHashSet();
            var scoped = Issues
                .Where(issue => allowedProjectIds.Contains(issue.ProjectId))
                .Where(issue => !issue.MilestoneId.HasValue || allowedMilestoneIds.Contains(issue.MilestoneId.Value));
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

        var members = FilterIssues(null, projectId, milestoneId)
            .Where(i => i.AssigneeId.HasValue)
            .GroupBy(i => i.AssigneeId!.Value)
            .Select(g => new SelectorMemberDto(g.Key, g.First().AssigneeName))
            .OrderBy(m => m.MemberName)
            .ToList();

        var scopedProjectIds = FilterIssues(memberId, null, milestoneId)
            .Select(i => i.ProjectId)
            .Distinct()
            .ToHashSet();
        var scopedProjects = availableProjects.Where(p => scopedProjectIds.Contains(p.ProjectId)).ToList();

        var scopedMilestones = availableMilestones
            .Where(m => !projectId.HasValue || m.ProjectId == projectId.Value)
            .Where(m => !memberId.HasValue || Issues.Any(issue =>
                issue.AssigneeId == memberId.Value &&
                issue.MilestoneId == m.MilestoneId))
            .ToList();

        return Task.FromResult(new SelectionOptionsDto(Groups, members, scopedProjects, scopedMilestones));
    }

    public Task<MilestoneDashboardDto?> GetDashboardAsync(int? groupId, int milestoneId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var milestoneIssues = Issues.Where(i => i.MilestoneId == milestoneId).ToList();
        if (milestoneIssues.Count == 0)
        {
            return Task.FromResult<MilestoneDashboardDto?>(null);
        }

        var milestone = Milestones.First(m => m.MilestoneId == milestoneId);
        var today = DateOnly.FromDateTime(DateTime.Today);

        var dto = new MilestoneDashboardDto(
            MilestoneId: milestoneId,
            MilestoneTitle: milestone.MilestoneTitle,
            MilestoneWebUrl: $"{Issues.First(i => i.MilestoneId == milestoneId).ProjectUrl}/-/milestones/{milestoneId}",
            TotalIssues: milestoneIssues.Count,
            OpenIssues: milestoneIssues.Count(i => i.State.Equals("opened", StringComparison.OrdinalIgnoreCase)),
            ClosedIssues: milestoneIssues.Count(i => i.State.Equals("closed", StringComparison.OrdinalIgnoreCase)),
            OverdueIssues: milestoneIssues.Count(i => i.State.Equals("opened", StringComparison.OrdinalIgnoreCase) && i.DueDate.HasValue && i.DueDate.Value < today),
            StartDate: milestone.StartDate,
            DueDate: milestone.DueDate,
            EstimateSeconds: milestoneIssues.Sum(i => i.TimeEstimateSeconds),
            ActualSeconds: milestoneIssues.Sum(i => i.TotalTimeSpentSeconds));

        return Task.FromResult<MilestoneDashboardDto?>(dto);
    }

    public Task<IReadOnlyList<DashboardIssue>> GetIssuesAsync(int? groupId, int milestoneId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<DashboardIssue>>(Issues.Where(i => i.MilestoneId == milestoneId).ToList());
    }

    public Task<IReadOnlyList<GanttItemDto>> GetGanttAsync(int? groupId, int milestoneId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var items = Issues
            .Where(i => i.MilestoneId == milestoneId)
            .Select(i => new GanttItemDto(
                i.Id,
                i.Title,
                "milestone",
                i.AssigneeName,
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
                i.DueDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
                i.State.Equals("closed", StringComparison.OrdinalIgnoreCase) ? 100 : 0,
                i.MilestoneId,
                i.MilestoneTitle,
                i.TimeEstimateSeconds,
                i.TotalTimeSpentSeconds))
            .ToList();

        return Task.FromResult<IReadOnlyList<GanttItemDto>>(items);
    }
}
