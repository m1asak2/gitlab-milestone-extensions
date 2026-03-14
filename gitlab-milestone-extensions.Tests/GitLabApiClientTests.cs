using System.Net;
using System.Text;
using System.Text.Json;
using gitlab_milestone_extensions.ApiService.Models;
using gitlab_milestone_extensions.ApiService.Options;
using gitlab_milestone_extensions.ApiService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace gitlab_milestone_extensions.Tests;

public sealed class GitLabApiClientTests
{
    [Fact]
    public async Task GetCurrentUserAsync_UsesPrivateTokenAndReturnsUser()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            Assert.Equal("/api/v4/user", request.RequestUri?.AbsolutePath);
            Assert.Equal("user-token", request.Headers.GetValues("PRIVATE-TOKEN").Single());

            return CreateJsonResponse(new
            {
                id = 7,
                name = "Misa",
                username = "misa",
                avatar_url = "https://gitlab.example.local/uploads/avatar.png",
                web_url = "https://gitlab.example.local/misa"
            });
        });

        var user = await CreateClient(handler).GetCurrentUserAsync(CancellationToken.None);

        Assert.Equal(7, user.UserId);
        Assert.Equal("Misa", user.Name);
        Assert.Equal("misa", user.Username);
    }

    [Fact]
    public async Task GetProjectMilestonesAsync_UsesMilestoneIidInProjectWebUrl()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            Assert.Equal("/api/v4/projects/101/milestones", request.RequestUri?.AbsolutePath);
            Assert.Equal("per_page=100&page=1", request.RequestUri?.Query.TrimStart('?'));
            Assert.Equal("user-token", request.Headers.GetValues("PRIVATE-TOKEN").Single());

            return CreateJsonResponse(new[]
            {
                new
                {
                    id = 5001,
                    iid = 42,
                    title = "Sprint 42",
                    state = "active",
                    start_date = "2026-03-01",
                    due_date = "2026-03-31"
                }
            });
        });

        var client = CreateClient(handler);
        var projects = new[]
        {
            new GitLabProjectDto(101, "Web", "https://gitlab.example.local/team/web")
        };

        var milestones = await client.GetProjectMilestonesAsync(projects, CancellationToken.None);

        var milestone = Assert.Single(milestones);
        Assert.Equal(5001, milestone.MilestoneId);
        Assert.Equal(42, milestone.MilestoneIid);
        Assert.Equal("https://gitlab.example.local/team/web/-/milestones/42", milestone.WebUrl);
    }

    [Fact]
    public async Task CachedSnapshotService_PopulatesGroupMilestoneWebUrl()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            return request.RequestUri?.AbsolutePath switch
            {
                "/api/v4/groups/4" => CreateJsonResponse(new
                {
                    id = 4,
                    name = "Platform",
                    web_url = "https://gitlab.example.local/groups/platform"
                }),
                "/api/v4/groups/4/projects" => CreateJsonResponse(Array.Empty<object>()),
                "/api/v4/projects" => CreateJsonResponse(Array.Empty<object>()),
                "/api/v4/groups/4/milestones" => CreateJsonResponse(new[]
                {
                    new
                    {
                        id = 301,
                        iid = 17,
                        title = "Group Sprint",
                        state = "active",
                        start_date = "2026-03-01",
                        due_date = "2026-03-31"
                    }
                }),
                _ => throw new Xunit.Sdk.XunitException($"Unexpected request: {request.RequestUri}")
            };
        });

        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var httpContextAccessor = CreateHttpContextAccessor();
        var apiClient = CreateClient(handler, httpContextAccessor);
        var snapshotService = new CachedGitLabDataSnapshotService(
            memoryCache,
            apiClient,
            httpContextAccessor,
            NullLogger<CachedGitLabDataSnapshotService>.Instance);

        var snapshot = await snapshotService.GetSnapshotAsync(4, CancellationToken.None);

        var milestone = Assert.Single(snapshot.Milestones);
        Assert.Equal("https://gitlab.example.local/groups/platform/-/milestones/301", milestone.WebUrl);
    }

    private static GitLabApiClient CreateClient(HttpMessageHandler handler, IHttpContextAccessor? httpContextAccessor = null)
    {
        var httpClient = new HttpClient(handler);
        var options = Options.Create(new GitLabOptions
        {
            BaseUrl = "https://gitlab.example.local"
        });
        httpContextAccessor ??= CreateHttpContextAccessor();

        return new GitLabApiClient(
            httpClient,
            options,
            httpContextAccessor,
            NullLogger<GitLabApiClient>.Instance);
    }

    private static IHttpContextAccessor CreateHttpContextAccessor()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-GitLab-Private-Token"] = "user-token";
        return new HttpContextAccessor
        {
            HttpContext = httpContext
        };
    }

    private static HttpResponseMessage CreateJsonResponse<T>(T value)
    {
        var json = JsonSerializer.Serialize(value);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }
}
