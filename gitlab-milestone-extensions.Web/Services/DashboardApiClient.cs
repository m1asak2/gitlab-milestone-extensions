using System.Net.Http.Json;
using gitlab_milestone_extensions.Web.Models;

namespace gitlab_milestone_extensions.Web.Services;

public sealed class DashboardApiClient(HttpClient httpClient, MilestoneSelectionState selectionState)
{
    private const string RequestPrivateTokenHeaderName = "X-GitLab-Private-Token";

    public Task<GitLabCurrentUserViewModel?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
        => GetFromApiAsync<GitLabCurrentUserViewModel>("api/gitlab/user", cancellationToken);

    public async Task<SelectionOptionsViewModel> GetSelectionOptionsAsync(
        int? groupId,
        int? memberId,
        int? projectId,
        int? milestoneId,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string>();

        if (groupId.HasValue)
        {
            query.Add($"groupId={groupId.Value}");
        }

        if (memberId.HasValue)
        {
            query.Add($"memberId={memberId.Value}");
        }

        if (projectId.HasValue)
        {
            query.Add($"projectId={projectId.Value}");
        }

        if (milestoneId.HasValue)
        {
            query.Add($"milestoneId={milestoneId.Value}");
        }

        var url = query.Count == 0
            ? "api/selection/options"
            : $"api/selection/options?{string.Join("&", query)}";

        return await GetFromApiAsync<SelectionOptionsViewModel>(url, cancellationToken)
            ?? throw new InvalidOperationException($"Failed to load {url}.");
    }

    public async Task<MilestoneDashboardViewModel?> GetDashboardAsync(int milestoneId, CancellationToken cancellationToken = default)
    {
        var url = BuildScopedApiUrl("api/dashboard", milestoneId);
        return await GetFromApiAsync<MilestoneDashboardViewModel>(url, cancellationToken);
    }

    public async Task<IReadOnlyList<IssueViewModel>> GetIssuesAsync(int milestoneId, CancellationToken cancellationToken = default)
    {
        var url = BuildScopedApiUrl("api/issues", milestoneId);
        return await GetFromApiAsync<IReadOnlyList<IssueViewModel>>(url, cancellationToken)
            ?? throw new InvalidOperationException($"Failed to load {url}.");
    }

    public async Task<IReadOnlyList<GanttItemViewModel>> GetGanttAsync(int milestoneId, CancellationToken cancellationToken = default)
    {
        var url = BuildScopedApiUrl("api/gantt", milestoneId);
        return await GetFromApiAsync<IReadOnlyList<GanttItemViewModel>>(url, cancellationToken)
            ?? throw new InvalidOperationException($"Failed to load {url}.");
    }

    private async Task<T?> GetFromApiAsync<T>(string url, CancellationToken cancellationToken)
    {
        if (!selectionState.HasPrivateToken)
        {
            throw new InvalidOperationException("GitLab private token is required.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation(RequestPrivateTokenHeaderName, selectionState.PrivateToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var message = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(message)
                ? $"API request failed: {(int)response.StatusCode} {response.ReasonPhrase}"
                : message);
        }

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
    }

    private string BuildScopedApiUrl(string path, int milestoneId)
    {
        var query = new List<string>();
        if (selectionState.SelectedGroupId.HasValue)
        {
            query.Add($"groupId={selectionState.SelectedGroupId.Value}");
        }

        query.Add($"milestoneId={milestoneId}");
        return $"{path}?{string.Join("&", query)}";
    }
}
