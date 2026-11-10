namespace gitlab_milestone_extensions.ApiService.Options;

public sealed class GitLabOptions
{
    public string TimeZoneId { get; set; } = "Asia/Tokyo";
    public string PrivateToken { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
}
