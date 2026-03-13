# GitLab Milestone Dashboard

GitLab CE のマイルストーン、Issue、担当者情報を可視化するための表示専用ダッシュボードです。

現在の UI は milestone 選択を中心に構成されており、`Dashboard` / `Issues` / `Gantt` の 3 タブで確認できます。

## 目的

GitLab 標準画面だけでは把握しづらい以下の情報を見やすく表示します。

- グループ別の進捗
- プロジェクト別の進捗
- 担当者別の進捗
- Issue 一覧
- ガント風のタイムライン表示
- サマリー指標の表示
- GitLab milestone / issue / project への直接リンク
- Issues タブ内の state フィルタ

## 前提

このリポジトリは、Visual Studio で作成した **Aspire Starter App** の雛形をベースに構築します。

Aspire Starter App により生成される以下は、開発体験向上のため維持します。

- `AppHost`
- `ServiceDefaults`

一方で、本プロジェクトに不要なサンプルコードや初期ファイルは削除します。

## 技術方針

- .NET 10
- ASP.NET Core Minimal API
- Blazor
- MudBlazor
- Aspire AppHost
- Aspire ServiceDefaults

## 開発環境

- Visual Studio
- VSCode
- VSCode 上では Codex を利用して実装支援を行う

## スコープ

このアプリは **表示専用** です。

### 実施すること
- GitLab API からデータ取得
- ダッシュボード表示
- テーブル表示
- ガント風表示
- フィルタ表示

### 実施しないこと
- GitLab の Issue / Milestone 編集
- GitLab への書き戻し
- ローカル DB 永続化
- 背景ジョブ
- Webhook 連携
- 認証の作り込み（初期段階では対象外）

## ベース構成

Aspire 雛形をベースに、必要最小限の構成へ整理します。

想定構成:

```text
gitlab-milestone-extensions/
├─ docs/
├─ gitlab-milestone-extensions.AppHost/
├─ gitlab-milestone-extensions.ServiceDefaults/
├─ gitlab-milestone-extensions.ApiService/
├─ gitlab-milestone-extensions.Web/
└─ gitlab-milestone-extensions.Tests/
```

## OpenAPI
https://localhost:7364/scalar/v1

## Docker

Docker 構成と運用方針は `docs/docker-deployment.md` を参照してください。
