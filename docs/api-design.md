# API Design

## Base Path
`/api`

## Endpoints

### GET `/api/selection/options`
selector 用の候補一覧を返す。

#### Query
- `groupId`: optional
- `memberId`: optional
- `projectId`: optional
- `milestoneId`: optional

#### Response

```json
{
  "groups": [
    { "groupId": 4, "groupName": "Platform" }
  ],
  "members": [
    { "memberId": 1, "memberName": "Alice" }
  ],
  "projects": [
    { "projectId": 11, "projectName": "Web" }
  ],
  "milestones": [
    {
      "milestoneId": 201,
      "milestoneTitle": "Sprint 1",
      "projectId": 11,
      "projectName": "Web",
      "startDate": "2026-03-01",
      "dueDate": "2026-03-31"
    }
  ]
}
```

### GET `/api/dashboard`
選択中 milestone のサマリーを返す。

#### Query
- `milestoneId`: required

#### Response

```json
{
  "milestoneId": 201,
  "milestoneTitle": "Sprint 1",
  "milestoneWebUrl": "https://gitlab.example.local/team/web/-/milestones/42",
  "totalIssues": 12,
  "openIssues": 7,
  "closedIssues": 5,
  "overdueIssues": 2,
  "startDate": "2026-03-01",
  "dueDate": "2026-03-31",
  "estimateSeconds": 28800,
  "actualSeconds": 14400
}
```

#### Status
- `400`: `milestoneId` 未指定
- `404`: milestone に紐づく issue が存在しない

### GET `/api/issues`
選択中 milestone に属する issue 一覧を返す。

#### Query
- `milestoneId`: optional

#### Response

```json
[
  {
    "id": 101,
    "title": "Build dashboard cards",
    "issueUrl": "https://gitlab.example.local/team/web/-/issues/101",
    "projectName": "Web",
    "projectId": 11,
    "projectUrl": "https://gitlab.example.local/team/web",
    "milestoneId": 201,
    "milestone": "Sprint 1",
    "assigneeId": 1,
    "assignee": "Alice",
    "state": "opened",
    "dueDate": "2026-03-15",
    "timeEstimateSeconds": 3600,
    "totalTimeSpentSeconds": 1800,
    "humanTimeEstimate": "1h",
    "humanTotalTimeSpent": "30m"
  }
]
```

#### Notes
- `milestoneId` 未指定時は空配列を返す
- state フィルタは API ではなく Web の Issues タブ内 `select` で適用する

### GET `/api/gantt`
選択中 milestone の gantt 風表示データを返す。

#### Query
- `milestoneId`: optional

#### Response

```json
[
  {
    "id": 101,
    "title": "Build dashboard cards",
    "viewMode": "milestone",
    "assignee": "Alice",
    "startDate": "2026-03-01",
    "endDate": "2026-03-15",
    "progress": 0,
    "milestoneId": 201,
    "milestoneTitle": "Sprint 1",
    "timeEstimateSeconds": 3600,
    "totalTimeSpentSeconds": 1800
  }
]
```

#### Notes
- `milestoneId` 未指定時は空配列を返す
- `progress` は issue state が `closed` の場合のみ `100`

### GET `/api/gitlab/test`
GitLab 接続確認用。`groups/{groupId}` の取得結果を返す。

### GET `/api/gitlab/projects`
GitLab から取得した project 一覧を返す。

### GET `/api/gitlab/milestones`
GitLab から取得した project milestone 一覧を返す。

### GET `/api/gitlab/issues`
GitLab から取得した project issue 一覧を返す。

## Backend Mapping Rules

- project milestone URL: `{projectWebUrl}/-/milestones/{milestoneIid}`
- group milestone URL: `{groupWebUrl}/-/milestones/{milestoneId}`
- issue URL: GitLab issue API の `web_url` をそのまま利用
- project URL: GitLab project API の `web_url` をそのまま利用

## Error Handling

- 予期しないエラーは `ProblemDetails` で返す
- GitLab API 呼び出し失敗時は `ApiService` が例外を伝播し、HTTP 500 相当になる
