# Docker Deployment

## Purpose

この文書は、本リポジトリに対する Docker 適用方針と配置ルールをまとめたものです。

対象は本番相当構成の再現であり、日常開発の主系統は引き続き `gitlab-milestone-extensions.AppHost` を使用します。

## Scope

- コンテナ化対象
  - `gitlab-milestone-extensions.ApiService`
  - `gitlab-milestone-extensions.Web`
- コンテナ化しないもの
  - `gitlab-milestone-extensions.AppHost`
  - `gitlab-milestone-extensions.Tests`
- 共有ライブラリ
  - `gitlab-milestone-extensions.ServiceDefaults`

## Directory Layout

```text
docker/
├─ api/
│  └─ Dockerfile
├─ compose/
│  ├─ .env.example
│  └─ docker-compose.yml
└─ web/
   ├─ Dockerfile
   └─ nginx/
      └─ default.conf
```

## Runtime Topology

- `web` コンテナ
  - Blazor WebAssembly の静的ファイルを配信する
  - nginx で `/api/*` を `api` コンテナへプロキシする
- `api` コンテナ
  - ASP.NET Core Minimal API を起動する
  - GitLab API へアクセスする

ブラウザから見える入口は `web` のみで、`Web` は相対パス `/api/...` を利用します。

## Dockerfile Responsibilities

### `docker/api/Dockerfile`

- `ApiService` と `ServiceDefaults` を含めて restore / publish する
- 実行イメージは ASP.NET Core Runtime を使用する
- 本番向けポートとして `8080` を公開する

### `docker/web/Dockerfile`

- `Web` を publish する
- publish 結果の `wwwroot` を nginx イメージへコピーする
- nginx 設定で SPA fallback と `/api` プロキシを提供する

## `docker-compose.yml` Role

`docker/compose/docker-compose.yml` は本番そのものではなく、2 サービス構成をローカルで再現するために使用します。

役割:

- `api` と `web` の build 定義を持つ
- `web` のみをホストへ公開する
- `api` は compose ネットワーク内だけで到達可能にする
- `GitLab__BaseUrl` を `api` に環境変数で注入する

## Configuration

`ApiService` の Docker 実行時に外出しする設定は次のみです。

- `GitLab__BaseUrl`

現在の実装では、GitLab private token はブラウザから `X-GitLab-Private-Token` ヘッダで API へ送信されます。トークンの保存先は Web 側の local storage です。

ローカルの `docker-compose` で `http://gitlab.local` を利用する場合、`api` サービスには `gitlab.local:host-gateway` の `extra_hosts` を設定し、Docker コンテナからホスト側 GitLab に到達できるようにします。

## nginx Routing Policy

`docker/web/nginx/default.conf` の責務は次の 2 つです。

1. `/api/*` を `api:8080` に転送する
2. それ以外は静的ファイルとして配信し、未解決パスは `index.html` にフォールバックする

この構成により、Blazor WebAssembly は絶対 URL を持たず、同一オリジンの相対パスで API を呼び出せます。

## Usage

1. `docker/compose/.env.example` を `docker/compose/.env` としてコピーする
2. `GITLAB_BASE_URL` を実環境に合わせて設定する
3. `docker compose -f docker/compose/docker-compose.yml up --build` を実行する
