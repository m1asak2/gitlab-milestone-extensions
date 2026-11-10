namespace gitlab_milestone_extensions.ApiService.Options;

public sealed class GitLabOptions
{
    public string PrivateToken { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = string.Empty;
}
