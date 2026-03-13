# GitLab Milestone Extensions 詳細設計仕様書

## 1. 文書情報

| 項目 | 内容 |
| --- | --- |
| 文書名 | GitLab Milestone Extensions 詳細設計仕様書 |
| 対象システム | GitLab Milestone Extensions |
| 作成基準日 | 2026-03-13 |
| 対象リポジトリ | `gitlab-milestone-extensions` |
| 記載方針 | 現行実装準拠 |

## 2. システム概要

本システムは、GitLab CE / GitLab API からグループ、プロジェクト、マイルストーン、Issue 情報を取得し、マイルストーン単位の進捗状況を可視化する参照専用ダッシュボードである。

利用者は Web 画面上で Group、Member、Project、Milestone を選択し、選択したマイルストーンに対する以下の情報を閲覧できる。

- ダッシュボードサマリー
- Issue 一覧
- Gantt 風一覧

本システムは更新系機能を持たず、GitLab への書き戻しやローカル DB 永続化は行わない。

## 3. 目的

- GitLab 標準 UI で横断把握しにくいマイルストーン進捗を一覧化する
- 複数プロジェクトにまたがる Issue 情報をバックエンドで集約する
- GitLab のアクセストークンをブラウザへ露出させずに可視化する
- フロントエンド側を単純な参照 UI に保ち、取得・集計責務を API 側に集約する

## 4. スコープ

### 4.1 対象

- GitLab グループ情報の取得
- GitLab プロジェクト一覧の取得
- GitLab プロジェクトマイルストーン一覧の取得
- GitLab グループマイルストーン一覧の取得
- GitLab プロジェクト Issue 一覧の取得
- Issue 情報を用いたマイルストーン単位の集計
- Blazor WebAssembly による参照 UI の提供

### 4.2 非対象

- Issue / Milestone の作成、更新、削除
- GitLab への POST / PUT / DELETE
- アプリケーション内データベース
- バックグラウンドジョブ
- Webhook 連携
- 認証認可の本格実装

## 5. 全体アーキテクチャ

### 5.1 構成要素

| プロジェクト | 役割 |
| --- | --- |
| `gitlab-milestone-extensions.AppHost` | .NET Aspire によるローカル実行構成管理 |
| `gitlab-milestone-extensions.ServiceDefaults` | OpenTelemetry、Service Discovery、Health Check 等の共通設定 |
| `gitlab-milestone-extensions.ApiService` | GitLab API 集約、キャッシュ、ダッシュボード用 DTO 提供 |
| `gitlab-milestone-extensions.Web` | Blazor WebAssembly による UI |
| `gitlab-milestone-extensions.Tests` | xUnit による疎通・単体テスト |

### 5.2 論理アーキテクチャ

1. `Web` が `ApiService` の REST API を呼び出す
2. `ApiService` は `IDashboardDataService` を介して画面用データを取得する
3. `GitLabDashboardDataService` は `IGitLabDataSnapshotService` から GitLab スナップショットを取得する
4. `CachedGitLabDataSnapshotService` は `GitLabApiClient` を用いて GitLab REST API を並列呼び出しする
5. 取得したプロジェクト、マイルストーン、Issue をメモリキャッシュに格納し、DTO へ変換して返却する

### 5.3 実行形態

- AppHost から `ApiService` と `Web` を起動する
- `Web` は `ApiBaseUrl` を用いて `ApiService` にアクセスする
- `ApiService` は `GitLab` 設定を用いて外部 GitLab サーバーへアクセスする

## 6. 技術スタック

| 分類 | 採用技術 |
| --- | --- |
| ランタイム | .NET 10 |
| API | ASP.NET Core Minimal API |
| UI | Blazor WebAssembly |
| UI コンポーネント | MudBlazor |
| 開発オーケストレーション | .NET Aspire AppHost |
| 共通基盤 | Aspire ServiceDefaults |
| API ドキュメント | OpenAPI, Scalar |
| キャッシュ | `IMemoryCache` |
| テスト | xUnit, Aspire Testing |

## 7. ディレクトリ設計

```text
gitlab-milestone-extensions/
├─ docs/
│  ├─ architecture.md
│  ├─ api-design.md
│  └─ design-specification-ja.md
├─ gitlab-milestone-extensions.AppHost/
├─ gitlab-milestone-extensions.ServiceDefaults/
├─ gitlab-milestone-extensions.ApiService/
│  ├─ Endpoints/
│  ├─ Models/
│  ├─ Options/
│  ├─ Services/
│  └─ Program.cs
├─ gitlab-milestone-extensions.Web/
│  ├─ Layout/
│  ├─ Models/
│  ├─ Pages/
│  ├─ Services/
│  └─ wwwroot/
└─ gitlab-milestone-extensions.Tests/
```

## 8. バックエンド設計

### 8.1 起動処理

`ApiService/Program.cs` で以下を実施する。

- ServiceDefaults の適用
- ProblemDetails 登録
- OpenAPI 登録
- MemoryCache 登録
- `GitLabOptions` の設定バインド
- `GitLabApiClient` 用 `HttpClient` 登録
- `IGitLabDataSnapshotService` に `CachedGitLabDataSnapshotService` を登録
- CORS 全許可ポリシー登録
- `IDashboardDataService` に `GitLabDashboardDataService` を登録
- Minimal API エンドポイントのマッピング

### 8.2 API 責務分離

| 区分 | 実装 | 責務 |
| --- | --- | --- |
| エンドポイント | `Endpoints/*.cs` | ルーティング、パラメータ受理、HTTP 応答 |
| アプリケーションサービス | `GitLabDashboardDataService` | 画面用データ整形・集計 |
| 外部 API 連携 | `GitLabApiClient` | GitLab REST API 呼び出し |
| キャッシュ | `CachedGitLabDataSnapshotService` | 集約データの 5 分キャッシュ |
| モデル | `Models/*.cs` | DTO、外部 API 取得後の内部表現 |

### 8.3 エンドポイント仕様

#### 8.3.1 GET `/api/selection/options`

セレクタ用の候補一覧を返却する。

入力クエリ:

- `groupId` : 任意
- `memberId` : 任意
- `projectId` : 任意
- `milestoneId` : 任意

返却内容:

- `Groups`
- `Members`
- `Projects`
- `Milestones`

仕様:

- 指定済み選択条件に応じて後続セレクタ候補を動的に絞り込む
- `groupId` が不正な場合は `Groups` のみ返し、他は空配列とする

#### 8.3.2 GET `/api/dashboard`

マイルストーン単位のサマリーを返却する。

入力クエリ:

- `milestoneId` : 必須

応答仕様:

- `milestoneId` 未指定時は `400 BadRequest`
- 対象マイルストーンに紐づく Issue が存在しない場合は `404 NotFound`
- 対象存在時は `MilestoneDashboardDto`

集計項目:

- `MilestoneWebUrl`
- `TotalIssues`
- `OpenIssues`
- `ClosedIssues`
- `OverdueIssues`
- `StartDate`
- `DueDate`
- `EstimateSeconds`
- `ActualSeconds`

#### 8.3.3 GET `/api/issues`

マイルストーンに属する Issue 一覧を返却する。

入力クエリ:

- `milestoneId` : 任意

応答仕様:

- `milestoneId` 未指定時は空配列を返却
- 指定時は `DashboardIssue[]`

返却項目補足:

- `IssueUrl`
- `ProjectUrl`
- 画面表示 ID には GitLab issue の `iid` を使用する

#### 8.3.4 GET `/api/gantt`

Gantt 表示用の Issue 一覧を返却する。

入力クエリ:

- `milestoneId` : 任意

応答仕様:

- `milestoneId` 未指定時は空配列を返却
- 指定時は `GanttItemDto[]`

#### 8.3.5 GET `/api/gitlab/test`

GitLab 接続確認用 API。`groups` API の結果をそのまま返す。

#### 8.3.6 GET `/api/gitlab/projects`

取得済みプロジェクト一覧を返却する。

#### 8.3.7 GET `/api/gitlab/milestones`

プロジェクトマイルストーン一覧を返却する。

#### 8.3.8 GET `/api/gitlab/issues`

プロジェクト Issue 一覧を返却する。

### 8.4 サービス設計

#### 8.4.1 `GitLabApiClient`

役割:

- GitLab REST API への GET 実行
- ページング付き取得
- GitLab レスポンスから内部 DTO へのマッピング

主要仕様:

- `BaseUrl` は `GitLabOptions.BaseUrl` を元に `/api/v4/` を付与して設定する
- 認証は `PRIVATE-TOKEN` ヘッダを使用する
- `per_page=100` でページング取得する
- `X-Next-Page` ヘッダを見て次ページを追跡する
- プロジェクト一覧は次の 2 経路を統合する
  - `groups/{groupId}/projects?include_subgroups=true`
  - `projects?membership=true&simple=true`
- 結果は `Id` で重複排除する
- project / group / issue の `web_url` を内部 DTO へ引き継ぐ
- project milestone URL は `project.web_url` と milestone の `iid` から構築する

呼び出し対象 API:

- `GET groups/{groupId}`
- `GET groups/{groupId}/projects`
- `GET projects?membership=true&simple=true`
- `GET projects/{projectId}/milestones`
- `GET groups/{groupId}/milestones`
- `GET projects/{projectId}/issues`

Issue 取得仕様:

- 担当者は `assignee` を優先し、無い場合 `assignees` の先頭を使用する
- 工数は `time_stats` を使用する
- 画面用 ID には `iid` を引き継ぐ

Milestone 取得仕様:

- project milestone は GitLab milestone API の `iid` を保持する
- group milestone URL は snapshot 構築時に group `web_url` から補完する

#### 8.4.2 `CachedGitLabDataSnapshotService`

役割:

- GitLab から取得した全体スナップショットをメモリキャッシュする

仕様:

- キャッシュキー: `gitlab:data:snapshot:v1`
- キャッシュ有効期間: 5 分
- 多重取得防止のため `SemaphoreSlim` による排他制御を行う
- 初回取得時は以下を並列実行する
  - グループ取得
  - プロジェクト一覧取得
  - プロジェクトマイルストーン一覧取得
  - グループマイルストーン一覧取得
  - プロジェクト Issue 一覧取得
- マイルストーンは `Scope + MilestoneId + ProjectId + ProjectName` 単位で重複排除する
- group milestone には `group.web_url/-/milestones/{milestoneId}` を補完する

スナップショット構造:

- `Groups`
- `Projects`
- `Milestones`
- `Issues`
- `LoadedAtUtc`

#### 8.4.3 `GitLabDashboardDataService`

役割:

- スナップショットから画面用 DTO を生成する

主要ロジック:

- `GetSelectionOptionsAsync`
  - Group, Member, Project, Milestone の候補を現在選択に応じて絞り込む
- `GetDashboardAsync`
  - `milestoneId` 単位で Issue を集計してダッシュボード情報を作成する
- `GetIssuesAsync`
  - `milestoneId` で Issue を抽出する
- `GetGanttAsync`
  - `milestoneId` で Issue を抽出し、開始日・終了日・進捗率を決定する

日付決定ルール:

- Gantt の終了日
  - Issue の `DueDate`
  - 無ければマイルストーンの `DueDate`
  - それも無ければ当日
- Gantt の開始日
  - マイルストーンの `StartDate`
  - 無ければ終了日の 7 日前

進捗率ルール:

- Issue 状態が `closed` の場合 100
- それ以外は 0

工数文字列ルール:

- `HumanTimeEstimate` / `HumanTotalTimeSpent` が存在すればそれを優先
- 無い場合は秒から `d h` または `h m` または `m` に変換

### 8.5 データモデル設計

#### 8.5.1 GitLab 取得モデル

| モデル | 主な項目 |
| --- | --- |
| `GitLabGroupDto` | `GroupId`, `GroupName`, `WebUrl` |
| `GitLabProjectDto` | `ProjectId`, `ProjectName`, `WebUrl` |
| `GitLabMilestoneDto` | `ProjectId`, `ProjectName`, `MilestoneId`, `MilestoneIid`, `Title`, `Scope`, `State`, `StartDate`, `DueDate`, `WebUrl` |
| `GitLabIssueDto` | `ProjectId`, `ProjectName`, `ProjectWebUrl`, `IssueId`, `Iid`, `Title`, `WebUrl`, `State`, `MilestoneId`, `MilestoneTitle`, `AssigneeId`, `AssigneeName`, `DueDate`, 工数系項目 |

#### 8.5.2 API 返却モデル

| モデル | 用途 |
| --- | --- |
| `SelectionOptionsDto` | セレクタ候補一覧 |
| `MilestoneDashboardDto` | ダッシュボードサマリー。`MilestoneWebUrl` を含む |
| `DashboardIssue` | Issue 表示行。`IssueUrl` と `ProjectUrl` を含む |
| `GanttItemDto` | Gantt 表示行。担当者表示は `Assignee` |

`MilestoneDashboardDto` の算出式:

- `TotalIssues = 対象 Issue 件数`
- `OpenIssues = state == opened`
- `ClosedIssues = state == closed`
- `OverdueIssues = opened かつ DueDate < 今日`
- `EstimateSeconds = Σ TimeEstimateSeconds`
- `ActualSeconds = Σ TotalTimeSpentSeconds`

## 9. フロントエンド設計

### 9.1 起動処理

`Web/Program.cs` で以下を実施する。

- ルートコンポーネント登録
- MudBlazor サービス登録
- `ApiBaseUrl` を元に `HttpClient.BaseAddress` を設定
- `DashboardApiClient` 登録
- `MilestoneSelectionState` 登録

### 9.2 画面構成

画面は 1 ページ構成で、主に以下の領域を持つ。

1. AppBar
2. セレクタエリア
3. タブ表示エリア

### 9.3 レイアウト仕様

`MainLayout.razor` が共通レイアウトを提供する。

表示要素:

- タイトル `GitLab Milestone Dashboard`
- `Refresh` ボタン
- Group / Member / Project / Milestone の 4 つのセレクタ
- セレクタ取得失敗時のエラーメッセージ
- 各ページ本文

セレクタの挙動:

- いずれかの選択変更時に `SelectionState` を更新する
- 更新後に `/api/selection/options` を再取得する
- 現在選択値が新しい候補一覧に存在しない場合は自動で `null` に戻す
- 選択補正が発生した場合は再度 `/api/selection/options` を取得して整合性を取る

### 9.4 状態管理

`MilestoneSelectionState` はスコープ付きサービスとして動作する。

保持項目:

- `SelectedGroupId`
- `SelectedMemberId`
- `SelectedProjectId`
- `SelectedMilestoneId`

通知方式:

- 値変更時に `Changed` イベントを発火する
- `Dashboard` ページでは active tab index を保持し、selector 変更による再読み込み後もタブ位置を維持する

### 9.5 Dashboard 画面仕様

`Pages/Dashboard.razor` は `/` および `/dashboard` に割り当てられる。

表示条件:

- 読み込み中: 円形プログレス表示
- エラー時: エラーアラート表示
- マイルストーン未選択時: 情報メッセージ表示
- マイルストーン選択時: 3 タブを表示

#### 9.5.1 Dashboard タブ

表示項目:

- 画面上部に milestone 名を表示する
- milestone 名に対応する GitLab milestone URL が存在する場合はリンク表示する
- `Total Issues`
- `Open Issues`
- `Closed Issues`
- `Overdue Issues`
- `Start`
- `Due`
- `Estimate`
- `Actual`
- `Estimate / Actual by Member`

#### 9.5.2 Issues タブ

表示列:

- `Id`
- `Title`
- `Project`
- `Assignee`
- `State`
- `Estimate`
- `Actual`

表示仕様:

- `Title` は対応する GitLab issue URL へのリンク
- `Project` は対応する GitLab project URL へのリンク
- タブ内上部に native `select` による `State` フィルタを持つ
- `State` フィルタは API 再呼び出しではなく画面内絞り込みで実現する

#### 9.5.3 Gantt タブ

表示列:

- `Title`
- `Mode`
- `Assignee`
- `Start`
- `End`
- `Progress`

注記:

- 現状の Gantt は厳密なチャート描画ではなくテーブル形式の Gantt 風表示である

### 9.6 フロントエンド API クライアント

`DashboardApiClient` は以下 API を呼び出す。

- `GET api/selection/options`
- `GET api/dashboard?milestoneId={id}`
- `GET api/issues?milestoneId={id}`
- `GET api/gantt?milestoneId={id}`

例外方針:

- `GetFromJsonAsync` が `null` を返した場合は `InvalidOperationException`
- `GetDashboardAsync` は `404` 時に `null` となり得る
- GitLab 画面用リンク URL は API の返却 DTO をそのまま利用し、Web 側で URL 組み立ては行わない

## 10. 画面遷移・処理フロー

### 10.1 初期表示フロー

1. Web 起動
2. `MainLayout` 初期化
3. `/api/selection/options` 呼び出し
4. Group / Member / Project / Milestone 候補表示
5. `Dashboard` ページ初期化
6. マイルストーン未選択の場合、コンテンツは案内表示のみ

### 10.2 マイルストーン選択フロー

1. 利用者が Milestone を選択
2. `MilestoneSelectionState.SetMilestone` 実行
3. `MainLayout` が選択候補を再取得
4. `Dashboard` ページが `Changed` イベントを受信
5. 以下 3 API を順次呼び出す
   - `/api/dashboard`
   - `/api/issues`
   - `/api/gantt`
6. 取得結果を各タブに反映

### 10.3 API 側データ取得フロー

1. `DashboardEndpoints` が `IDashboardDataService` を呼び出す
2. `GitLabDashboardDataService` が `IGitLabDataSnapshotService` を呼び出す
3. キャッシュヒット時はメモリ上のスナップショットを返す
4. キャッシュミス時は GitLab API から再取得する
5. DTO へ整形してレスポンスを返却する

## 11. 設定設計

### 11.1 ApiService 設定

`appsettings.json` / `appsettings.Development.json` に `GitLab` セクションを持つ。

| 設定キー | 必須 | 内容 |
| --- | --- | --- |
| `GitLab:BaseUrl` | 必須 | GitLab ベース URL |
| `GitLab:PrivateToken` | 必須 | GitLab アクセストークン |
| `GitLab:GroupId` | 必須 | 対象グループ ID |

注意事項:

- `appsettings.json` には `GroupId` が未定義のため、実運用では環境別設定またはシークレット管理で補完すること
- アクセストークンは本来ソース管理へ含めず、User Secrets や環境変数で管理することが望ましい

### 11.2 Web 設定

`wwwroot/appsettings.json`

| 設定キー | 必須 | 内容 |
| --- | --- | --- |
| `ApiBaseUrl` | 必須 | API サービスのベース URL |

## 12. 非機能要件

### 12.1 性能

- GitLab 全件取得は高コストなため 5 分キャッシュを必須とする
- 初回取得では外部 API 呼び出しを並列化して待機時間を抑制する
- フロントエンドはサーバー集約結果をそのまま描画し、ブラウザ側集計を最小化する

### 12.2 可観測性

- ServiceDefaults により OpenTelemetry のトレースとメトリクスを有効化する
- HTTP クライアント、ASP.NET Core、ランタイムメトリクスを収集対象とする
- `OTEL_EXPORTER_OTLP_ENDPOINT` 設定時は OTLP エクスポート可能

### 12.3 可用性

- 開発環境では `/health` と `/alive` を公開する
- AppHost では API の `/health` を利用したヘルスチェックを設定する

### 12.4 セキュリティ

- GitLab トークンは API サービス側のみで使用する
- WebAssembly クライアントは GitLab へ直接アクセスしない
- CORS は現状 `AllowAnyOrigin` のため、公開運用時は制限が必要

## 13. エラー処理方針

- API サービスは `AddProblemDetails` と `UseExceptionHandler` を利用する
- GitLab API 失敗時は例外を再送出し、最終的にサーバーエラー応答となる
- Web 側は API 失敗時に例外メッセージを画面アラート表示する
- `dashboard` はデータ未存在時に `404`、`issues` と `gantt` は空配列で返す

## 14. テスト設計

### 14.1 単体テスト

`DummyDashboardDataServiceTests`

確認内容:

- グループマイルストーンが候補に含まれること
- プロジェクトマイルストーンが候補に含まれること
- グループ外プロジェクトのマイルストーンが候補に含まれること

`GitLabApiClientTests`

確認内容:

- project milestone URL が `id` ではなく `iid` を用いて構築されること
- group milestone URL が snapshot で補完されること

`GitLabDashboardDataServiceTests`

確認内容:

- `MilestoneDashboardDto` に `MilestoneWebUrl` が反映されること
- `DashboardIssue` に `IssueUrl` / `ProjectUrl` が反映されること

### 14.2 結合テスト

`WebTests`

確認内容:

- AppHost を起動できること
- `webfrontend` がヘルシーになること
- `/` に対して `200 OK` が返ること

## 15. 既知の制約と今後の拡張候補

### 15.1 既知の制約

- Group セレクタは現状 1 グループ前提であり、複数グループ切替には未対応
- Gantt はテーブル表示であり、ドラッグ可能なチャートではない
- API 呼び出しの一部はプロジェクト数増加に伴いレスポンス時間が伸びる可能性がある
- 認証、認可、監査ログは未実装

### 15.2 拡張候補

- マイルストーン一覧 API と専用画面の追加
- グループ横断対応
- フィルタ条件の URL 永続化
- 実際のガントチャートコンポーネント導入
- データ更新日時の UI 表示
- キャッシュ失効ポリシーの高度化

## 16. 運用上の注意

- 開発用設定に実トークンを置かないこと
- GitLab API レートや対象プロジェクト数に応じてキャッシュ時間を調整すること
- 本番公開時は CORS 制限、設定秘匿、OpenAPI 公開範囲の見直しを行うこと

## 17. 参照ファイル

- `gitlab-milestone-extensions.ApiService/Program.cs`
- `gitlab-milestone-extensions.ApiService/Endpoints/DashboardEndpoints.cs`
- `gitlab-milestone-extensions.ApiService/Services/GitLabApiClient.cs`
- `gitlab-milestone-extensions.ApiService/Services/CachedGitLabDataSnapshotService.cs`
- `gitlab-milestone-extensions.ApiService/Services/GitLabDashboardDataService.cs`
- `gitlab-milestone-extensions.Web/Layout/MainLayout.razor`
- `gitlab-milestone-extensions.Web/Pages/Dashboard.razor`
- `gitlab-milestone-extensions.Web/Services/DashboardApiClient.cs`
- `gitlab-milestone-extensions.Web/Services/MilestoneSelectionState.cs`
