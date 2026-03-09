
---

# 4. docs/architecture.md ひな形

```md
# Architecture

## Overview
このシステムは GitLab CE のデータを読み取り、可視化専用の UI を提供する。

## Layers

### Domain
- 画面や API に依存しない基本モデルを配置する

### Application
- ダッシュボード用の集計ロジック
- DTO
- クエリサービス
- フィルタ処理

### Infrastructure
- GitLab REST API 呼び出し
- GitLab レスポンスモデル
- キャッシュ
- 設定

### Api
- Minimal API のエンドポイント
- OpenAPI
- 例外処理
- DI 設定

### Web
- Blazor WebAssembly
- MudBlazor UI
- API クライアント
- 画面コンポーネント

## Data Flow
1. Web が Api を呼ぶ
2. Api が Application の QueryService を呼ぶ
3. Application が Infrastructure の GitLab service を呼ぶ
4. Infrastructure が GitLab REST API を呼ぶ
5. Application が集計して DTO を返す
6. Web が表示する

## Why Minimal API
- read-only 中心で相性が良い
- endpoint 数が多すぎない
- OpenAPI と組み合わせやすい
- Program.cs を薄くしやすい

## Why Backend Aggregation
- GitLab token をブラウザに出さない
- 複数 API 呼び出しを集約できる
- キャッシュできる
- UI を単純化できる