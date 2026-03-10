using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using gitlab_milestone_extensions.ApiService.Models;
using gitlab_milestone_extensions.ApiService.Options;
using Microsoft.Extensions.Options;

namespace gitlab_milestone_extensions.ApiService.Services;

public class GitLabApiClient
{
    private readonly HttpClient _httpClient;
    private readonly int _groupId;

    public GitLabApiClient(HttpClient httpClient, IOptions<GitLabOptions> options)
    {
        var opt = options.Value;
        _groupId = opt.GroupId;

        httpClient.BaseAddress = new Uri($"{opt.BaseUrl.TrimEnd('/')}/api/v4/");
        httpClient.DefaultRequestHeaders.Remove("PRIVATE-TOKEN");
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("PRIVATE-TOKEN", opt.PrivateToken);
        httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        _httpClient = httpClient;
    }

    public Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        return _httpClient.GetFromJsonAsync<T>(url, cancellationToken);
    }

    public async Task<IReadOnlyList<GitLabProjectDto>> GetProjectsAsync(CancellationToken cancellationToken = default)
    {
        var projects = await GetAsync<List<GitLabProjectResponse>>(
            $"groups/{_groupId}/projects?per_page=100",
            cancellationToken) ?? [];

        return projects
            .Select(p => new GitLabProjectDto(p.Id, p.Name))
            .ToList();
    }

    public async Task<IReadOnlyList<GitLabMilestoneDto>> GetProjectMilestonesAsync(CancellationToken cancellationToken = default)
    {
        var projects = await GetProjectsAsync(cancellationToken);

        var milestoneTasks = projects.Select(async project =>
        {
            var milestones = await GetAsync<List<GitLabMilestoneResponse>>(
                $"projects/{project.ProjectId}/milestones?per_page=100",
                cancellationToken) ?? [];

            return milestones.Select(m => new GitLabMilestoneDto(
                project.ProjectId,
                project.ProjectName,
                m.Id,
                m.Title,
                m.State,
                m.StartDate,
                m.DueDate));
        });

        var resultByProject = await Task.WhenAll(milestoneTasks);
        return resultByProject.SelectMany(x => x).ToList();
    }

    public async Task<IReadOnlyList<GitLabIssueDto>> GetProjectIssuesAsync(CancellationToken cancellationToken = default)
    {
        var projects = await GetProjectsAsync(cancellationToken);

        var issueTasks = projects.Select(async project =>
        {
            var issues = await GetAsync<List<GitLabIssueResponse>>(
                $"projects/{project.ProjectId}/issues?per_page=100",
                cancellationToken) ?? [];

            return issues.Select(i => new GitLabIssueDto(
                project.ProjectId,
                project.ProjectName,
                i.Id,
                i.Iid,
                i.Title,
                i.State,
                i.Milestone?.Title,
                i.Assignee?.Name ?? i.Assignees?.FirstOrDefault()?.Name));
        });

        var resultByProject = await Task.WhenAll(issueTasks);
        return resultByProject.SelectMany(x => x).ToList();
    }

    private sealed record GitLabProjectResponse(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("name")] string Name);

    private sealed record GitLabMilestoneResponse(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("state")] string State,
        [property: JsonPropertyName("start_date")] DateOnly? StartDate,
        [property: JsonPropertyName("due_date")] DateOnly? DueDate);

    private sealed record GitLabIssueResponse(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("iid")] int Iid,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("state")] string State,
        [property: JsonPropertyName("milestone")] GitLabIssueMilestoneResponse? Milestone,
        [property: JsonPropertyName("assignee")] GitLabIssueAssigneeResponse? Assignee,
        [property: JsonPropertyName("assignees")] IReadOnlyList<GitLabIssueAssigneeResponse>? Assignees);

    private sealed record GitLabIssueMilestoneResponse(
        [property: JsonPropertyName("title")] string Title);

    private sealed record GitLabIssueAssigneeResponse(
        [property: JsonPropertyName("name")] string Name);
}
