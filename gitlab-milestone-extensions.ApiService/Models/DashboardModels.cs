namespace gitlab_milestone_extensions.ApiService.Models;

/// <summary>
/// Summary information displayed on the dashboard cards.
/// </summary>
/// <param name="TotalIssues">Total issue count.</param>
/// <param name="OpenIssues">Open issue count.</param>
/// <param name="ClosedIssues">Closed issue count.</param>
/// <param name="OverdueIssues">Overdue issue count.</param>
public sealed record SummaryDto(
    int TotalIssues,
    int OpenIssues,
    int ClosedIssues,
    int OverdueIssues,
    int TotalMilestones = 0);

/// <summary>
/// Issue row used by the issues table.
/// </summary>
/// <param name="Id">Issue id.</param>
/// <param name="Title">Issue title.</param>
/// <param name="ProjectName">Project name.</param>
/// <param name="Assignee">Assignee display name.</param>
/// <param name="State">Issue state.</param>
/// <param name="Milestone">Milestone title.</param>
/// <param name="DueDate">Due date.</param>
public sealed record DashboardIssue(
    string ProjectName,
    int? MilestoneId,
    string MilestoneTitle,
    string Title,
    string State,
    string AssigneeName,
    DateOnly? DueDate,
    int TimeEstimateSeconds,
    int TotalTimeSpentSeconds,
    string? HumanTimeEstimate,
    string? HumanTotalTimeSpent,
    int Id = 0)
{
    public string Assignee => AssigneeName;
    public string Milestone => MilestoneTitle;
}

/// <summary>
/// Milestone row used by the milestone table.
/// </summary>
/// <param name="Id">Milestone id.</param>
/// <param name="Title">Milestone title.</param>
/// <param name="ProjectName">Project name.</param>
/// <param name="StartDate">Start date.</param>
/// <param name="DueDate">Due date.</param>
/// <param name="Progress">Progress percentage from 0 to 100.</param>
public sealed record DashboardMilestone(
    string ProjectName,
    string Title,
    string Scope,
    string State,
    DateOnly? StartDate,
    DateOnly? DueDate,
    int OpenIssues,
    int ClosedIssues,
    int TimeEstimateSeconds,
    int TotalTimeSpentSeconds,
    int Id = 0)
{
    public int Progress
    {
        get
        {
            var total = OpenIssues + ClosedIssues;
            return total == 0 ? 0 : (int)Math.Round((double)ClosedIssues / total * 100);
        }
    }
}

/// <summary>
/// Timeline item for gantt-like visualization.
/// </summary>
/// <param name="Id">Item id.</param>
/// <param name="Title">Item title.</param>
/// <param name="ViewMode">Current grouping mode.</param>
/// <param name="Owner">Owner/group label in the selected mode.</param>
/// <param name="StartDate">Start date.</param>
/// <param name="EndDate">End date.</param>
/// <param name="Progress">Progress percentage from 0 to 100.</param>
public sealed record GanttItemDto(
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
