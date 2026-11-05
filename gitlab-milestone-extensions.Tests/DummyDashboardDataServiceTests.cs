using gitlab_milestone_extensions.ApiService.Services;

namespace gitlab_milestone_extensions.Tests;

public sealed class DummyDashboardDataServiceTests
{
    [Fact]
    public async Task GetGanttAsync_ReturnsIssueMetadataAndDateStatus()
    {
        var service = new DummyDashboardDataService();
        var issues = await service.GetIssuesAsync(null, 201, CancellationToken.None);

        var items = await service.GetGanttAsync(null, 201, CancellationToken.None);

        Assert.Equal(issues.Count, items.Count);
        Assert.All(items, item =>
        {
            var issue = Assert.Single(issues, issue => issue.Id == item.Id);
            Assert.Equal("issue", item.ViewMode);
            Assert.Equal(issue.ProjectId, item.ProjectId);
            Assert.Equal(issue.ProjectName, item.ProjectName);
            Assert.Equal(issue.IssueUrl, item.IssueUrl);
            Assert.Equal(issue.MilestoneId, item.MilestoneId);
            Assert.Equal(issue.DueDate, item.EndDate);
            Assert.True(item.StartDateInferred);
            Assert.False(item.EndDateInferred);
            Assert.Equal(item.StartDate <= item.EndDate, item.HasValidDates);
            Assert.Equal(issue.State == "closed" ? 100 : 0, item.Progress);
        });
    }

    [Fact]
    public async Task GetSelectionOptionsAsync_WithoutGroupSelection_ReturnsProjectsForDirectSelection()
    {
        var service = new DummyDashboardDataService();

        var options = await service.GetSelectionOptionsAsync(
            groupId: null,
            memberId: null,
            projectId: null,
            milestoneId: null,
            cancellationToken: CancellationToken.None);

        Assert.NotEmpty(options.Groups);
        Assert.NotEmpty(options.Projects);
        Assert.Contains(options.Projects, p => p.ProjectName == "Standalone.Tools");
        Assert.Contains(options.Milestones, m => m.ProjectName == "Default Group");
        Assert.Contains(options.Milestones, m => m.ProjectName == "Standalone.Tools");
    }

    [Fact]
    public async Task GetSelectionOptionsAsync_WithGroupSelection_ReturnsAtLeastOneGroupMilestone()
    {
        var service = new DummyDashboardDataService();

        var options = await service.GetSelectionOptionsAsync(
            groupId: 4,
            memberId: null,
            projectId: null,
            milestoneId: null,
            cancellationToken: CancellationToken.None);

        Assert.Contains(options.Milestones, m => m.ProjectId == 4 && m.ProjectName == "Default Group");
        Assert.DoesNotContain(options.Milestones, m => m.ProjectName == "Standalone.Tools");
    }

    [Fact]
    public async Task GetSelectionOptionsAsync_WithGroupSelection_ReturnsNonGroupProjectMilestone()
    {
        var service = new DummyDashboardDataService();

        var options = await service.GetSelectionOptionsAsync(
            groupId: 4,
            memberId: null,
            projectId: null,
            milestoneId: null,
            cancellationToken: CancellationToken.None);

        Assert.Contains(options.Milestones, m =>
            m.ProjectId == 10 &&
            m.ProjectName == "Dashboard.Api" &&
            m.MilestoneTitle == "MVP Sprint 1");
    }

    [Fact]
    public async Task GetSelectionOptionsAsync_WithProjectSelection_FiltersMilestonesByProject()
    {
        var service = new DummyDashboardDataService();

        var options = await service.GetSelectionOptionsAsync(
            groupId: 4,
            memberId: null,
            projectId: 10,
            milestoneId: null,
            cancellationToken: CancellationToken.None);

        Assert.All(options.Milestones, milestone => Assert.Equal(10, milestone.ProjectId));
        Assert.DoesNotContain(options.Milestones, milestone => milestone.ProjectId == 4);
    }
}
