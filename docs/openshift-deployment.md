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
3. `openshift/*.yaml` を apply
4. `ConfigMap` の GitLab URL を実環境値へ更新

OpenShift BuildConfig に寄せることもできますが、現時点ではコンテナ化済みの成果を流用する方がシンプルです。

## Sample Manifests

サンプル manifest は `openshift/` ディレクトリに配置しています。

- `openshift/api.yaml`
- `openshift/web.yaml`

これらは次を前提にした最小例です。

- namespace 内に `api` / `web` Deployment を作る
- `web` のみ `Route` で外部公開する
- イメージ名は適宜置き換える

## Migration Checklist

1. GitLab への疎通 URL を cluster 目線で確定する
2. `api` / `web` イメージをレジストリへ push する
3. manifest 内のイメージ参照を置き換える
4. `openshift/api.yaml` の `GitLab__BaseUrl` を実値に変更する
5. `oc apply -f openshift/` を実行する
6. `oc get pods` と `oc logs` で起動確認する
7. `Route` 経由で UI と `/api` 疎通を確認する

## Notes

- いまの実装では GitLab private token はブラウザの local storage に保持される
- Route を TLS 終端にしても、ブラウザから見た API 呼び出しは同一 origin の `/api/...` で継続できる
- 将来的に OpenShift Secret や SSO を使うなら、この token 受け渡し方式は見直し候補になる
