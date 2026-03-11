using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using gitlab_milestone_extensions.ApiService.Models;
using gitlab_milestone_extensions.ApiService.Options;
using Microsoft.Extensions.Options;

namespace gitlab_milestone_extensions.ApiService.Services;

public class GitLabApiClient
{
    private readonly HttpClient _httpClient;
    private readonly int _groupId;
    private readonly ILogger<GitLabApiClient> _logger;

    public GitLabApiClient(HttpClient httpClient, IOptions<GitLabOptions> options, ILogger<GitLabApiClient> logger)
    {
        var opt = options.Value;
        _groupId = opt.GroupId;
        _logger = logger;

        httpClient.BaseAddress = new Uri($"{opt.BaseUrl.TrimEnd('/')}/api/v4/");
        httpClient.DefaultRequestHeaders.Remove("PRIVATE-TOKEN");
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("PRIVATE-TOKEN", opt.PrivateToken);
        httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        _httpClient = httpClient;
    }

    public async Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken = default)
    {
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

    public async Task<IReadOnlyList<GitLabProjectDto>> GetProjectsAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var projects = await GetPagedAsync<GitLabProjectResponse>(
            $"groups/{_groupId}/projects",
            cancellationToken);
        stopwatch.Stop();
        _logger.LogInformation(
            "GitLab projects fetched in {ElapsedMs}ms. Count={Count}",
            stopwatch.ElapsedMilliseconds,
            projects.Count);

        return projects
            .Select(p => new GitLabProjectDto(p.Id, p.Name))
            .ToList();
    }

    public async Task<IReadOnlyList<GitLabMilestoneDto>> GetProjectMilestonesAsync(CancellationToken cancellationToken = default)
    {
        var projects = await GetProjectsAsync(cancellationToken);
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
                m.Title,
                m.State,
                m.StartDate,
                m.DueDate));
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

    public async Task<IReadOnlyList<GitLabIssueDto>> GetProjectIssuesAsync(CancellationToken cancellationToken = default)
    {
        var projects = await GetProjectsAsync(cancellationToken);
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
                i.Id,
                i.Iid,
                i.Title,
                i.State,
                i.Milestone?.Title,
                i.Assignee?.Name ?? i.Assignees?.FirstOrDefault()?.Name,
                i.DueDate));
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
        [property: JsonPropertyName("due_date")] DateOnly? DueDate,
        [property: JsonPropertyName("milestone")] GitLabIssueMilestoneResponse? Milestone,
        [property: JsonPropertyName("assignee")] GitLabIssueAssigneeResponse? Assignee,
        [property: JsonPropertyName("assignees")] IReadOnlyList<GitLabIssueAssigneeResponse>? Assignees);

    private sealed record GitLabIssueMilestoneResponse(
        [property: JsonPropertyName("title")] string Title);

    private sealed record GitLabIssueAssigneeResponse(
        [property: JsonPropertyName("name")] string Name);
}
