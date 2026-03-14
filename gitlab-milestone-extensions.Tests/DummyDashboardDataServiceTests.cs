using gitlab_milestone_extensions.ApiService.Services;

namespace gitlab_milestone_extensions.Tests;

public sealed class DummyDashboardDataServiceTests
{
    [Fact]
    public async Task GetSelectionOptionsAsync_WithoutGroupSelection_ReturnsGroupsOnly()
    {
        var service = new DummyDashboardDataService();

        var options = await service.GetSelectionOptionsAsync(
            groupId: null,
            memberId: null,
            projectId: null,
            milestoneId: null,
            cancellationToken: CancellationToken.None);

        Assert.NotEmpty(options.Groups);
        Assert.Empty(options.Members);
        Assert.Empty(options.Projects);
        Assert.Empty(options.Milestones);
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
            m.ProjectId == 12 &&
            m.ProjectName == "Standalone.Tools" &&
            m.MilestoneTitle == "Standalone Milestone");
    }
}
