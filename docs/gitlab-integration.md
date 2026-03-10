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

---

# Phase 3: ダッシュボード用データへの変換

## 目的

このフェーズでは、GitLab API から取得した生データを、既存のダッシュボード API で使う形式へ変換する。

この段階では以下を目標とする。

- GitLab DTO をそのまま画面に返さない
- ダッシュボード専用 DTO を作成する
- 既存の `/api/issues` を実データに差し替える
- 既存の `/api/milestones` を実データに差し替える
- `/api/summary` を issues から計算できるようにする
- `/api/gantt` は簡易版でよい

まだ pagination 対応や高度な集約は不要とする。

---

## 方針

- GitLab API のレスポンス DTO と、画面用 DTO を分ける
- API 層から返すデータはダッシュボード専用 DTO とする
- milestone はまず `project + milestone title` 単位で扱う
- group milestone はまだ対象外
- project milestone のみ対象とする

---

## Step 9: DashboardIssue DTO を作成

画面表示用の issue DTO を作成する。

例:

```csharp
public sealed class DashboardIssue
{
    public string ProjectName { get; set; } = "";
    public string MilestoneTitle { get; set; } = "";
    public string Title { get; set; } = "";
    public string State { get; set; } = "";
    public string AssigneeName { get; set; } = "";
    public DateTime? DueDate { get; set; }
}

## Step 10: DashboardMilestone DTO を作成

画面表示用の milestone DTO を作成する。

例:

```csharp
public sealed class DashboardMilestone
{
    public string ProjectName { get; set; } = "";
    public string Title { get; set; } = "";
    public string State { get; set; } = "";
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public int OpenIssues { get; set; }
    public int ClosedIssues { get; set; }
}
```

初期は `project + title` ごとに1件として扱う。

------

## Step 11: GitLab Issue を DashboardIssue に変換

GitLab から取得した issue 一覧を DashboardIssue に変換する。

変換時に最低限含める項目:

- ProjectName
- MilestoneTitle
- Title
- State
- AssigneeName
- DueDate

assignee や milestone が null の場合は空文字でよい。

例:

```csharp
var dashboardIssues = issues.Select(x => new DashboardIssue
{
    ProjectName = x.ProjectName,
    MilestoneTitle = x.Milestone?.Title ?? "",
    Title = x.Title,
    State = x.State,
    AssigneeName = x.Assignee?.Name ?? "",
    DueDate = x.DueDate
}).ToList();
```

------

## Step 12: milestone ごとの issue 件数を集計する

DashboardMilestone を作成する際に、対応する issue を集計する。

集計単位は以下とする。

- ProjectName
- MilestoneTitle

集計値:

- OpenIssues
- ClosedIssues

例:

```csharp
var openIssues = relatedIssues.Count(x => x.State == "opened");
var closedIssues = relatedIssues.Count(x => x.State == "closed");
```

milestone title が同じでも project が違う場合は別 milestone として扱う。

------

## Step 13: `/api/issues` を実データに差し替える

既存のダミーデータ endpoint を GitLab 実データベースの DashboardIssue 一覧へ差し替える。

対象:

```shell
GET /api/issues
```

返却型:

```csharp
List<DashboardIssue>
```

この段階ではフィルタやページングはまだ不要。

------

## Step 14: `/api/milestones` を実データに差し替える

既存のダミーデータ endpoint を GitLab 実データベースの DashboardMilestone 一覧へ差し替える。

対象:

```shell
GET /api/milestones
```

返却型:

```csharp
List<DashboardMilestone>
```

この段階では group milestone はまだ含めない。

------

## Step 15: `/api/summary` を issues から生成する

summary は issue 一覧から算出する。

最低限含める項目:

- OpenIssues
- ClosedIssues
- OverdueIssues
- TotalMilestones

overdue 判定は以下とする。

- state が opened
- dueDate が存在する
- dueDate が今日より前

例:

```csharp
var overdueIssues = issues.Count(x =>
    x.State == "opened" &&
    x.DueDate.HasValue &&
    x.DueDate.Value.Date < DateTime.Today);
```

completion rate を出す場合は以下でよい。

```csharp
var totalIssues = openIssues + closedIssues;
var completionRate = totalIssues == 0
    ? 0
    : (double)closedIssues / totalIssues * 100;
```

------

## Step 16: `/api/gantt` を簡易実装する

この段階では簡易的な gantt データでよい。

まずは issue 単位で返す。

含める情報の例:

- ProjectName
- MilestoneTitle
- Title
- DueDate

start date が無ければ null でよい。

複雑な階層化はまだ不要。

------

## 実装ルール

- Aspire AppHost は維持する
- ServiceDefaults は維持する
- Minimal API を維持する
- 既存の Web UI を壊さない
- まずは API JSON が正しく返ることを優先する
- pagination はまだ未対応でよい
- group milestone はまだ実装しない
- project milestone のみ対象とする

------

## このフェーズの完了条件

以下が成立すれば完了とする。

- `/api/issues` が GitLab 実データを返す
- `/api/milestones` が GitLab 実データを返す
- `/api/summary` が GitLab issue から算出される
- `/api/gantt` が簡易データを返す
- ダミーデータ依存が主要 endpoint から除去されている


---

# Phase 4: API品質改善（Pagination / Cache / Filter）

## 目的

このフェーズでは GitLab API を実運用レベルで利用できるように改善する。

GitLab REST API には以下の特性がある。

- デフォルトでページネーションされる
- 1リクエストの取得件数が制限されている
- 多数の API を呼び出すと遅くなる
- Rate Limit が存在する

そのため、以下を実装する。

- Pagination 対応
- GitLab API 呼び出し戦略の整理
- API レスポンスキャッシュ
- フィルタ機能

---

# Step 17: Pagination対応

GitLab API はデフォルトでページネーションされる。

デフォルト取得件数: 20

そのため、ページをループして全件取得する必要がある。

まず `per_page=100` を使用する。

例:
```
GET /projects/{projectId}/issues?per_page=100&page=1
```
---

## Pagination実装例

GitLabApiClient に以下のメソッドを追加する。

```csharp
public async Task<List<T>> GetPagedAsync<T>(string url)
{
    var results = new List<T>();
    var page = 1;

    while (true)
    {
        var response = await _httpClient.GetFromJsonAsync<List<T>>(
            $"{url}?per_page=100&page={page}");

        if (response == null || response.Count == 0)
            break;

        results.AddRange(response);

        if (response.Count < 100)
            break;

        page++;
    }

    return results;
}
```

------

# Step 18: Issue取得戦略

Issue を取得する方法は複数ある。

```
/projects/:id/issues
/groups/:id/issues
/search
```

今回のダッシュボードでは以下を採用する。

```
projects/{projectId}/issues
```

理由:

- milestone 情報が確実に取れる
- project name を紐付けやすい
- APIの挙動が安定している

------

# Step 19: Milestone取得戦略

Milestone は以下を対象とする。

```
projects/{projectId}/milestones
```

この段階では以下は対象外とする。

```
groups/{groupId}/milestones
```

理由:

- project milestone が一般的
- issue は project 単位
- group milestone は運用依存が大きい

------

# Step 20: API呼び出しフロー

最終的な GitLab API 呼び出し順序は以下とする。

```
groups/{groupId}/projects
        ↓
projects/{projectId}/milestones
        ↓
projects/{projectId}/issues
```

この結果を Dashboard DTO に変換する。

------

# Step 21: APIキャッシュ

GitLab API を毎回呼び出すと遅くなるため、簡易キャッシュを入れる。

ASP.NET Core の `IMemoryCache` を使用する。

キャッシュ対象:

```
projects
milestones
issues
```

キャッシュ時間:

```
5分
```

------

## キャッシュ実装例

```
public async Task<List<DashboardIssue>> GetIssuesAsync()
{
    return await _cache.GetOrCreateAsync("gitlab_issues", async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

        return await FetchIssuesFromGitLab();
    });
}
```

------

# Step 22: Filter対応

API に基本的なフィルタを追加する。

対象:

```
project
milestone
assignee
state
```

例:

```
GET /api/issues?project=api&state=opened
```

------

## Filter実装例

```
app.MapGet("/api/issues", (string? project, string? state) =>
{
    var query = issues.AsQueryable();

    if (!string.IsNullOrEmpty(project))
        query = query.Where(x => x.ProjectName == project);

    if (!string.IsNullOrEmpty(state))
        query = query.Where(x => x.State == state);

    return query.ToList();
});
```

------

# Step 23: Web UI Filter

Web UI に以下のフィルタを追加する。

```
Project
Milestone
Assignee
State
```

MudBlazor のコンポーネントを使用する。

例:

```
MudSelect
MudAutocomplete
MudChip
```

------

# このフェーズの完了条件

以下が成立すれば Phase4 完了とする。

- GitLab API が pagination 対応
- 全 issue が取得できる
- milestone が正しく集計される
- API レスポンスがキャッシュされる
- `/api/issues` にフィルタが追加される
- Web UI に基本フィルタが追加される