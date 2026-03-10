using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;

namespace gitlab_milestone_extensions.ApiService.Services;

public sealed class CachedGitLabDataSnapshotService(
    IMemoryCache memoryCache,
    GitLabApiClient gitLabApiClient,
    ILogger<CachedGitLabDataSnapshotService> logger) : IGitLabDataSnapshotService
{
    private const string CacheKey = "gitlab:data:snapshot:v1";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private readonly SemaphoreSlim _fetchLock = new(1, 1);

    public async Task<GitLabDataSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        if (memoryCache.TryGetValue<GitLabDataSnapshot>(CacheKey, out var cacheHit) && cacheHit is not null)
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
            if (memoryCache.TryGetValue<GitLabDataSnapshot>(CacheKey, out cacheHit) && cacheHit is not null)
            {
                stopwatch.Stop();
                logger.LogInformation(
                    "GitLab snapshot cache hit(after lock) in {ElapsedMs}ms. LoadedAtUtc={LoadedAtUtc}",
                    stopwatch.ElapsedMilliseconds,
                    cacheHit.LoadedAtUtc);
                return cacheHit;
            }

            var fetchStopwatch = Stopwatch.StartNew();
            var projects = await gitLabApiClient.GetProjectsAsync(cancellationToken);
            var milestones = await gitLabApiClient.GetProjectMilestonesAsync(projects, cancellationToken);
            var issues = await gitLabApiClient.GetProjectIssuesAsync(projects, cancellationToken);
            fetchStopwatch.Stop();

            var snapshot = new GitLabDataSnapshot(
                projects,
                milestones,
                issues,
                DateTimeOffset.UtcNow);

            memoryCache.Set(CacheKey, snapshot, new MemoryCacheEntryOptions
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
}
