# Codex 環境で .NET 10 を使う手順

`dotnet` が入っていないコンテナでも、ユーザー権限で .NET SDK をローカル配置して実行できます。

## 1) install script を取得
```bash
curl -fsSL https://dot.net/v1/dotnet-install.sh -o dotnet-install.sh
chmod +x dotnet-install.sh
```

## 2) .NET 10 SDK をローカルインストール
```bash
./dotnet-install.sh --channel 10.0 --install-dir "$HOME/.dotnet"
```

> 版を固定したい場合は `--version 10.0.100` のように指定します。

## 3) PATH を通す
```bash
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$DOTNET_ROOT:$PATH"
```

## 4) 動作確認
```bash
dotnet --info
dotnet --list-sdks
```

## 5) プロジェクトで使う
```bash
dotnet restore
dotnet build
```

## 補足
- セッションごとに `export` が必要です。永続化する場合は `~/.bashrc` に追記します。
- CI/再現性重視の場合は `global.json` を追加して SDK バージョンを固定してください。
