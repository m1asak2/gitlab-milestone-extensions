# OpenShift Deployment

## Purpose

この文書は、本リポジトリを OpenShift 上で動かすための前提、差分、最小構成をまとめたものです。

対象は、既存の Docker 2 コンテナ構成を大きく崩さずに OpenShift へ載せ替えることです。

## Target Topology

- `web`
  - Blazor WebAssembly の静的ファイルを配信する
  - `/api/*` を `api` Service へプロキシする
  - OpenShift `Route` の公開先になる
- `api`
  - ASP.NET Core Minimal API を提供する
  - GitLab API へアクセスする
  - Cluster 内では `Service` 経由で `web` から到達される

## Current Fit For OpenShift

このリポジトリは、もともとのコンテナ分離が OpenShift に載せやすい形になっています。

そのままでは注意が必要な点:

- `api` の `/health` と `/alive` は development 環境でしか公開されない
- `web` は OpenShift restricted SCC を意識して、非特権 nginx イメージを使う方が安全
- `docker-compose` の `extra_hosts: host-gateway` は OpenShift では使えない

今回の対応では次を前提にしています。

- `web` は `nginxinc/nginx-unprivileged` ベースにする
- `api` は `ExposeDefaultHealthEndpoints=true` を与えたときだけ probe 用 endpoint を公開する
- GitLab は OpenShift Cluster から到達できる URL を `GitLab__BaseUrl` に設定する

## Configuration Mapping

Docker Compose から OpenShift への主な読み替え:

- `web` 公開
  - Compose: `ports`
  - OpenShift: `Service` + `Route`
- `api` 内部公開
  - Compose: `expose`
  - OpenShift: `Service`
- `GitLab__BaseUrl`
  - Compose: `environment`
  - OpenShift: `ConfigMap` または `Deployment` の env

推奨する環境変数:

- `ASPNETCORE_ENVIRONMENT=Production`
- `GitLab__BaseUrl=https://<your-gitlab>`
- `GitLab__PrivateToken`（APIだけへSecretから注入）
- `ExposeDefaultHealthEndpoints=true`

## Security Notes

### `web`

- OpenShift ではランダム UID で起動される可能性がある
- そのため root 前提の nginx より、非特権実行を前提にしたイメージを使う
- 待受ポートは `8080` のまま維持する

### `api`

- `dotnet/aspnet` イメージで `8080` 待受のため、特権ポートは不要
- 永続ボリュームは不要
- health endpoint は probe 用にだけ有効化する

## Health Probes

`api` は以下を使います。

- readiness: `/health`
- liveness: `/alive`

どちらも `ExposeDefaultHealthEndpoints=true` のときだけ公開されます。

`web` は静的配信のみのため、`/` に対する HTTP probe で十分です。

## Networking

`web` の nginx 設定は `proxy_pass http://api:8080;` です。

OpenShift では `api` Service 名を `api` にしておけば、そのまま名前解決できます。

別 namespace をまたぐ場合は、`api.<namespace>.svc.cluster.local` のように nginx 設定を変更してください。

## Build / Release Strategy

最小構成では、既存 Dockerfile をそのまま使って外部レジストリへ push し、OpenShift では `Deployment` から参照します。

想定フロー:

1. `api` イメージを build/push
2. `web` イメージを build/push
3. 配備先で管理しているDeployment/Service/Routeを更新
4. `gitlab-reader` Secretを用意し、APIへの設定パッチを適用

OpenShift BuildConfig に寄せることもできますが、現時点ではコンテナ化済みの成果を流用する方がシンプルです。

## Existing Deploymentへの設定適用

Deployment/Service/Route本体は配備先の管理対象です。このリポジトリには、既存の `api` Deployment・`api` コンテナに設定を追加するための `openshift/api-gitlab-settings.patch.yaml` を用意しています。

1. GitLabへの疎通URLをcluster目線で確定する。
2. 最小権限の共通トークンを発行し、安全な入力手段で `gitlab-reader` Secret の `base-url` / `private-token` キーを設定する。値をGitやログに残さない。
3. 対象namespaceで `oc patch deployment api --patch-file openshift/api-gitlab-settings.patch.yaml` を実行する。このファイルはDeployment本体ではないため `oc apply -f openshift/` は使用しない。
4. `oc rollout status deployment/api` で設定反映を確認する。認証情報の更新時もAPIを再起動する。
5. 許可された利用者だけがRouteへ到達する認証・ネットワーク制御を確認した後、UIと同一Originの `/api` 疎通を確認する。

## Notes

- GitLab private token はAPIの `GitLab__PrivateToken` にSecretから注入する。ブラウザには保存しない
- Route を TLS 終端にしても、ブラウザから見た API 呼び出しは同一 origin の `/api/...` で継続できる
- `openshift/api-gitlab-settings.patch.yaml` と `docs/shared-gitlab-token-design.md` の手順を参照し、Ingress/Route側の認証・公開範囲を配備前に設定する
