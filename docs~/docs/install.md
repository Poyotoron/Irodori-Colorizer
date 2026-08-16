# インストール

## 動作環境

| 項目 | 要件 |
|---|---|
| Unity | 2022.3 (LTS) 以降 |
| VRChat SDK | **不要**（入っていても問題ありません） |
| OS | Windows / macOS |

!!! note "VRChat 専用ツールではありません"
    アバター改変向けのラベル一式を同梱していますが、SDK には依存していません。
    一般的な Unity プロジェクトでもそのまま使えます。

## VCC / ALCOM から導入する（推奨）

1. VCC または ALCOM に、配布元のリポジトリを追加します。
2. 対象の Unity プロジェクトを開き、**Manage Packages** を選びます。
3. 一覧から「Irodori Colorizer」を追加します。

## zip から導入する

VCC を使っていない場合は、[リリースページ](https://github.com/Poyotoron/Irodori-Colorizer/releases)から
`.unitypackage` をダウンロードし、Unity のプロジェクトへドラッグ＆ドロップしてインポートしてください。
zip を `Packages/` 配下へ展開する形でも導入できます。

## 導入できたか確認する

Project ウィンドウでフォルダを右クリックし、次の項目が増えていればインストール成功です。

```
Irodori Colorizer > Set Label…
```

設定画面は `Edit > Project Settings > Irodori Colorizer` から開きます。

## 設定の保存先

有効なプリセット、ラベルの編集内容、どの対象にどのラベルを付けたかは、
すべて次のファイルにまとめて保存されます。

```
ProjectSettings/IrodoriColorizer.asset
```

!!! tip "`Assets/` を汚さず、チームで共有できます"
    設定は `Assets/` の外にあるため、アセットの一覧に余計なファイルが増えません。
    このファイルを Git で共有すれば、**同じプロジェクトを開いた全員に同じ色が付きます。**

## アンインストール

VCC の Manage Packages から削除します（`.unitypackage` で入れた場合は、
`Packages/` 配下のフォルダを削除します）。

アンインストールしても `ProjectSettings/IrodoriColorizer.asset` は残ります。
再導入すれば、以前の色がそのまま復元されます。完全に消したい場合は、
このファイルを手で削除してください。

!!! success "アンインストールしてもプロジェクトは壊れません"
    このツールは色を描いているだけで、アセットやシーンには何も書き込みません。
    削除しても、色が付かなくなるだけです。
