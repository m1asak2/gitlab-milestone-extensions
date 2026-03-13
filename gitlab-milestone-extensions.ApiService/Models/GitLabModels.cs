namespace gitlab_milestone_extensions.ApiService.Models;

public sealed record GitLabCurrentUserDto(
    int UserId,
    string Name,
    string Username,
    string? AvatarUrl,
    string? WebUrl);

public sealed record GitLabProjectDto(
    int ProjectId,
    string ProjectName,
    string? WebUrl);

public sealed record GitLabGroupDto(
    int GroupId,
    string GroupName,
    string? WebUrl);

public sealed record GitLabMilestoneDto(
    int ProjectId,
    string ProjectName,
    int MilestoneId,
    int MilestoneIid,
    string Title,
    string Scope,
    string State,
    DateOnly? StartDate,
    DateOnly? DueDate,
    string? WebUrl);

public sealed record GitLabIssueDto(
    int ProjectId,
    string ProjectName,
    string? ProjectWebUrl,
    int IssueId,
    int Iid,
    string Title,
    string? WebUrl,
    string State,
    int? MilestoneId,
    string? MilestoneTitle,
    int? AssigneeId,
    string? AssigneeName,
    DateOnly? DueDate,
    int TimeEstimateSeconds,
    int TotalTimeSpentSeconds,
    string? HumanTimeEstimate,
    string? HumanTotalTimeSpent);
