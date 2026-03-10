namespace gitlab_milestone_extensions.ApiService.Models;

public sealed record GitLabProjectDto(
    int ProjectId,
    string ProjectName);

public sealed record GitLabMilestoneDto(
    int ProjectId,
    string ProjectName,
    int MilestoneId,
    string Title,
    string State,
    DateOnly? StartDate,
    DateOnly? DueDate);

public sealed record GitLabIssueDto(
    int ProjectId,
    string ProjectName,
    int IssueId,
    int Iid,
    string Title,
    string State,
    string? MilestoneTitle,
    string? AssigneeName);
