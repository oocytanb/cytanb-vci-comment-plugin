# cytanb-vci-comment-plugin

![NUnit](https://github.com/oocytanb/cytanb-vci-comment-plugin/workflows/NUnit/badge.svg)

- コメントメッセージの実験用プロジェクトです。

- [MultiCommentViewer | Copyright (c) ryu-s](https://github.com/CommentViewerCollection/MultiCommentViewer) (v0.5.56) のプラグインです。

- [cytanb.lua](https://github.com/oocytanb/cytanb-vci-lua) をモジュールとして設定してある VCI と組み合わせて動作します。
(例: [cytanb-comment-source](https://github.com/oocytanb/oO-vci-pack) の VCI)

- 関連するソフトウェアや、サービスのアップデートにより、正常に動作しなくなる可能性があります。よく理解された上で、ご使用ください。

## ビルド

1. [Visual Studio 2019](https://visualstudio.microsoft.com/) をインストールします。
    - ワークロード
        - .NET デスクトップ開発
    - オプション
        - F# デスクトップ言語のサポート
        - .NET Framework 4.6.2 開発ツール

1. `mcv-comment/lib/mcv/dll/` ディレクトリに、MultiCommentViewer の DLL を配置します。

1. ソリューションファイルを開き、NuGet パッケージの復元を行います。
    (ソリューションエクスプローラーの、ソリューションのコンテキストメニュー)

1. ソリューションのビルドを行います。

## 実行手順

1. `CytanbVciCommentSourcePlugin` のプラグインファイル群を、MultiCommentViewer の `plugins` ディレクトリに展開します。

1. MultiCommentViewer を起動し、プラグインメニューから `cytanb-vci-comment-source` を開き、設定を有効にします。
