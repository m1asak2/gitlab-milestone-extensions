using Microsoft.JSInterop;

namespace gitlab_milestone_extensions.Web.Services;

public sealed class PrivateTokenStorage(IJSRuntime jsRuntime)
{
    private const string TokenStorageKey = "gitlab.privateToken";

    public ValueTask<string?> GetAsync()
        => jsRuntime.InvokeAsync<string?>("gitlabMilestoneExtensionsTokenStore.get", TokenStorageKey);

    public ValueTask SetAsync(string token)
        => jsRuntime.InvokeVoidAsync("gitlabMilestoneExtensionsTokenStore.set", TokenStorageKey, token);

    public ValueTask RemoveAsync()
        => jsRuntime.InvokeVoidAsync("gitlabMilestoneExtensionsTokenStore.remove", TokenStorageKey);
}
