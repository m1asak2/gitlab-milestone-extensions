using System.Net.Http.Json;
using gitlab_milestone_extensions.Web.Models;

namespace gitlab_milestone_extensions.Web.Services;

public sealed class DashboardApiClient(HttpClient httpClient)
{
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

        return await httpClient.GetFromJsonAsync<SelectionOptionsViewModel>(url, cancellationToken)
            ?? throw new InvalidOperationException($"Failed to load {url}.");
    }

    public async Task<MilestoneDashboardViewModel?> GetDashboardAsync(int milestoneId, CancellationToken cancellationToken = default)
    {
        var url = $"api/dashboard?milestoneId={milestoneId}";
        return await httpClient.GetFromJsonAsync<MilestoneDashboardViewModel>(url, cancellationToken);
    }

    public async Task<IReadOnlyList<IssueViewModel>> GetIssuesAsync(int milestoneId, CancellationToken cancellationToken = default)
    {
        var url = $"api/issues?milestoneId={milestoneId}";
        return await httpClient.GetFromJsonAsync<IReadOnlyList<IssueViewModel>>(url, cancellationToken)
            ?? throw new InvalidOperationException($"Failed to load {url}.");
    }

    public async Task<IReadOnlyList<GanttItemViewModel>> GetGanttAsync(int milestoneId, CancellationToken cancellationToken = default)
    {
        var url = $"api/gantt?milestoneId={milestoneId}";
        return await httpClient.GetFromJsonAsync<IReadOnlyList<GanttItemViewModel>>(url, cancellationToken)
            ?? throw new InvalidOperationException($"Failed to load {url}.");
    }
}
