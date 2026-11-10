using System.Security.Cryptography;
using System.Text;

namespace gitlab_milestone_extensions.ApiService.Options;

internal static class GitLabConnectionSettings
{
    public static string Token(GitLabOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.PrivateToken))
            throw new GitLabConfigurationException();
        return options.PrivateToken;
    }

    public static Uri ApiBase(GitLabOptions options)
    {
        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != "http" && uri.Scheme != "https") || !string.IsNullOrEmpty(uri.UserInfo))
            throw new GitLabConfigurationException();
        return new Uri($"{options.BaseUrl.TrimEnd('/')}/api/v4/");
    }

    public static string CacheIdentity(GitLabOptions options)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{ApiBase(options)}\n{Token(options)}")));
}

/// <summary>Signals missing server settings without exposing their values.</summary>
public sealed class GitLabConfigurationException() : Exception("GitLab server configuration is missing or invalid.");
