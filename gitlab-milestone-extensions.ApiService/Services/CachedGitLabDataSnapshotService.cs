using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using gitlab_milestone_extensions.ApiService.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace gitlab_milestone_extensions.ApiService.Services;

public sealed class CachedGitLabDataSnapshotService(
    IMemoryCache memoryCache,
    GitLabApiClient gitLabApiClient,
    IHttpContextAccessor httpContextAccessor,
    ILogger<CachedGitLabDataSnapshotService> logger) : IGitLabDataSnapshotService
{
    private const string RequestPrivateTokenHeaderName = "X-GitLab-Private-Token";
    private const string CacheKeyPrefix = "gitlab:data:snapshot:v2:";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private readonly SemaphoreSlim _fetchLock = new(1, 1);

    public Task<IReadOnlyList<GitLabGroupDto>> GetAccessibleGroupsAsync(CancellationToken cancellationToken)
        => gitLabApiClient.GetAccessibleGroupsAsync(cancellationToken);

    public async Task<GitLabDataSnapshot> GetSnapshotAsync(int groupId, CancellationToken cancellationToken)
    {
        var cacheKey = $"{CacheKeyPrefix}{groupId}:{ComputeTokenHash(GetRequiredPrivateToken(httpContextAccessor))}";
        var stopwatch = Stopwatch.StartNew();
        if (memoryCache.TryGetValue<GitLabDataSnapshot>(cacheKey, out var cacheHit) && cacheHit is not null)
        {
            stopwatch.Stop();
            logger.LogInformation(
                "GitLab snapshot cache hit in {ElapsedMs}ms. LoadedAtUtc={LoadedAtUtc}",
                stopwatch.ElapsedMilliseconds,
                cacheHit.LoadedAtUtc);
            return cacheHit;
        }

        await _fetchLock.WaitAsync(cancellationToken);
        try
        {
            if (memoryCache.TryGetValue<GitLabDataSnapshot>(cacheKey, out cacheHit) && cacheHit is not null)
            {
                stopwatch.Stop();
                logger.LogInformation(
                    "GitLab snapshot cache hit(after lock) in {ElapsedMs}ms. LoadedAtUtc={LoadedAtUtc}",
                    stopwatch.ElapsedMilliseconds,
                    cacheHit.LoadedAtUtc);
                return cacheHit;
            }

            var fetchStopwatch = Stopwatch.StartNew();
            var groupTask = gitLabApiClient.GetGroupAsync(groupId, cancellationToken);
            var projects = await gitLabApiClient.GetProjectsAsync(groupId, cancellationToken);
            var projectMilestonesTask = gitLabApiClient.GetProjectMilestonesAsync(projects, cancellationToken);
            var groupMilestonesTask = gitLabApiClient.GetGroupMilestonesAsync(groupId, cancellationToken);
            var issuesTask = gitLabApiClient.GetProjectIssuesAsync(projects, cancellationToken);
            await Task.WhenAll(groupTask, projectMilestonesTask, groupMilestonesTask, issuesTask);

            var groups = new[] { await groupTask };
            var projectMilestones = await projectMilestonesTask;
            var groupMilestones = (await groupMilestonesTask)
                .Select(m => m with
                {
                    WebUrl = groups[0].WebUrl is null ? null : $"{groups[0].WebUrl}/-/milestones/{m.MilestoneId}"
                })
                .ToList();
            var milestones = projectMilestones
                .Concat(groupMilestones)
                .GroupBy(m => new { m.Scope, m.MilestoneId, m.ProjectId, m.ProjectName })
                .Select(g => g.First())
                .ToList();
            var issues = await issuesTask;
            fetchStopwatch.Stop();

            var snapshot = new GitLabDataSnapshot(
                groups,
                projects,
                milestones,
                issues,
                DateTimeOffset.UtcNow);

            memoryCache.Set(cacheKey, snapshot, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheDuration
            });

            stopwatch.Stop();
            logger.LogInformation(
                "GitLab snapshot cache miss. Fetched in {FetchElapsedMs}ms, total {TotalElapsedMs}ms. Projects={Projects}, Milestones={Milestones}, Issues={Issues}",
                fetchStopwatch.ElapsedMilliseconds,
                stopwatch.ElapsedMilliseconds,
                projects.Count,
                milestones.Count,
                issues.Count);

            return snapshot;
        }
        finally
        {
            _fetchLock.Release();
        }
    }

    private static string GetRequiredPrivateToken(IHttpContextAccessor httpContextAccessor)
    {
        var privateToken = httpContextAccessor.HttpContext?.Request.Headers[RequestPrivateTokenHeaderName].ToString();
        if (string.IsNullOrWhiteSpace(privateToken))
        {
            throw new InvalidOperationException("GitLab private token is required.");
        }

        return privateToken;
    }

    private static string ComputeTokenHash(string privateToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(privateToken));
        return Convert.ToHexString(bytes);
    }
}
