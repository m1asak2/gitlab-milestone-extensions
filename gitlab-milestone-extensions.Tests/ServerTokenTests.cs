using Aspire.Hosting;
using gitlab_milestone_extensions.ApiService.Options;
using gitlab_milestone_extensions.ApiService.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace gitlab_milestone_extensions.Tests;

public sealed class ServerTokenTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ApiUsesServerTokenIgnoresBrowserTokenAndSanitizesFailures(bool missingConfiguration)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        await using var upstream = builder.Build();
        string? received = null;
        var status = 200;
        upstream.MapGet("/api/v4/user", (HttpContext context) =>
        {
            received = context.Request.Headers["PRIVATE-TOKEN"];
            return status == 200
                ? Results.Json(new { id = 1, name = "Service account", username = "reader" })
                : Results.StatusCode(status);
        });
        await upstream.StartAsync(cancellationToken);
        var url = upstream.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        var host = await DistributedApplicationTestingBuilder.CreateAsync<Projects.gitlab_milestone_extensions_AppHost>(cancellationToken);
        host.CreateResourceBuilder<ProjectResource>("apiservice")
            .WithEnvironment("GitLab__BaseUrl", url)
            .WithEnvironment("GitLab__PrivateToken", missingConfiguration ? "" : "server-only-test-credential");
        await using var app = await host.BuildAsync(cancellationToken);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(2));
        await app.StartAsync(timeout.Token);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("apiservice", timeout.Token);
        using var client = app.CreateHttpClient("apiservice", "http");
        client.DefaultRequestHeaders.Add("X-GitLab-Private-Token", "browser-spoofed-test-token");
        client.DefaultRequestHeaders.Add("Origin", "https://untrusted.example");
        using var success = await client.GetAsync("/api/gitlab/user", timeout.Token);
        if (missingConfiguration)
        {
            Assert.Equal(HttpStatusCode.ServiceUnavailable, success.StatusCode);
            Assert.Null(received);
            var unavailable = await success.Content.ReadAsStringAsync(timeout.Token);
            Assert.DoesNotContain("browser-spoofed-test-token", unavailable);
            Assert.DoesNotContain("stack", unavailable, StringComparison.OrdinalIgnoreCase);
            return;
        }
        Assert.Equal(HttpStatusCode.OK, success.StatusCode);
        Assert.Equal("server-only-test-credential", received);
        Assert.False(success.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.DoesNotContain("server-only-test-credential", await success.Content.ReadAsStringAsync(timeout.Token));
        foreach (var rejected in new[] { 401, 403 })
        {
            status = rejected;
            using var response = await client.GetAsync("/api/gitlab/user", timeout.Token);
            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync(timeout.Token);
            Assert.DoesNotContain("server-only-test-credential", body);
            Assert.DoesNotContain("browser-spoofed-test-token", body);
            Assert.DoesNotContain("stack", body, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("invalid\r\ncredential")]
    public void MissingServerTokenFailsExplicitly(string token)
    {
        var options = Options.Create(new GitLabOptions { BaseUrl = "https://gitlab.example", PrivateToken = token });
        Assert.Throws<GitLabConfigurationException>(() => new GitLabApiClient(new HttpClient(), options, NullLogger<GitLabApiClient>.Instance));
    }

    [Theory]
    [InlineData("https://user:secret@gitlab.example")]
    [InlineData("https://gitlab.example?private_token=secret")]
    [InlineData("ftp://gitlab.example")]
    public void InvalidServerUrlFailsWithoutEchoingSettings(string baseUrl)
    {
        var options = Options.Create(new GitLabOptions { BaseUrl = baseUrl, PrivateToken = "server-only-test-credential" });
        var error = Assert.Throws<GitLabConfigurationException>(() => new GitLabApiClient(new HttpClient(), options, NullLogger<GitLabApiClient>.Instance));
        Assert.DoesNotContain("secret", error.Message);
        Assert.DoesNotContain("server-only-test-credential", error.Message);
    }

    [Fact]
    public async Task CacheIsReusedForSameServerButIsolatedAcrossTokenRotation()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var requests = 0;
        async Task Fetch(string token)
        {
            var options = Options.Create(new GitLabOptions { BaseUrl = "https://gitlab.example", PrivateToken = token });
            using var http = new HttpClient(new EmptyGitLabHandler(() => Interlocked.Increment(ref requests)));
            var api = new GitLabApiClient(http, options, NullLogger<GitLabApiClient>.Instance);
            var snapshots = new CachedGitLabDataSnapshotService(cache, api, options, NullLogger<CachedGitLabDataSnapshotService>.Instance);
            await snapshots.GetSnapshotAsync(null, CancellationToken.None);
        }
        await Fetch("first-test-credential");
        var firstCount = requests;
        Assert.True(firstCount > 0);
        await Fetch("first-test-credential");
        Assert.Equal(firstCount, requests);
        await Fetch("rotated-test-credential");
        Assert.True(requests > firstCount);
    }

    [Fact]
    public async Task ActualMrCacheUsesServerSettingsAndInvalidatesOnRotation()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var requests = 0;
        var issue = new gitlab_milestone_extensions.ApiService.Models.GitLabIssueDto(11, "Web", null, 1001, 101, "Issue", null, "opened", 201, "Sprint", null, null, null, 0, 0, null, null);
        async Task Fetch(string token)
        {
            var options = Options.Create(new GitLabOptions { BaseUrl = "https://gitlab.example", PrivateToken = token });
            using var http = new HttpClient(new EmptyGitLabHandler(() => Interlocked.Increment(ref requests)));
            var api = new GitLabApiClient(http, options, NullLogger<GitLabApiClient>.Instance);
            var snapshots = new CachedGitLabDataSnapshotService(cache, api, options, NullLogger<CachedGitLabDataSnapshotService>.Instance);
            var starts = await snapshots.GetActualStartsAsync([issue], CancellationToken.None);
            Assert.Null(starts[(11, 101)]);
        }
        await Fetch("first-test-credential");
        Assert.Equal(1, requests);
        await Fetch("first-test-credential");
        Assert.Equal(1, requests);
        await Fetch("rotated-test-credential");
        Assert.Equal(2, requests);
    }

    private sealed class EmptyGitLabHandler(Action requested) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            requested();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[]", System.Text.Encoding.UTF8, "application/json") });
        }
    }
}
