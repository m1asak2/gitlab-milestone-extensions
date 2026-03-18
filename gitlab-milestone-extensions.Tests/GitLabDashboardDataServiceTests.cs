using gitlab_milestone_extensions.ApiService.Models;
using gitlab_milestone_extensions.ApiService.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace gitlab_milestone_extensions.Tests;

public sealed class GitLabDashboardDataServiceTests
{
    [Fact]
    public async Task GetDashboardAsync_ReturnsMilestoneWebUrl()
    {
        var service = new GitLabDashboardDataService(
            new StubSnapshotService(CreateSnapshot()),
            NullLogger<GitLabDashboardDataService>.Instance);

        var result = await service.GetDashboardAsync(null, 201, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Sprint 1", result.MilestoneTitle);
        Assert.Equal("https://gitlab.example.local/team/web/-/milestones/42", result.MilestoneWebUrl);
        Assert.Equal(2, result.TotalIssues);
        Assert.Equal(7200, result.EstimateSeconds);
        Assert.Equal(3600, result.ActualSeconds);
    }

    [Fact]
    public async Task GetIssuesAsync_MapsIssueAndProjectUrls()
    {
        var service = new GitLabDashboardDataService(
            new StubSnapshotService(CreateSnapshot()),
            NullLogger<GitLabDashboardDataService>.Instance);

        var issues = await service.GetIssuesAsync(null, 201, CancellationToken.None);

        Assert.Equal(2, issues.Count);

        var first = issues[0];
        Assert.Equal(101, first.Id);
        Assert.Equal("https://gitlab.example.local/team/web", first.ProjectUrl);
        Assert.Equal("https://gitlab.example.local/team/web/-/issues/101", first.IssueUrl);
    }

    private static GitLabDataSnapshot CreateSnapshot()
    {
        var groups = new[]
        {
            new GitLabGroupDto(4, "Platform", "https://gitlab.example.local/groups/platform")
        };

        var projects = new[]
        {
            new GitLabProjectDto(11, "Web", "https://gitlab.example.local/team/web")
        };

        var milestones = new[]
        {
            new GitLabMilestoneDto(
                ProjectId: 11,
                ProjectName: "Web",
                MilestoneId: 201,
                MilestoneIid: 42,
                Title: "Sprint 1",
                Scope: "Project",
                State: "active",
                StartDate: new DateOnly(2026, 3, 1),
                DueDate: new DateOnly(2026, 3, 31),
                WebUrl: "https://gitlab.example.local/team/web/-/milestones/42")
        };

        var issues = new[]
        {
            new GitLabIssueDto(
                ProjectId: 11,
                ProjectName: "Web",
                ProjectWebUrl: "https://gitlab.example.local/team/web",
                IssueId: 1001,
                Iid: 101,
                Title: "Build dashboard cards",
                WebUrl: "https://gitlab.example.local/team/web/-/issues/101",
                State: "opened",
                MilestoneId: 201,
                MilestoneTitle: "Sprint 1",
                AssigneeId: 1,
                AssigneeName: "Alice",
                DueDate: new DateOnly(2026, 3, 15),
                TimeEstimateSeconds: 3600,
                TotalTimeSpentSeconds: 1800,
                HumanTimeEstimate: "1h",
                HumanTotalTimeSpent: "30m"),
            new GitLabIssueDto(
                ProjectId: 11,
                ProjectName: "Web",
                ProjectWebUrl: "https://gitlab.example.local/team/web",
                IssueId: 1002,
                Iid: 102,
                Title: "Add issue links",
                WebUrl: "https://gitlab.example.local/team/web/-/issues/102",
                State: "closed",
                MilestoneId: 201,
                MilestoneTitle: "Sprint 1",
                AssigneeId: 2,
                AssigneeName: "Bob",
                DueDate: new DateOnly(2026, 3, 20),
                TimeEstimateSeconds: 3600,
                TotalTimeSpentSeconds: 1800,
                HumanTimeEstimate: "1h",
                HumanTotalTimeSpent: "30m")
        };

        return new GitLabDataSnapshot(groups, projects, milestones, issues, DateTimeOffset.UtcNow);
    }

    private sealed class StubSnapshotService(GitLabDataSnapshot snapshot) : IGitLabDataSnapshotService
    {
        public Task<IReadOnlyList<GitLabGroupDto>> GetAccessibleGroupsAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<GitLabGroupDto>>(snapshot.Groups);
        }

        public Task<GitLabDataSnapshot> GetSnapshotAsync(int? groupId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(snapshot);
        }
    }
}
