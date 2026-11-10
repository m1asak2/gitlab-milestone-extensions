# GitLab共通トークン移行 詳細設計（Issue #5）

> 状態: 実装済み。共通トークンはAPIサーバー設定のみを使用し、ブラウザトークン方式は廃止。

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

## 設定・移行手順
1. APIプロセスだけに `GitLab__BaseUrl` と `GitLab__PrivateToken` を設定する。Web/WASMの設定やビルド引数には入れない。未設定はAPIデータ要求に503、GitLab認証失敗は秘密値なしの502を返す。
2. Composeは `docker/compose/.env.example` を参考に、Git管理しない `.env` に設定する。設定はapiサービスだけへ渡す。`docker compose config` の展開済み出力も秘密値を含むため保存・共有しない。
3. OpenShiftは既存api Deploymentに `openshift/api-gitlab-settings.patch.yaml` を適用する。`gitlab-reader` Secretを安全な値入力手段で別途作成し、`base-url` / `private-token` キーを用意する。Secret値をマニフェストやCIログへ出さない。
4. 利用者ごとのGitLab認可は引き継がれない。配備前にIngress/Route側で組織の認証・ネットワーク制限を設定し、この共通アカウントの閲覧範囲全体を公開してよい利用者に限定する。APIの任意Origin許可は撤去し、Webと同一Originのリバースプロキシを使う。
5. トークンはread_api等の必要最小限の権限を持つ専用アカウントで発行。ローテーションはSecret/環境変数更新後APIを再起動する。キャッシュキーは接続先・トークンをハッシュ化した識別子を含み、新旧トークンで共有しない。
6. ブラウザに保存済みの旧 `gitlab.privateToken` は初期HTMLで削除する。保存・復元・ヘッダー転送は行わない。

## 検証
- ローカルGitLabスタブと実ApiServiceを使い、サーバートークン送信、ブラウザ指定値の無視、設定欠落、401/403の安全な応答、CORS全許可の撤去を統合テスト。
- キャッシュ再利用とトークン変更時の分離、既存選択UIを回帰検証。
- Chromiumで入力UIなしのデータ表示、送信ヘッダー、旧LocalStorage削除、担当者フィルタとブラウザエラーなしを確認。
- 実GitLab用テストは `GitLab__BaseUrl` / `GitLab__PrivateToken` をAPIプロセスに設定して別途実行。実トークンや配備先への接続はこの変更に含めない。
