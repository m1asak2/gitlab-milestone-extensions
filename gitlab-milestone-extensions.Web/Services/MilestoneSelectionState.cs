namespace gitlab_milestone_extensions.Web.Services;

public sealed class MilestoneSelectionState
{
    public string? PrivateToken { get; private set; }
    public int? SelectedGroupId { get; private set; }
    public int? SelectedMemberId { get; private set; }
    public int? SelectedProjectId { get; private set; }
    public int? SelectedMilestoneId { get; private set; }
    public bool HasPrivateToken => !string.IsNullOrWhiteSpace(PrivateToken);

    public event Action? Changed;

    public void SetPrivateToken(string? privateToken)
    {
        var normalizedToken = string.IsNullOrWhiteSpace(privateToken) ? null : privateToken.Trim();
        if (string.Equals(PrivateToken, normalizedToken, StringComparison.Ordinal))
        {
            return;
        }

        PrivateToken = normalizedToken;
        SelectedGroupId = null;
        SelectedMemberId = null;
        SelectedProjectId = null;
        SelectedMilestoneId = null;
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
