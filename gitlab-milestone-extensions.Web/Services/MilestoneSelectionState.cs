namespace gitlab_milestone_extensions.Web.Services;

public sealed class MilestoneSelectionState
{
    public int? SelectedGroupId { get; private set; }
    public int? SelectedMemberId { get; private set; }
    public int? SelectedProjectId { get; private set; }
    public int? SelectedMilestoneId { get; private set; }

    public event Action? Changed;

    public void SetSelection(int? groupId, int? memberId, int? projectId, int? milestoneId)
    {
        if (SelectedGroupId == groupId &&
            SelectedMemberId == memberId &&
            SelectedProjectId == projectId &&
            SelectedMilestoneId == milestoneId)
        {
            return;
        }

        SelectedGroupId = groupId;
        SelectedMemberId = memberId;
        SelectedProjectId = projectId;
        SelectedMilestoneId = milestoneId;
        Changed?.Invoke();
    }

    public void SetGroup(int? groupId)
    {
        if (SelectedGroupId == groupId)
        {
            return;
        }

        SelectedGroupId = groupId;
        Changed?.Invoke();
    }

    public void SetMember(int? memberId)
    {
        if (SelectedMemberId == memberId)
        {
            return;
        }

        SelectedMemberId = memberId;
        Changed?.Invoke();
    }

    public void SetProject(int? projectId)
    {
        if (SelectedProjectId == projectId)
        {
            return;
        }

        SelectedProjectId = projectId;
        Changed?.Invoke();
    }

    public void SetMilestone(int? milestoneId)
    {
        if (SelectedMilestoneId == milestoneId)
        {
            return;
        }

        SelectedMilestoneId = milestoneId;
        Changed?.Invoke();
    }
}
