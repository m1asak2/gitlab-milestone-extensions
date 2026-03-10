## Phase 1 — GitLab 接続確認

このフェーズでは **GitLab API と通信できることだけ確認**します。

### Step 1 — GitLab Token を設定

```
appsettings.Development.json
{
  "GitLab": {
    "BaseUrl": "http://gitlab.local",
    "PrivateToken": "YOUR_PRIVATE",
    "GroupId": 4
  }
}
```

------

### Step 2 — 設定クラス作成

```
public sealed class GitLabOptions
{
    public string BaseUrl { get; set; } = "";
    public string PrivateToken { get; set; } = "";
    public int GroupId { get; set; }
}
```

Program.cs

```
builder.Services.Configure<GitLabOptions>(
    builder.Configuration.GetSection("GitLab"));
```

------

### Step 3 — GitLabApiClient 作成

```
public class GitLabApiClient
{
    private readonly HttpClient _httpClient;

    public GitLabApiClient(HttpClient httpClient, IOptions<GitLabOptions> options)
    {
        var opt = options.Value;

        httpClient.BaseAddress = new Uri(opt.BaseUrl + "/api/v4/");
        httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", opt.PrivateToken);

        _httpClient = httpClient;
    }

    public async Task<T?> GetAsync<T>(string url)
    {
        return await _httpClient.GetFromJsonAsync<T>(url);
    }
}
```

DI登録

```
builder.Services.AddHttpClient<GitLabApiClient>();
```

------

### Step 4 — 接続テストAPI

```
app.MapGet("/api/gitlab/test", async (GitLabApiClient client) =>
{
    var groups = await client.GetAsync<object>("groups");
    return Results.Ok(groups);
});
```

動作確認

```
GET /api/gitlab/test
```

ここで **groups JSON が返れば成功**。

------

# Phase 2 — GitLab データ取得

接続確認後に以下を実装します。

### Step 5 — Groups API

GitLab

```
GET /groups
```

------

### Step 6 — Projects API

```
GET /groups/{groupId}/projects
```

------

### Step 7 — Milestones API

```
GET /projects/{projectId}/milestones
```

------

### Step 8 — Issues API

```
GET /projects/{projectId}/issues
```