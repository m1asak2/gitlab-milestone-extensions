using Microsoft.Extensions.Logging;

namespace gitlab_milestone_extensions.Tests;

public class WebTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(120);

    [Fact]
    public async Task GetWebResourceRootReturnsOkStatusCode()
    {
        // Arrange
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(DefaultTimeout);
        var cancellationToken = timeout.Token;

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.gitlab_milestone_extensions_AppHost>(cancellationToken);
        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            // Override the logging filters from the app's configuration
            logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
            logging.AddFilter("Aspire.", LogLevel.Debug);
            logging.AddConsole();
        });
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        // Act
        var httpClient = app.CreateHttpClient("webfrontend");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("webfrontend", cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        using var response = await httpClient.GetAsync("/", cancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        const string cssBundle = "gitlab-milestone-extensions.Web.styles.css";
        Assert.Contains($"href=\"{cssBundle}\"", html);

        using var cssResponse = await httpClient.GetAsync(cssBundle, cancellationToken);
        Assert.Equal(HttpStatusCode.OK, cssResponse.StatusCode);
        Assert.Equal("text/css", cssResponse.Content.Headers.ContentType?.MediaType);
        var css = await cssResponse.Content.ReadAsStringAsync(cancellationToken);
        Assert.Contains(".gantt-board[", css);
        Assert.Contains(".gantt-bar[", css);
        Assert.Contains("display:grid", css);

        using var apiClient = app.CreateHttpClient("apiservice", "http");
        using var healthResponse = await apiClient.GetAsync("/health", cancellationToken);
        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
    }
}
