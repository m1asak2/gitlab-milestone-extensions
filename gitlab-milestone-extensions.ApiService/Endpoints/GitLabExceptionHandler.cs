using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using gitlab_milestone_extensions.ApiService.Options;

namespace gitlab_milestone_extensions.ApiService.Endpoints;

/// <summary>Returns safe upstream failures without exception details or credentials.</summary>
public sealed class GitLabExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var status = exception switch
        {
            GitLabConfigurationException => StatusCodes.Status503ServiceUnavailable,
            HttpRequestException => StatusCodes.Status502BadGateway,
            _ => StatusCodes.Status500InternalServerError
        };
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = status == 503 ? "GitLab server configuration is unavailable." :
                status == 502 ? "GitLab request failed. Contact the application administrator." : "An unexpected error occurred."
        }, cancellationToken);
        return true;
    }
}
