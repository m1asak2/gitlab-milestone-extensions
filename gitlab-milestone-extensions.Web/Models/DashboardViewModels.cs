namespace gitlab_milestone_extensions.Web.Models;

public sealed record MilestoneDashboardViewModel(
    int MilestoneId,
    string MilestoneTitle,
    int TotalIssues,
    int OpenIssues,
    int ClosedIssues,
    int OverdueIssues,
    DateOnly? StartDate,
    DateOnly? DueDate,
    int EstimateSeconds,
    int ActualSeconds);

public sealed record IssueViewModel(
    int Id,
    string Title,
    string ProjectName,
    int ProjectId,
    int? MilestoneId,
    string Milestone,
    int? AssigneeId,
    string Assignee,
    string State,
    DateOnly? DueDate,
    int TimeEstimateSeconds,
    int TotalTimeSpentSeconds,
    string? HumanTimeEstimate,
    string? HumanTotalTimeSpent);

public sealed record GanttItemViewModel(
    int Id,
    string Title,
    string ViewMode,
    string Owner,
    DateOnly StartDate,
    DateOnly EndDate,
    int Progress,
    int? MilestoneId,
    string? MilestoneTitle,
    int TimeEstimateSeconds,
    int TotalTimeSpentSeconds);

public sealed record SelectionGroupViewModel(
    int GroupId,
    string GroupName);

public sealed record SelectionMemberViewModel(
    int MemberId,
    string MemberName);

public sealed record SelectionProjectViewModel(
    int ProjectId,
    string ProjectName);

public sealed record SelectionMilestoneViewModel(
    int MilestoneId,
    string MilestoneTitle,
    int ProjectId,
    string ProjectName,
    DateOnly? StartDate,
    DateOnly? DueDate);

public sealed record SelectionOptionsViewModel(
    IReadOnlyList<SelectionGroupViewModel> Groups,
    IReadOnlyList<SelectionMemberViewModel> Members,
    IReadOnlyList<SelectionProjectViewModel> Projects,
    IReadOnlyList<SelectionMilestoneViewModel> Milestones);
