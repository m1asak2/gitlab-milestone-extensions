using gitlab_milestone_extensions.ApiService.Models;

namespace gitlab_milestone_extensions.ApiService.Services;

public interface IGitLabDataSnapshotService
{
    Task<GitLabDataSnapshot> GetSnapshotAsync(CancellationToken cancellationToken);
}

public sealed record GitLabDataSnapshot(
    IReadOnlyList<GitLabGroupDto> Groups,
    IReadOnlyList<GitLabProjectDto> Projects,
    IReadOnlyList<GitLabMilestoneDto> Milestones,
    IReadOnlyList<GitLabIssueDto> Issues,
    DateTimeOffset LoadedAtUtc);
