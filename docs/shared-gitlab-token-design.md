# GitLab共通トークン移行 詳細設計（Issue #5）

> 状態: 実装前の合意仕様。既存のユーザートークン方式は移行完了まで有効。

## 目的
ユーザーがGitLabトークンを入力する方式を廃止し、アプリ全体で単一のGitLab読み取りトークンを使用する。秘密値はAPIサーバーに限定し、Blazor WebAssemblyや静的配信ファイルには絶対に含めない。

## 構成
```text
Browser (Blazor WASM, no token)
  -> /api/* (nginx reverse proxy)
  -> ASP.NET Core ApiService (GitLab__PrivateToken)
  -> GitLab REST API (PRIVATE-TOKEN)
```

- API構成: `GitLab__BaseUrl` と `GitLab__PrivateToken` を環境変数で指定。`GitLabOptions` にPrivateTokenを追加し、DI経由でGitLabApiClientに注入。
- Web: トークン入力UI、ローカルストレージ保存・復元、`MilestoneSelectionState.PrivateToken/HasPrivateToken`、`X-GitLab-Private-Token` ヘッダー送信と関連分岐を廃止。
- API: クライアントからのトークン転送を廃止し、すべてのGitLab呼び出しでサーバー設定値を使用。設定欠落時は明示的に失敗し、秘密値を例外・ログ・HTTP応答に含めない。
- キャッシュ: 共通トークンへの移行でキャッシュが他ユーザーへ漏れないか、アプリ公開範囲・認可境界をレビュー。必要ならキャッシュキーとスコープを調整。

## 配備
### Docker Compose
```yaml
services:
  api:
    environment:
      GitLab__BaseUrl: ${GITLAB_BASE_URL}
      GitLab__PrivateToken: ${GITLAB_PRIVATE_TOKEN}
```
`GITLAB_PRIVATE_TOKEN` の実値をリポジトリ、Dockerfile、WASM設定、ログに書かない。

### OpenShift
Secretを作成し、API Deploymentの `env[].valueFrom.secretKeyRef` から `GitLab__PrivateToken` を注入。Secret自体の実値はGit管理しない。

## セキュリティ
- 共通トークンは閲覧専用の専用アカウント/最小権限で発行し、ローテーション手順を整備。
- アプリの閲覧者は共通トークンが閲覧できるGitLabデータを参照可能になる。アプリの公開範囲、認証、ネットワーク制御を運用上必ず確認。
- CORSの全許可等、共通トークン化で露出が増す設定を見直す。
- 開発時のサンプル値はダミーのみ使用し、実トークンをコミットしない。

## 受入テスト
1. ブラウザのHTML/JS/Network/LocalStorageにGitLabトークンが現れない。
2. トークン入力UIがなく、ログイン操作なしで許可された利用者がデータを表示できる。
3. APIはサーバー設定のトークンをGitLabへ送信し、ブラウザ指定トークンに依存しない。
4. トークン欠落/不正、GitLab 401/403を安全に扱う。
5. Docker Compose/OpenShift Secret設定、.NET 10 restore/build/test、既存の選択UI・キャッシュを回帰検証。

## 実装運用
Issue #5 / 独立Draft PRで管理。Issue #4のPRに依存させない。マージはレビューとユーザー承認後。
