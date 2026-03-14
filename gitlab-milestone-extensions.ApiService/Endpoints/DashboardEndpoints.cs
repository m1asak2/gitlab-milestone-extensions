using gitlab_milestone_extensions.ApiService.Models;
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

        group.MapGet("/selection/options", async (
            int? groupId,
            int? memberId,
            int? projectId,
            int? milestoneId,
            IDashboardDataService service,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger("DashboardEndpoints");
            var stopwatch = Stopwatch.StartNew();
            var result = await service.GetSelectionOptionsAsync(groupId, memberId, projectId, milestoneId, cancellationToken);
            stopwatch.Stop();
            logger.LogInformation(
                "GET /api/selection/options completed in {ElapsedMs}ms. groupId={GroupId}, memberId={MemberId}, projectId={ProjectId}, milestoneId={MilestoneId}",
                stopwatch.ElapsedMilliseconds,
                groupId?.ToString() ?? "(none)",
                memberId?.ToString() ?? "(none)",
                projectId?.ToString() ?? "(none)",
                milestoneId?.ToString() ?? "(none)");
            return Results.Ok(result);
        })
            .WithName("GetSelectionOptions");

        group.MapGet("/dashboard", async (
            int? groupId,
            int? milestoneId,
            IDashboardDataService service,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            if (!milestoneId.HasValue)
            {
                return Results.BadRequest("milestoneId is required.");
            }

            var logger = loggerFactory.CreateLogger("DashboardEndpoints");
            var stopwatch = Stopwatch.StartNew();
            var result = await service.GetDashboardAsync(groupId, milestoneId.Value, cancellationToken);
            stopwatch.Stop();
            logger.LogInformation(
                "GET /api/dashboard completed in {ElapsedMs}ms. groupId={GroupId}, milestoneId={MilestoneId}",
                stopwatch.ElapsedMilliseconds,
                groupId?.ToString() ?? "(none)",
                milestoneId.Value);

            return result is null ? Results.NotFound() : Results.Ok(result);
        })
            .WithName("GetDashboard");

        group.MapGet("/issues", async (
            int? groupId,
            int? milestoneId,
            IDashboardDataService service,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            if (!milestoneId.HasValue)
            {
                return Results.Ok(Array.Empty<DashboardIssue>());
            }

            var logger = loggerFactory.CreateLogger("DashboardEndpoints");
            var stopwatch = Stopwatch.StartNew();
            var result = await service.GetIssuesAsync(groupId, milestoneId.Value, cancellationToken);
            stopwatch.Stop();
            logger.LogInformation(
                "GET /api/issues completed in {ElapsedMs}ms. groupId={GroupId}, milestoneId={MilestoneId}. Count={Count}",
                stopwatch.ElapsedMilliseconds,
                groupId?.ToString() ?? "(none)",
                milestoneId.Value,
                result.Count);
            return Results.Ok(result);
        })
            .WithName("GetIssues");

        group.MapGet("/gantt", async (
            int? groupId,
            int? milestoneId,
            IDashboardDataService service,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            if (!milestoneId.HasValue)
            {
                return Results.Ok(Array.Empty<GanttItemDto>());
            }

            var logger = loggerFactory.CreateLogger("DashboardEndpoints");
            var stopwatch = Stopwatch.StartNew();
            var result = await service.GetGanttAsync(groupId, milestoneId.Value, cancellationToken);
            stopwatch.Stop();
            logger.LogInformation(
                "GET /api/gantt completed in {ElapsedMs}ms. groupId={GroupId}, milestoneId={MilestoneId}. Count={Count}",
                stopwatch.ElapsedMilliseconds,
                groupId?.ToString() ?? "(none)",
                milestoneId.Value,
                result.Count);
            return Results.Ok(result);
        })
            .WithName("GetGantt");

        return app;
    }
}
