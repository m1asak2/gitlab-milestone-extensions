namespace gitlab_milestone_extensions.ApiService.Options;

public sealed class GitLabOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string PrivateToken { get; set; } = string.Empty;
    public int GroupId { get; set; }
}
