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
    public async Task ActualStarts_VerifiesClosingTargetsAndChoosesEarliestAcrossPages()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path.EndsWith("related_merge_requests"))
            {
                if (request.RequestUri.Query.Contains("page=2")) return CreateJsonResponse(new[] { new { project_id = 12, iid = 3, created_at = "2026-03-05T23:30:00Z" } });
                var response = CreateJsonResponse(new[] {
                    new { project_id = 12, iid = 1, created_at = "2026-03-01T00:00:00Z" },
                    new { project_id = 12, iid = 2, created_at = "2026-03-08T00:00:00Z" } });
                response.Headers.Add("X-Next-Page", "2");
                return response;
            }
            if (path.Contains("/merge_requests/1/")) return CreateJsonResponse(new[] { new { project_id = 99, iid = 101, id = 1001 } });
            return CreateJsonResponse(new[] { new { project_id = 11, iid = 101, id = 1001 } });
        });
        var client = CreateClient(handler);
        var issue = new GitLabIssueDto(11, "Web", null, 1001, 101, "Issue", null, "opened", 201, "Sprint", null, null, null, 0, 0, null, null);

        var starts = await client.GetActualStartsAsync([issue], CancellationToken.None);

        Assert.Equal(DateTimeOffset.Parse("2026-03-05T23:30:00Z"), starts[(11, 101)]);
    }

    [Theory]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(404)]
    public async Task ActualStarts_ReportsApiFailuresInsteadOfMissingData(int status)
    {
        var client = CreateClient(new StubHttpMessageHandler(_ => new HttpResponseMessage((HttpStatusCode)status)));
        var issue = new GitLabIssueDto(11, "Web", null, 1001, 101, "Issue", null, "opened", 201, "Sprint", null, null, null, 0, 0, null, null);
        var error = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetActualStartsAsync([issue], CancellationToken.None));
        Assert.Equal((HttpStatusCode)status, error.StatusCode);
    }

    [Theory]
    [InlineData("2026-03-05")]
    [InlineData(null)]
    public async Task GetProjectIssuesAsync_MapsIssueStartDate(string? startDate)
    {
        var handler = new StubHttpMessageHandler(_ => CreateJsonResponse(new[]
        {
            new { id = 1001, iid = 101, title = "Scheduled issue", state = "opened",
                start_date = startDate, due_date = "2026-03-15" }
        }));
        var client = CreateClient(handler);

        var issue = Assert.Single(await client.GetProjectIssuesAsync(
            new[] { new GitLabProjectDto(11, "Web", null) }, CancellationToken.None));

        Assert.Equal(startDate is null ? (DateOnly?)null : new DateOnly(2026, 3, 5), issue.StartDate);
    }

    [Fact]
    public async Task GetCurrentUserAsync_UsesPrivateTokenAndReturnsUser()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            Assert.Equal("/api/v4/user", request.RequestUri?.AbsolutePath);
            Assert.Equal("server-test-token", request.Headers.GetValues("PRIVATE-TOKEN").Single());

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
            Assert.Equal("server-test-token", request.Headers.GetValues("PRIVATE-TOKEN").Single());

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
            Options.Create(new GitLabOptions { BaseUrl = "https://gitlab.example.local", PrivateToken = "server-test-token" }),
            NullLogger<CachedGitLabDataSnapshotService>.Instance);

        var snapshot = await snapshotService.GetSnapshotAsync(4, CancellationToken.None);

        var milestone = Assert.Single(snapshot.Milestones);
        Assert.Equal("https://gitlab.example.local/groups/platform/-/milestones/301", milestone.WebUrl);
    }

    [Fact]
    public async Task GetProjectIssuesAsync_SkipsProjectWhenARequestFails()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            return request.RequestUri?.AbsolutePath switch
            {
                "/api/v4/projects/101/issues" => CreateJsonResponse(new[]
                {
                    new
                    {
                        id = 1001,
                        iid = 11,
                        title = "Healthy project issue",
                        web_url = "https://gitlab.example.local/team/web/-/issues/11",
                        state = "opened",
                        due_date = "2026-03-20",
                        milestone = (object?)null,
                        assignee = (object?)null,
                        assignees = Array.Empty<object>(),
                        time_stats = new
                        {
                            time_estimate = 0,
                            total_time_spent = 0,
                            human_time_estimate = (string?)null,
                            human_total_time_spent = (string?)null
                        }
                    }
                }),
                "/api/v4/projects/202/issues" => throw new HttpRequestException("The response ended prematurely."),
                _ => throw new Xunit.Sdk.XunitException($"Unexpected request: {request.RequestUri}")
            };
        });

        var client = CreateClient(handler);
        var projects = new[]
        {
            new GitLabProjectDto(101, "Healthy", "https://gitlab.example.local/team/healthy"),
            new GitLabProjectDto(202, "Broken", "https://gitlab.example.local/team/broken")
        };

        var issues = await client.GetProjectIssuesAsync(projects, CancellationToken.None);

        var issue = Assert.Single(issues);
        Assert.Equal(101, issue.ProjectId);
        Assert.Equal("Healthy project issue", issue.Title);
    }

    [Fact]
    public async Task GetProjectsAsync_SkipsGroupWhenARequestFails()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            return request.RequestUri?.AbsolutePath switch
            {
                "/api/v4/groups" => CreateJsonResponse(new[]
                {
                    new { id = 1, name = "Alpha", web_url = "https://gitlab.example.local/groups/alpha" },
                    new { id = 2, name = "Beta", web_url = "https://gitlab.example.local/groups/beta" }
                }),
                "/api/v4/groups/1/projects" => CreateJsonResponse(new[]
                {
                    new { id = 101, name = "Healthy", web_url = "https://gitlab.example.local/team/healthy" }
                }),
                "/api/v4/groups/2/projects" => throw new TaskCanceledException("The operation was canceled."),
                "/api/v4/projects" => CreateJsonResponse(Array.Empty<object>()),
                _ => throw new Xunit.Sdk.XunitException($"Unexpected request: {request.RequestUri}")
            };
        });

        var client = CreateClient(handler);

        var projects = await client.GetProjectsAsync(null, CancellationToken.None);

        var project = Assert.Single(projects);
        Assert.Equal(101, project.ProjectId);
        Assert.Equal("Healthy", project.ProjectName);
    }

    private static GitLabApiClient CreateClient(HttpMessageHandler handler, IHttpContextAccessor? httpContextAccessor = null)
    {
        var httpClient = new HttpClient(handler);
        var options = Options.Create(new GitLabOptions
        {
            PrivateToken = "server-test-token",
            BaseUrl = "https://gitlab.example.local"
        });
        httpContextAccessor ??= CreateHttpContextAccessor();

        return new GitLabApiClient(
            httpClient,
            options,
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
