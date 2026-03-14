using gitlab_milestone_extensions.ApiService.Models;

namespace gitlab_milestone_extensions.ApiService.Services;

public interface IGitLabDataSnapshotService
{
    Task<IReadOnlyList<GitLabGroupDto>> GetAccessibleGroupsAsync(CancellationToken cancellationToken);

    Task<GitLabDataSnapshot> GetSnapshotAsync(int groupId, CancellationToken cancellationToken);
}

public sealed record GitLabDataSnapshot(
    IReadOnlyList<GitLabGroupDto> Groups,
    IReadOnlyList<GitLabProjectDto> Projects,
    IReadOnlyList<GitLabMilestoneDto> Milestones,
    IReadOnlyList<GitLabIssueDto> Issues,
    DateTimeOffset LoadedAtUtc);
