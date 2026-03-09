using gitlab_milestone_extensions.ApiService.Services;

namespace gitlab_milestone_extensions.ApiService.Endpoints;

/// <summary>
/// Endpoint mappings for dashboard data.
/// </summary>
public static class DashboardEndpoints
{
    /// <summary>
    /// Maps read-only dashboard endpoints.
    /// </summary>
    /// <param name="app">Route builder.</param>
    /// <returns>The route builder.</returns>
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api");

        group.MapGet("/summary", async (IDashboardDataService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetSummaryAsync(cancellationToken)))
            .WithName("GetSummary")
            .WithOpenApi();

        group.MapGet("/issues", async (IDashboardDataService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetIssuesAsync(cancellationToken)))
            .WithName("GetIssues")
            .WithOpenApi();

        group.MapGet("/milestones", async (IDashboardDataService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetMilestonesAsync(cancellationToken)))
            .WithName("GetMilestones")
            .WithOpenApi();

        group.MapGet("/gantt", async (string? viewMode, IDashboardDataService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetGanttAsync(viewMode, cancellationToken)))
            .WithName("GetGantt")
            .WithOpenApi();

        return app;
    }
}
