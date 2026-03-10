using gitlab_milestone_extensions.ApiService.Services;
using System.Diagnostics;

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

        group.MapGet("/summary", async (IDashboardDataService service, ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger("DashboardEndpoints");
            var stopwatch = Stopwatch.StartNew();
            var result = await service.GetSummaryAsync(cancellationToken);
            stopwatch.Stop();
            logger.LogInformation("GET /api/summary completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            return Results.Ok(result);
        })
            .WithName("GetSummary")
            .WithOpenApi();

        group.MapGet("/issues", async (IDashboardDataService service, ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger("DashboardEndpoints");
            var stopwatch = Stopwatch.StartNew();
            var result = await service.GetIssuesAsync(cancellationToken);
            stopwatch.Stop();
            logger.LogInformation("GET /api/issues completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            return Results.Ok(result);
        })
            .WithName("GetIssues")
            .WithOpenApi();

        group.MapGet("/milestones", async (IDashboardDataService service, ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger("DashboardEndpoints");
            var stopwatch = Stopwatch.StartNew();
            var result = await service.GetMilestonesAsync(cancellationToken);
            stopwatch.Stop();
            logger.LogInformation("GET /api/milestones completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            return Results.Ok(result);
        })
            .WithName("GetMilestones")
            .WithOpenApi();

        group.MapGet("/gantt", async (string? viewMode, IDashboardDataService service, ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger("DashboardEndpoints");
            var stopwatch = Stopwatch.StartNew();
            var result = await service.GetGanttAsync(viewMode, cancellationToken);
            stopwatch.Stop();
            logger.LogInformation("GET /api/gantt completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            return Results.Ok(result);
        })
            .WithName("GetGantt")
            .WithOpenApi();

        return app;
    }
}
