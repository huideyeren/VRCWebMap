# WPPLS WorldData JSON 正式仕様対応 設計

## 背景

`POST /portal/world-data` は、登録済みの `VRChatWorld` を
PortalLibrarySystem（WPPLS）の `WorldData.json` として出力する。

WPPLS の正式な JSON 仕様が公開されたため、現行実装を仕様に合わせる。

- 公式仕様: https://wppls.genkaikogyo-ultd.com/docs/setup/json
- `ShowPrivateWorld` は省略時に `false` となり、`false` の場合は
  `ReleaseStatus: "private"` のワールドを一覧から除外するのではなく、
  WPPLS 側で選択不能にする。
- 本アプリケーションが生成する JSON では、private ワールドもポータルから
  選択可能にするため、`ShowPrivateWorld` を常に `true` とする。

## 目的

- WPPLS の正式な JSON contract に沿ったデータを出力する。
- VRChat 上のワールド公開状態と、WPPLS 上の閲覧権限を別の概念として扱う。
- 現時点で不要なロール情報を出力せず、将来の地図外ワールド登録で追加できる
  境界を記録する。

## 今回のスコープ

### Contract

`GetWorldData.Request` から `ShowPrivateWorld` を削除し、入力項目のない request とする。

`GetWorldData.Response` は次のプロパティを持つ。

- `ReverseCategorys`: 常に `false`
- `ShowPrivateWorld`: 常に `true`
- `Categorys`: 地域カテゴリごとのワールド一覧

現時点では、次のプロパティを contract に含めず、JSON にも出力しない。

- ルートの `Roles`
- カテゴリの `PermittedRoles`
- ワールドの `PermittedRoles`

出力例:

```json
{
  "ReverseCategorys": false,
  "ShowPrivateWorld": true,
  "Categorys": [
    {
      "Category": "関東",
      "Worlds": [
        {
          "ID": "wrld_00001111-2222-3333-4444-555566667777",
          "Name": "ワールド名",
          "RecommendedCapacity": 30,
          "Capacity": 60,
          "Description": "ワールドの説明",
          "Platform": {
            "PC": true,
            "Android": true,
            "iOS": false
          },
          "ReleaseStatus": "private"
        }
      ]
    }
  ]
}
```

`ID` には VRChat ワールドページ URL ではなく、WPPLS の正式仕様どおり
`wrld_...` 形式の VRChat world ID を出力する。

### UseCase

`GetWorldDataUseCase` は `VRChatWorld.IsPrivate` を理由にワールドを除外しない。
Spot が存在するすべての登録済みワールドを地域カテゴリ別にまとめる。

`VRChatWorld.IsPrivate` は個人向け公開設定ではなく、VRChat 上でそのワールドが
private release になっていることを示す。出力時は次のように変換する。

- `IsPrivate: false` → `ReleaseStatus: "public"`
- `IsPrivate: true` → `ReleaseStatus: "private"`

`ShowPrivateWorld: true` により、どちらのワールドも WPPLS 上で選択可能になる。

Spot が存在しない孤立した `VRChatWorld` は、現行どおり出力しない。
この処理に新しい業務エラーは追加しない。

### フロントエンド

WorldData のダウンロード処理は `/portal/world-data` に空の JSON object を送る。

VRChat ワールド登録・編集フォームの `Private` checkbox は、個人向けの
閲覧制限と誤解されないよう、VRChat 上の release status を表す文言へ変更する。

### ドキュメント

次の説明を正式仕様と一致させる。

- `AGENTS.md`
- `README.md`
- Contract と model の XML documentation

特に、ポータル JSON の `ID` は `wrld_...` 形式であること、`IsPrivate` は
VRChat 上の公開状態であること、ロールによる閲覧制御とは無関係であることを
明記する。

## 将来の地図外ワールド登録

地図上の Spot に紐づかないワールドを登録できる将来機能で、WPPLS の
`Roles` と `PermittedRoles` を導入する。

権限規則:

- 管理者は、全体公開またはロール限定の地図外ワールドを登録できる。
- 一般ユーザーは、ロール限定の地図外ワールドだけを登録できる。
- 管理者・一般ユーザーを問わず、ロール限定登録で使うロール名は、登録者本人の
  VRChat Display Name とする。
- そのロールの `DisplayNames` には、同じ登録者本人の VRChat Display Name を
  割り当てる。
- ロール限定ワールドの `PermittedRoles` は、対応する登録者ロールを参照する。

`ReleaseStatus` は VRChat 上の公開状態、`PermittedRoles` は WPPLS 上の閲覧権限
であり、互いに独立して設定・出力する。

この将来機能の永続化モデル、登録 contract、UseCase、管理 UI は今回実装しない。

## テスト

UseCase と serialization を中心に、次を検証する。

- public と private の両方のワールドが出力される。
- `ShowPrivateWorld` が常に `true` になる。
- `ReverseCategorys` が従来どおり `false` になる。
- `Roles` と各階層の `PermittedRoles` が JSON に存在しない。
- `ID` に `wrld_...` 形式の値がそのまま出力される。
- `IsPrivate` が `ReleaseStatus: "private"` に変換される。
- 存在する Spot の地域カテゴリと表示順が維持される。
- Spot が存在しないワールドは出力されない。

Contract 変更に伴い、次も検証する。

- .NET build と test
- frontend typecheck と production build
- OpenAPI document
- Swagger UI
- ReDoc

## 非目標

- WPPLS のロール登録・編集機能
- 地図外ワールドの永続化・登録機能
- サムネイル動画の生成
- `ReverseCategorys` の設定機能
- VRChat API からの release status 自動取得
