using gitlab_milestone_extensions.ApiService.Models;
using gitlab_milestone_extensions.ApiService.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace gitlab_milestone_extensions.Tests;

public sealed class GitLabDashboardDataServiceTests
{
    [Theory]
    [InlineData("opened", true, false, true)]
    [InlineData("closed", true, true, false)]
    [InlineData("closed", true, false, false)]
    [InlineData("opened", false, false, true)]
    public async Task Gantt_SeparatesActualDatesFromInvalidPlan(string state, bool hasMr, bool hasClosedAt, bool ongoing)
    {
        var source = CreateSnapshot();
        var issue = source.Issues[0] with { State = state, ClosedAt = hasClosedAt ? DateTimeOffset.Parse("2026-03-10T23:30:00Z") : null };
        var snapshot = source with { Issues = [issue], Milestones = [source.Milestones[0] with { StartDate = null }] };
        var starts = new Dictionary<(int, int), DateTimeOffset?> { [(11, 101)] = hasMr ? DateTimeOffset.Parse("2026-03-05T23:30:00Z") : null };
        var service = new GitLabDashboardDataService(new StubSnapshotService(snapshot, starts), NullLogger<GitLabDashboardDataService>.Instance);

        var item = Assert.Single(await service.GetGanttAsync(null, 201, CancellationToken.None));

        Assert.False(item.HasValidDates);
        Assert.Null(item.PlannedStartDate);
        Assert.Equal(hasMr ? new DateOnly(2026, 3, 6) : (DateOnly?)null, item.ActualStartDate);
        Assert.Equal(hasClosedAt ? new DateOnly(2026, 3, 11) : (DateOnly?)null, item.ActualEndDate);
        Assert.Equal(ongoing, item.IsInProgress);
        Assert.Equal(issue.Title, item.Title);
    }

    [Theory]
    [InlineData(5, 15, true)]
    [InlineData(10, 20, true)]
    [InlineData(null, 15, true)]
    [InlineData(20, 15, true)]
    [InlineData(5, null, true)]
    public async Task GetGanttAsync_UsesMilestonePlannedStartAndIssueDueDate(int? startDay, int? endDay, bool valid)
    {
        var original = CreateSnapshot();
        var issue = original.Issues[0] with
        {
            StartDate = startDay.HasValue ? new DateOnly(2026, 3, startDay.Value) : null,
            DueDate = endDay.HasValue ? new DateOnly(2026, 3, endDay.Value) : null
        };
        var service = new GitLabDashboardDataService(
            new StubSnapshotService(original with { Issues = [issue] }),
            NullLogger<GitLabDashboardDataService>.Instance);

        var item = Assert.Single(await service.GetGanttAsync(null, 201, CancellationToken.None));

        Assert.Equal(valid, item.HasValidDates);
        Assert.True(item.StartDateInferred);
        Assert.Equal(!endDay.HasValue, item.EndDateInferred);
        Assert.Equal(original.Milestones[0].StartDate, item.PlannedStartDate);
        Assert.Equal(issue.DueDate ?? original.Milestones[0].DueDate, item.EndDate);
    }

    [Fact]
    public async Task GetGanttAsync_UsesSamePlannedStartForSameMilestone()
    {
        var original = CreateSnapshot();
        var snapshot = original with
        {
            Issues = [original.Issues[0] with { StartDate = new DateOnly(2026, 3, 5) },
                      original.Issues[1] with { StartDate = new DateOnly(2026, 3, 10) }]
        };
        var service = new GitLabDashboardDataService(new StubSnapshotService(snapshot),
            NullLogger<GitLabDashboardDataService>.Instance);

        var items = await service.GetGanttAsync(null, 201, CancellationToken.None);

        Assert.Equal(new[] { new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 1) }, items.Select(i => i.StartDate));
        Assert.All(items, item => Assert.True(item.HasValidDates));
    }

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
    public async Task GetIssuesAsync_UsesGitLabWorkdayConversionWhenHumanDurationMissing()
    {
        var snapshot = CreateSnapshot() with
        {
            Issues =
            [
                new GitLabIssueDto(
                    ProjectId: 11,
                    ProjectName: "Web",
                    ProjectWebUrl: "https://gitlab.example.local/team/web",
                    IssueId: 1003,
                    Iid: 103,
                    Title: "Convert duration format",
                    WebUrl: "https://gitlab.example.local/team/web/-/issues/103",
                    State: "opened",
                    MilestoneId: 201,
                    MilestoneTitle: "Sprint 1",
                    AssigneeId: 3,
                    AssigneeName: "Charlie",
                    DueDate: new DateOnly(2026, 3, 25),
                    TimeEstimateSeconds: 144000,
                    TotalTimeSpentSeconds: 31800,
                    HumanTimeEstimate: null,
                    HumanTotalTimeSpent: null)
            ]
        };

        var service = new GitLabDashboardDataService(
            new StubSnapshotService(snapshot),
            NullLogger<GitLabDashboardDataService>.Instance);

        var issues = await service.GetIssuesAsync(null, 201, CancellationToken.None);

        var issue = Assert.Single(issues);
        Assert.Equal("1w", issue.HumanTimeEstimate);
        Assert.Equal("1d 50m", issue.HumanTotalTimeSpent);
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

    private sealed class StubSnapshotService(GitLabDataSnapshot snapshot, IReadOnlyDictionary<(int, int), DateTimeOffset?>? starts = null) : IGitLabDataSnapshotService
    {
        public Task<IReadOnlyDictionary<(int ProjectId, int Iid), DateTimeOffset?>> GetActualStartsAsync(IReadOnlyList<GitLabIssueDto> issues, CancellationToken cancellationToken)
            => Task.FromResult(starts ?? (IReadOnlyDictionary<(int, int), DateTimeOffset?>)new Dictionary<(int, int), DateTimeOffset?>());

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
