using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using gitlab_milestone_extensions.ApiService.Models;
using gitlab_milestone_extensions.ApiService.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace gitlab_milestone_extensions.ApiService.Services;

public class GitLabApiClient
{
    private const string ClientPrivateTokenHeaderName = "PRIVATE-TOKEN";
    private const string RequestPrivateTokenHeaderName = "X-GitLab-Private-Token";
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<GitLabApiClient> _logger;

    public GitLabApiClient(
        HttpClient httpClient,
        IOptions<GitLabOptions> options,
        IHttpContextAccessor httpContextAccessor,
        ILogger<GitLabApiClient> logger)
    {
        var opt = options.Value;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;

        httpClient.BaseAddress = new Uri($"{opt.BaseUrl.TrimEnd('/')}/api/v4/");
        httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        _httpClient = httpClient;
    }

    public async Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        ApplyPrivateTokenHeader();
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await _httpClient.GetFromJsonAsync<T>(url, cancellationToken);
            stopwatch.Stop();
            _logger.LogInformation(
                "GitLab API GET {Url} completed in {ElapsedMs}ms",
                url,
                stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "GitLab API GET {Url} failed in {ElapsedMs}ms",
                url,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<IReadOnlyList<GitLabGroupDto>> GetAccessibleGroupsAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var groups = await GetPagedAsync<GitLabGroupResponse>(
            "groups?all_available=true",
            cancellationToken);

        var result = groups
            .GroupBy(group => group.Id)
            .Select(group => group.First())
            .OrderBy(group => group.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => new GitLabGroupDto(group.Id, group.Name, group.WebUrl))
            .ToList();

        stopwatch.Stop();
        _logger.LogInformation(
            "GitLab accessible groups fetched in {ElapsedMs}ms. Count={Count}",
            stopwatch.ElapsedMilliseconds,
            result.Count);

        return result;
    }

    public async Task<IReadOnlyList<GitLabProjectDto>> GetProjectsAsync(int groupId, CancellationToken cancellationToken = default)
    {
        ValidateGroupId(groupId);
        var stopwatch = Stopwatch.StartNew();
        var groupProjectsTask = GetPagedAsync<GitLabProjectResponse>(
            $"groups/{groupId}/projects?include_subgroups=true",
            cancellationToken);
        var membershipProjectsTask = GetPagedAsync<GitLabProjectResponse>(
            "projects?membership=true&simple=true",
            cancellationToken);
        await Task.WhenAll(groupProjectsTask, membershipProjectsTask);

        var projects = (await groupProjectsTask)
            .Concat(await membershipProjectsTask)
            .GroupBy(p => p.Id)
            .Select(g => g.First())
            .ToList();
        stopwatch.Stop();
        _logger.LogInformation(
            "GitLab projects fetched in {ElapsedMs}ms. GroupId={GroupId}, Count={Count}",
            stopwatch.ElapsedMilliseconds,
            groupId,
            projects.Count);

        return projects
            .Select(p => new GitLabProjectDto(p.Id, p.Name, p.WebUrl))
            .ToList();
    }

    public async Task<GitLabGroupDto> GetGroupAsync(int groupId, CancellationToken cancellationToken = default)
    {
        ValidateGroupId(groupId);
        var stopwatch = Stopwatch.StartNew();
        var group = await GetAsync<GitLabGroupResponse>($"groups/{groupId}", cancellationToken)
            ?? throw new InvalidOperationException($"GitLab group '{groupId}' was not found.");
        stopwatch.Stop();

        _logger.LogInformation(
            "GitLab group fetched in {ElapsedMs}ms. GroupId={GroupId}",
            stopwatch.ElapsedMilliseconds,
            groupId);

        return new GitLabGroupDto(group.Id, group.Name, group.WebUrl);
    }

    public async Task<GitLabCurrentUserDto> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var user = await GetAsync<GitLabCurrentUserResponse>("user", cancellationToken)
            ?? throw new InvalidOperationException("GitLab user could not be resolved from the provided token.");
        stopwatch.Stop();

        _logger.LogInformation(
            "GitLab current user fetched in {ElapsedMs}ms. UserId={UserId}, Username={Username}",
            stopwatch.ElapsedMilliseconds,
            user.Id,
            user.Username);

        return new GitLabCurrentUserDto(user.Id, user.Name, user.Username, user.AvatarUrl, user.WebUrl);
    }

    public async Task<IReadOnlyList<GitLabMilestoneDto>> GetProjectMilestonesAsync(int groupId, CancellationToken cancellationToken = default)
    {
        var projects = await GetProjectsAsync(groupId, cancellationToken);
        return await GetProjectMilestonesAsync(projects, cancellationToken);
    }

    public async Task<IReadOnlyList<GitLabMilestoneDto>> GetProjectMilestonesAsync(
        IReadOnlyList<GitLabProjectDto> projects,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        var milestoneTasks = projects.Select(async project =>
        {
            var milestones = await GetPagedAsync<GitLabMilestoneResponse>(
                $"projects/{project.ProjectId}/milestones",
                cancellationToken);

            return milestones.Select(m => new GitLabMilestoneDto(
                project.ProjectId,
                project.ProjectName,
                m.Id,
                m.Iid,
                m.Title,
                "Project",
                m.State,
                m.StartDate,
                m.DueDate,
                project.WebUrl is null ? null : $"{project.WebUrl}/-/milestones/{m.Iid}"));
        });

        var resultByProject = await Task.WhenAll(milestoneTasks);
        var results = resultByProject.SelectMany(x => x).ToList();
        stopwatch.Stop();
        _logger.LogInformation(
            "GitLab project milestones fetched in {ElapsedMs}ms. ProjectCount={ProjectCount}, MilestoneCount={MilestoneCount}",
            stopwatch.ElapsedMilliseconds,
            projects.Count,
            results.Count);

        return results;
    }

    public async Task<IReadOnlyList<GitLabMilestoneDto>> GetGroupMilestonesAsync(int groupId, CancellationToken cancellationToken = default)
    {
        ValidateGroupId(groupId);
        var stopwatch = Stopwatch.StartNew();
        var milestones = await GetPagedAsync<GitLabMilestoneResponse>(
            $"groups/{groupId}/milestones",
            cancellationToken);

        var results = milestones
            .Select(m => new GitLabMilestoneDto(
                ProjectId: groupId,
                ProjectName: "Group",
                MilestoneId: m.Id,
                MilestoneIid: m.Id,
                Title: m.Title,
                Scope: "Group",
                State: m.State,
                StartDate: m.StartDate,
                DueDate: m.DueDate,
                WebUrl: null))
            .ToList();

        stopwatch.Stop();
        _logger.LogInformation(
            "GitLab group milestones fetched in {ElapsedMs}ms. GroupId={GroupId}, MilestoneCount={MilestoneCount}",
            stopwatch.ElapsedMilliseconds,
            groupId,
            results.Count);

        return results;
    }

    public async Task<IReadOnlyList<GitLabIssueDto>> GetProjectIssuesAsync(int groupId, CancellationToken cancellationToken = default)
    {
        var projects = await GetProjectsAsync(groupId, cancellationToken);
        return await GetProjectIssuesAsync(projects, cancellationToken);
    }

    public async Task<IReadOnlyList<GitLabIssueDto>> GetProjectIssuesAsync(
        IReadOnlyList<GitLabProjectDto> projects,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        var issueTasks = projects.Select(async project =>
        {
            var issues = await GetPagedAsync<GitLabIssueResponse>(
                $"projects/{project.ProjectId}/issues",
                cancellationToken);

            return issues.Select(i => new GitLabIssueDto(
                project.ProjectId,
                project.ProjectName,
                project.WebUrl,
                i.Id,
                i.Iid,
                i.Title,
                i.WebUrl,
                i.State,
                i.Milestone?.Id,
                i.Milestone?.Title,
                i.Assignee?.Id ?? i.Assignees?.FirstOrDefault()?.Id,
                i.Assignee?.Name ?? i.Assignees?.FirstOrDefault()?.Name,
                i.DueDate,
                i.TimeStats?.TimeEstimate ?? 0,
                i.TimeStats?.TotalTimeSpent ?? 0,
                i.TimeStats?.HumanTimeEstimate,
                i.TimeStats?.HumanTotalTimeSpent));
        });

        var resultByProject = await Task.WhenAll(issueTasks);
        var results = resultByProject.SelectMany(x => x).ToList();
        stopwatch.Stop();
        _logger.LogInformation(
            "GitLab project issues fetched in {ElapsedMs}ms. ProjectCount={ProjectCount}, IssueCount={IssueCount}",
            stopwatch.ElapsedMilliseconds,
            projects.Count,
            results.Count);

        return results;
    }

    private async Task<List<T>> GetPagedAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        ApplyPrivateTokenHeader();
        var allItems = new List<T>();
        var page = 1;

        while (true)
        {
            var separator = url.Contains('?') ? '&' : '?';
            var pagedUrl = $"{url}{separator}per_page=100&page={page}";
            var stopwatch = Stopwatch.StartNew();

            using var response = await _httpClient.GetAsync(pagedUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var pageItems = await response.Content.ReadFromJsonAsync<List<T>>(cancellationToken) ?? [];
            stopwatch.Stop();

            _logger.LogInformation(
                "GitLab API GET {Url} page {Page} loaded in {ElapsedMs}ms. Count={Count}",
                url,
                page,
                stopwatch.ElapsedMilliseconds,
                pageItems.Count);

            allItems.AddRange(pageItems);

            var nextPageHeader = response.Headers.TryGetValues("X-Next-Page", out var values)
                ? values.FirstOrDefault()
                : null;

            if (!string.IsNullOrWhiteSpace(nextPageHeader) && int.TryParse(nextPageHeader, out var nextPage))
            {
                page = nextPage;
                continue;
            }

            if (pageItems.Count < 100)
            {
                break;
            }

            page++;
        }

        return allItems;
    }

    private void ApplyPrivateTokenHeader()
    {
        var privateToken = _httpContextAccessor.HttpContext?.Request.Headers[RequestPrivateTokenHeaderName].ToString();
        if (string.IsNullOrWhiteSpace(privateToken))
        {
            throw new InvalidOperationException("GitLab private token is required.");
        }

        _httpClient.DefaultRequestHeaders.Remove(ClientPrivateTokenHeaderName);
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(ClientPrivateTokenHeaderName, privateToken);
    }

    private static void ValidateGroupId(int groupId)
    {
        if (groupId <= 0)
        {
            throw new InvalidOperationException("A valid GitLab groupId is required.");
        }
    }

    private sealed record GitLabProjectResponse(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("web_url")] string? WebUrl);

    private sealed record GitLabGroupResponse(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("web_url")] string? WebUrl);

    private sealed record GitLabCurrentUserResponse(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("username")] string Username,
        [property: JsonPropertyName("avatar_url")] string? AvatarUrl,
        [property: JsonPropertyName("web_url")] string? WebUrl);

    private sealed record GitLabMilestoneResponse(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("iid")] int Iid,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("state")] string State,
        [property: JsonPropertyName("start_date")] DateOnly? StartDate,
        [property: JsonPropertyName("due_date")] DateOnly? DueDate);

    private sealed record GitLabIssueResponse(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("iid")] int Iid,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("web_url")] string? WebUrl,
        [property: JsonPropertyName("state")] string State,
        [property: JsonPropertyName("due_date")] DateOnly? DueDate,
        [property: JsonPropertyName("milestone")] GitLabIssueMilestoneResponse? Milestone,
        [property: JsonPropertyName("assignee")] GitLabIssueAssigneeResponse? Assignee,
        [property: JsonPropertyName("assignees")] IReadOnlyList<GitLabIssueAssigneeResponse>? Assignees,
        [property: JsonPropertyName("time_stats")] GitLabIssueTimeStatsResponse? TimeStats);

    private sealed record GitLabIssueMilestoneResponse(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("title")] string Title);

    private sealed record GitLabIssueAssigneeResponse(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("name")] string Name);

    private sealed record GitLabIssueTimeStatsResponse(
        [property: JsonPropertyName("time_estimate")] int TimeEstimate,
        [property: JsonPropertyName("total_time_spent")] int TotalTimeSpent,
        [property: JsonPropertyName("human_time_estimate")] string? HumanTimeEstimate,
        [property: JsonPropertyName("human_total_time_spent")] string? HumanTotalTimeSpent);
}
