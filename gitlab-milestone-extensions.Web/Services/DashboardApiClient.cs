using System.Net.Http.Json;
using gitlab_milestone_extensions.Web.Models;

namespace gitlab_milestone_extensions.Web.Services;

public sealed class DashboardApiClient(HttpClient httpClient)
{
    public async Task<DashboardViewModel> GetDashboardDataAsync(CancellationToken cancellationToken = default)
    {
        var summary = await httpClient.GetFromJsonAsync<SummaryViewModel>("api/summary", cancellationToken)
            ?? throw new InvalidOperationException("Failed to load /api/summary.");
        var issues = await httpClient.GetFromJsonAsync<IReadOnlyList<IssueViewModel>>("api/issues", cancellationToken)
            ?? throw new InvalidOperationException("Failed to load /api/issues.");
        var milestones = await httpClient.GetFromJsonAsync<IReadOnlyList<MilestoneViewModel>>("api/milestones", cancellationToken)
            ?? throw new InvalidOperationException("Failed to load /api/milestones.");
        var gantt = await GetGanttAsync(null, null, cancellationToken);

        return new DashboardViewModel(summary, issues, milestones, gantt);
    }

    public async Task<IReadOnlyList<GanttItemViewModel>> GetGanttAsync(
        string? milestone,
        int? milestoneId,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(milestone))
        {
            query.Add($"milestone={Uri.EscapeDataString(milestone)}");
        }

        if (milestoneId.HasValue)
        {
            query.Add($"milestoneId={milestoneId.Value}");
        }

        var url = query.Count == 0 ? "api/gantt" : $"api/gantt?{string.Join("&", query)}";

        return await httpClient.GetFromJsonAsync<IReadOnlyList<GanttItemViewModel>>(url, cancellationToken)
            ?? throw new InvalidOperationException($"Failed to load {url}.");
    }
}
