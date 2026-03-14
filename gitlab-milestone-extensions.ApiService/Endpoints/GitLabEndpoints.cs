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
        .WithName("TestGitLabConnection");

        group.MapGet("/projects", async (GitLabApiClient client, CancellationToken cancellationToken) =>
        {
            var selectedGroupId = (await client.GetAccessibleGroupsAsync(cancellationToken)).FirstOrDefault()?.GroupId
                ?? throw new InvalidOperationException("No accessible GitLab groups were found for the current token.");
            var projects = await client.GetProjectsAsync(selectedGroupId, cancellationToken);
            return Results.Ok(projects);
        })
        .WithName("GetGitLabProjects");

        group.MapGet("/user", async (GitLabApiClient client, CancellationToken cancellationToken) =>
        {
            var user = await client.GetCurrentUserAsync(cancellationToken);
            return Results.Ok(user);
        })
        .WithName("GetGitLabCurrentUser");

        group.MapGet("/milestones", async (GitLabApiClient client, CancellationToken cancellationToken) =>
        {
            var selectedGroupId = (await client.GetAccessibleGroupsAsync(cancellationToken)).FirstOrDefault()?.GroupId
                ?? throw new InvalidOperationException("No accessible GitLab groups were found for the current token.");
            var milestones = await client.GetProjectMilestonesAsync(selectedGroupId, cancellationToken);
            return Results.Ok(milestones);
        })
        .WithName("GetGitLabProjectMilestones");

        group.MapGet("/issues", async (GitLabApiClient client, CancellationToken cancellationToken) =>
        {
            var selectedGroupId = (await client.GetAccessibleGroupsAsync(cancellationToken)).FirstOrDefault()?.GroupId
                ?? throw new InvalidOperationException("No accessible GitLab groups were found for the current token.");
            var issues = await client.GetProjectIssuesAsync(selectedGroupId, cancellationToken);
            return Results.Ok(issues);
        })
        .WithName("GetGitLabProjectIssues");

        return app;
    }
}
