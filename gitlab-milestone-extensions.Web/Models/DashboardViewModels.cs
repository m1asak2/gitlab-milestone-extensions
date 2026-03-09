namespace gitlab_milestone_extensions.Web.Models;

public sealed record SummaryViewModel(int TotalIssues, int OpenIssues, int ClosedIssues, int OverdueIssues);

public sealed record IssueViewModel(int Id, string Title, string ProjectName, string Assignee, string State, string Milestone, DateOnly? DueDate);

public sealed record MilestoneViewModel(int Id, string Title, string ProjectName, DateOnly? StartDate, DateOnly? DueDate, int Progress);

public sealed record GanttItemViewModel(int Id, string Title, string ViewMode, string Owner, DateOnly StartDate, DateOnly EndDate, int Progress);

public sealed record DashboardViewModel(
    SummaryViewModel Summary,
    IReadOnlyList<IssueViewModel> Issues,
    IReadOnlyList<MilestoneViewModel> Milestones,
    IReadOnlyList<GanttItemViewModel> GanttItems);
