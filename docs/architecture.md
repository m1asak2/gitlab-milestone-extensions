# Architecture

## Overview
このシステムは GitLab CE / GitLab REST API から取得した Group / Project / Milestone / Issue をバックエンドで集約し、Blazor WebAssembly の read-only ダッシュボードとして表示する。

主なユースケースは次の 3 つ。

- milestone 単位のサマリー確認
- issue 一覧の確認と GitLab 画面への遷移
- gantt 風一覧による担当者別進捗確認

## Runtime Components

| Component | Responsibility |
| --- | --- |
| `gitlab-milestone-extensions.AppHost` | ローカル開発時の起動構成と依存関係管理 |
| `gitlab-milestone-extensions.ApiService` | GitLab API 呼び出し、キャッシュ、画面用 DTO 生成、Minimal API 公開 |
| `gitlab-milestone-extensions.Web` | Blazor WebAssembly UI、セレクタ状態管理、API 呼び出し、画面描画 |
| `gitlab-milestone-extensions.ServiceDefaults` | OpenTelemetry / Health Check / Service Discovery などの共通設定 |
| `gitlab-milestone-extensions.Tests` | xUnit による回帰テストと疎通確認 |

## Deployment Shape

- 開発時は `gitlab-milestone-extensions.AppHost` から `ApiService` と `Web` を起動する
- Docker 配備対象は `gitlab-milestone-extensions.ApiService` と `gitlab-milestone-extensions.Web` のみとする
- `Web` は nginx 上で静的配信し、`/api` を `ApiService` へリバースプロキシする
- `Web` からの API 呼び出しは相対パス `/api/*` を使用する

## Data Flow

1. `Web` が `ApiService` の `/api/selection/options` でセレクタ候補を取得する
2. 利用者が Group / Member / Project / Milestone を選択する
3. `Web` が `/api/dashboard`、`/api/issues`、`/api/gantt` を `milestoneId` 指定で呼び出す
4. `ApiService` が `GitLabDashboardDataService` を介して画面用 DTO を生成する
5. `GitLabDashboardDataService` は `CachedGitLabDataSnapshotService` から GitLab スナップショットを取得する
6. `CachedGitLabDataSnapshotService` は `GitLabApiClient` を使って GitLab REST API を並列取得し、5 分キャッシュへ保存する
7. `Web` は取得した DTO をそのまま描画し、Issues の state フィルタや Dashboard の member 集計のような画面内ロジックのみを実施する

## Backend Design

### API Layer

- Minimal API で `/api` 配下の read-only endpoint を公開する
- エンドポイントはルーティング、入力検証、HTTP ステータス制御に集中する
- 例外は `ProblemDetails` ベースで処理する

### Application Layer

- `GitLabDashboardDataService` が milestone 単位のサマリー、issue 一覧、gantt 一覧を生成する
- 画面リンク用に `MilestoneWebUrl`、`IssueUrl`、`ProjectUrl` を DTO に含める
- issue の工数は `time_stats` を利用し、Human readable 表記があれば優先する

### Infrastructure Layer

- `GitLabApiClient` が GitLab REST API を呼び出す
- projects / milestones / issues は `per_page=100` でページング取得する
- project milestone URL は `iid` を使って `/-/milestones/{iid}` を組み立てる
- group milestone URL は group `web_url` を使って snapshot 構築時に補完する
- snapshot は `IMemoryCache` に 5 分保存する

## Frontend Design

### Layout

- `MainLayout.razor` が Group / Member / Project / Milestone の selector を持つ
- selector 変更時は候補一覧を再取得し、不整合な下位選択は自動で解除する

### Dashboard Page

- `/` と `/dashboard` に割り当てる
- milestone 未選択時は案内のみ表示する
- タブは `Dashboard` / `Issues` / `Gantt` の 3 つ
- タブ選択状態はページ内で保持し、selector 変更時も active tab を維持する
- page title は milestone 名に追従し、画面上部の milestone 名は GitLab milestone へのリンクになる
- Issues タブはネイティブ `select` による state フィルタを持ち、一覧の `Title` / `Project` から GitLab に遷移できる
- Gantt タブは `Assignee` 表記を使用する

## Testing Strategy

- `GitLabApiClientTests`
  - project milestone URL に `iid` が使われること
  - group milestone URL が snapshot で補完されること
- `GitLabDashboardDataServiceTests`
  - dashboard / issues DTO に GitLab へのリンク情報が含まれること
- `DummyDashboardDataServiceTests`
  - selector 候補の基本整合性
- `WebTests`
  - AppHost 起動と Web フロントの疎通確認

## Design Rationale

- GitLab 呼び出しは backend のみで行い、Web 側は `X-GitLab-Private-Token` を API へ転送する
- 複数 project の issue / milestone をまとめて扱うため、API 側で集約する
- selector と詳細表示を分け、`milestoneId` を中心に UI を単純化する
- GitLab 画面へのリンクを API 側で生成し、Web 側では URL の知識を持たない構成にしている
