using gitlab_milestone_extensions.ApiService.Services;

namespace gitlab_milestone_extensions.Tests;

public sealed class DummyDashboardDataServiceTests
{
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
