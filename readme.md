# GitLab Milestone Dashboard

GitLab CE のマイルストーン、Issue、担当者情報を可視化するための表示専用ダッシュボードです。

## Purpose
GitLab 標準画面だけでは把握しづらい以下を見やすく表示します。

- グループ別の進捗
- プロジェクト別の進捗
- 担当者別の進捗
- マイルストーン一覧
- Issue 一覧
- サマリー指標
- ガント風タイムライン表示

## Non-Goals
このアプリは表示専用です。

- GitLab への更新は行いません
- Milestone / Issue の編集は行いません
- GitLab へ書き戻しません

## Architecture
- Backend: ASP.NET Core Minimal API
- Frontend: Blazor WebAssembly
- UI: MudBlazor
- Data Source: GitLab REST API
- Cache: IMemoryCache

## Solution Structure
- `Domain`: ドメインモデル
- `Application`: DTO、集計、クエリ
- `Infrastructure`: GitLab API 通信、キャッシュ、設定
- `Api`: Minimal API エンドポイント
- `Web`: Blazor WASM + MudBlazor UI

## Main Screens
1. Dashboard
2. Gantt
3. Issues
4. Milestones

## API Overview
- `GET /api/summary`
- `GET /api/gantt`
- `GET /api/issues`
- `GET /api/milestones`
- `GET /api/master/groups`
- `GET /api/master/projects`
- `GET /api/master/assignees`

## GitLab Configuration
GitLab の Personal Access Token または Access Token を使用して、バックエンドから GitLab REST API を呼び出します。

### appsettings.Development.json example
```json
{
  "GitLab": {
    "BaseUrl": "http://gitlab.local",
    "PrivateToken": "YOUR_TOKEN",
    "GroupIds": [ 1 ],
    "ProjectIds": [],
    "CacheMinutes": 5
  }
}