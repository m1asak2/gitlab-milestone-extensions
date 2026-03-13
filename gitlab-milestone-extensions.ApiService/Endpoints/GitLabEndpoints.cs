using gitlab_milestone_extensions.ApiService.Services;

namespace gitlab_milestone_extensions.ApiService.Endpoints;

public static class GitLabEndpoints
{
    public static IEndpointRouteBuilder MapGitLabEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/gitlab");

        group.MapGet("/test", async (GitLabApiClient client, CancellationToken cancellationToken) =>
        {
            var groups = await client.GetAsync<object>("groups", cancellationToken);
            return Results.Ok(groups);
        })
        .WithName("TestGitLabConnection")
        .WithOpenApi();

        group.MapGet("/projects", async (GitLabApiClient client, CancellationToken cancellationToken) =>
        {
            var projects = await client.GetProjectsAsync(cancellationToken);
            return Results.Ok(projects);
        })
        .WithName("GetGitLabProjects")
        .WithOpenApi();

        group.MapGet("/user", async (GitLabApiClient client, CancellationToken cancellationToken) =>
        {
            var user = await client.GetCurrentUserAsync(cancellationToken);
            return Results.Ok(user);
        })
        .WithName("GetGitLabCurrentUser")
        .WithOpenApi();

        group.MapGet("/milestones", async (GitLabApiClient client, CancellationToken cancellationToken) =>
        {
            var milestones = await client.GetProjectMilestonesAsync(cancellationToken);
            return Results.Ok(milestones);
        })
        .WithName("GetGitLabProjectMilestones")
        .WithOpenApi();

        group.MapGet("/issues", async (GitLabApiClient client, CancellationToken cancellationToken) =>
        {
            var issues = await client.GetProjectIssuesAsync(cancellationToken);
            return Results.Ok(issues);
        })
        .WithName("GetGitLabProjectIssues")
        .WithOpenApi();

        return app;
    }
}
