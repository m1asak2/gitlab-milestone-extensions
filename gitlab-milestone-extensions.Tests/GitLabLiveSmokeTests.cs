using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace gitlab_milestone_extensions.Tests;

public sealed class GitLabLiveSmokeTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(90);
    private const string PrivateTokenEnvVar = "LIVE_GITLAB_TOKEN";

    [Fact]
    public async Task SelectionOptionsEndpoint_ReturnsSuccessAgainstLiveGitLab()
    {
        var privateToken = Environment.GetEnvironmentVariable(PrivateTokenEnvVar);
        if (string.IsNullOrWhiteSpace(privateToken))
        {
            return;
        }

        var cancellationToken = TestContext.Current.CancellationToken;
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.gitlab_milestone_extensions_AppHost>(cancellationToken);
        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
            logging.AddFilter("gitlab_milestone_extensions", LogLevel.Debug);
            logging.AddFilter("Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware", LogLevel.Debug);
        });

        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("apiservice", cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        var httpClient = app.CreateHttpClient("apiservice");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/selection/options");
        request.Headers.TryAddWithoutValidation("X-GitLab-Private-Token", privateToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        TestContext.Current.SendDiagnosticMessage($"Status={(int)response.StatusCode} Body={body}");

        Assert.True(response.IsSuccessStatusCode, body);

        using var json = JsonDocument.Parse(body);
        var milestones = json.RootElement.GetProperty("milestones");
        if (milestones.GetArrayLength() == 0)
        {
            return;
        }

        var milestoneId = milestones[0].GetProperty("milestoneId").GetInt32();
        await AssertSuccessAsync(httpClient, $"/api/dashboard?milestoneId={milestoneId}", privateToken, cancellationToken);
        await AssertSuccessAsync(httpClient, $"/api/issues?milestoneId={milestoneId}", privateToken, cancellationToken);
        await AssertSuccessAsync(httpClient, $"/api/gantt?milestoneId={milestoneId}", privateToken, cancellationToken);
    }

    private static async Task AssertSuccessAsync(HttpClient httpClient, string url, string privateToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("X-GitLab-Private-Token", privateToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        TestContext.Current.SendDiagnosticMessage($"Url={url} Status={(int)response.StatusCode} Body={body}");
        Assert.True(response.IsSuccessStatusCode, $"URL={url}\n{body}");
    }
}
