# cytanb-vci-comment-plugin

![NUnit](https://github.com/oocytanb/cytanb-vci-comment-plugin/workflows/NUnit/badge.svg)

- コメントメッセージの実験用プロジェクトです。

- [MultiCommentViewer | Copyright (c) ryu-s](https://github.com/CommentViewerCollection/MultiCommentViewer) (v0.5.29) のプラグインです。

- [cytanb.lua](https://github.com/oocytanb/cytanb-vci-lua) をモジュールとして設定してある VCI と組み合わせて動作します。
(例: [cytanb-comment-source](https://github.com/oocytanb/oO-vci-pack) の VCI)

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

1. 出力されたファイル群を、MultiCommentViewer の `plugins` ディレクトリにコピーします。

1. MultiCommentViewer を起動し、プラグインメニューに `cytanb-vci-comment-source` が追加されていたら、成功です。
