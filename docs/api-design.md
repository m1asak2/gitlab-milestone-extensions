# API Design

## Base Path
`/api`

## Endpoints

### GET /api/summary
ダッシュボード用サマリーを返す。

### GET /api/gantt
ガント表示用データを返す。

#### Query
- `viewMode`: `group | project | assignee`
- `groupId`: optional
- `projectId`: optional
- `milestone`: optional
- `assignee`: optional
- `from`: optional
- `to`: optional

### GET /api/issues
Issue テーブル用データを返す。

#### Query
- `groupId`: optional
- `projectId`: optional
- `milestone`: optional
- `assignee`: optional
- `state`: optional
- `page`: optional
- `pageSize`: optional

### GET /api/milestones
Milestone テーブル用データを返す。

### GET /api/master/groups
フィルタ用グループ一覧を返す。

### GET /api/master/projects
フィルタ用プロジェクト一覧を返す。

### GET /api/master/assignees
フィルタ用担当者一覧を返す。

## Error Format
Unexpected errors return RFC7807 ProblemDetails.