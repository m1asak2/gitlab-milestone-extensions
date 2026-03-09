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
        var gantt = await httpClient.GetFromJsonAsync<IReadOnlyList<GanttItemViewModel>>("api/gantt", cancellationToken)
            ?? throw new InvalidOperationException("Failed to load /api/gantt.");

        return new DashboardViewModel(summary, issues, milestones, gantt);
    }
}
