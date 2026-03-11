namespace gitlab_milestone_extensions.Web.Models;

public sealed record SummaryViewModel(int TotalIssues, int OpenIssues, int ClosedIssues, int OverdueIssues);

public sealed record IssueViewModel(
    int Id,
    string Title,
    string ProjectName,
    string Assignee,
    string State,
    string Milestone,
    DateOnly? DueDate,
    int TimeEstimateSeconds,
    int TotalTimeSpentSeconds,
    string? HumanTimeEstimate,
    string? HumanTotalTimeSpent);

public sealed record MilestoneViewModel(
    int Id,
    string Title,
    string ProjectName,
    string Scope,
    DateOnly? StartDate,
    DateOnly? DueDate,
    int Progress,
    int TimeEstimateSeconds,
    int TotalTimeSpentSeconds);

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

public sealed record DashboardViewModel(
    SummaryViewModel Summary,
    IReadOnlyList<IssueViewModel> Issues,
    IReadOnlyList<MilestoneViewModel> Milestones,
    IReadOnlyList<GanttItemViewModel> GanttItems);
