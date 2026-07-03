# 登録者のVRChat表示名表示 設計

## 目的

Spotと関連データの登録者欄にDiscordユーザーIDが表示されている状態を解消し、最新のVRChat Display Nameを表示する。

公開APIから登録者のDiscordユーザーIDを除外し、表示と編集UIに必要な情報だけを返す。一方、保存済みのDiscordユーザーIDは、所有者認可と監査に必要な内部識別子として維持する。

## 対象

- Spot
- VRChat World
- PlaceInfo
- WebLink
- Comment
- Spot一覧、Spot詳細、作成、更新、KMLインポートの各レスポンス
- 公開地図画面と管理画面の登録者表示
- フロントエンドの編集可能判定

## 対象外

- 管理者専用ユーザー管理APIのDiscordユーザーID
  - 管理者権限を変更する対象の識別に必要なため、今回の除外対象としない。
- `/portal/world-data` のロール専用閲覧機能
  - PortalLibrarySystem開発者から正式なスキーマ情報を受領した後、別課題として設計する。
- 登録時点のVRChat Display Nameの履歴保存

## 基本方針

### 内部識別子

`Models/` の各モデルが持つ `RegisteredByUserId` は変更しない。

更新・削除時の認可は、従来どおり保存済みの `RegisteredByUserId` と `ICurrentActorAccessor` が返す現在ユーザーのDiscord IDを比較して行う。VRChat Display Nameは変更可能なため、所有者認可には使用しない。

### 公開Contract

永続モデルを外部レスポンスとして直接返さず、`Contracts/` にリソースごとの公開用DTOを定義する。

公開用DTOは既存の公開項目に加えて、次の項目を持つ。

- `RegisteredByDisplayName`: レスポンス生成時点の最新VRChat Display Name
- `CanEdit`: 現在ユーザーが対象データを編集できる場合は `true`

公開用DTOには `RegisteredByUserId` を含めない。

`ListSpots`、`GetSpot`、各リソースの作成・更新、KMLインポートなど、Spot系リソースを返すすべての外部Contractを公開用DTOへ統一する。

## データフロー

1. UseCaseがSpotまたは関連データをリポジトリから取得する。
2. UseCaseはDiscordユーザー一覧と現在ユーザーを取得する。
3. レスポンス生成用マッパーをUseCase実行単位で作成し、Discordユーザー一覧をIDで検索できる辞書にする。
4. 各リソースの `RegisteredByUserId` に対応する最新の `VRChatDisplayName` を解決する。
5. 現在ユーザーが管理者、または登録者本人の場合に `CanEdit = true` とする。
6. マッパーが永続モデルを公開用DTOへ変換し、UseCaseがレスポンスとして返す。

複数件を返すUseCaseでは、項目ごとのユーザー問い合わせを行わない。ユーザー一覧は一度だけ取得して辞書化し、同一レスポンス内の表示名解決に再利用する。

## 欠損時の扱い

次の場合、`RegisteredByDisplayName` は `不明なユーザー` とする。

- `RegisteredByUserId` に対応するDiscordユーザーが存在しない。
- Discordユーザーは存在するが、`VRChatDisplayName` が未登録または空白である。

表示名を解決できなくても、一覧や詳細取得は失敗させない。

未ログインの場合は `CanEdit = false` とする。表示用の `CanEdit` は利便性のための情報であり、更新・削除時のサーバー側認可を省略しない。

## 名前変更時の挙動

VRChat Display Nameはレスポンス生成時にユーザーリポジトリの現在値から解決する。

ユーザーがVRChat Display Nameを変更した場合、過去に登録したSpotと関連データにも、次回取得時から新しい名前を表示する。各リソースには表示名の複製を保存しない。

## フロントエンド

次の表示で `RegisteredByDisplayName` を使用する。

- Spot詳細
- VRChat World
- PlaceInfo
- WebLink
- Comment
- 管理画面のSpot一覧
- 登録・編集フォーム内の登録者表示

表示ラベルは既存の `追加ユーザー` を維持する。

フロントエンドの所有者判定は、DiscordユーザーIDの比較から `CanEdit` の参照へ変更する。管理者を含む編集UIの表示可否はAPIが返す `CanEdit` を使用する。

## ポータルJSONとの関係

内部の所有者認可は不変なDiscordユーザーID、外部表示と将来のポータル連携はVRChat Display Nameという境界を維持する。

既存のポータルContractにはロール対象の表示名を表す `Roles[].DisplayNames` があるが、正式なスキーマ情報を受領するまでは生成処理を変更しない。

## テスト

### UseCase

- 最新のVRChat Display NameがSpotと全関連データの公開用DTOへ設定される。
- VRChat Display Name変更後の再取得で新しい名前が返る。
- Discordユーザーが存在しない場合に `不明なユーザー` が返る。
- VRChat Display Nameが未登録または空白の場合に `不明なユーザー` が返る。
- 所有者の場合に `CanEdit = true` が返る。
- 管理者の場合に `CanEdit = true` が返る。
- 第三者と未ログインの場合に `CanEdit = false` が返る。
- 作成・更新レスポンスでも公開用DTOが返る。
- 更新・削除時の既存認可が維持される。

### ContractとOpenAPI

- Spot系の外部レスポンススキーマに `RegisteredByUserId` が含まれない。
- `RegisteredByDisplayName` と `CanEdit` がOpenAPIへ出力される。
- 管理者専用ユーザー管理Contractには必要なDiscordユーザーIDが維持される。

### フロントエンド

- 生のDiscordユーザーIDを登録者欄へ表示しない。
- 所有者判定のためのID比較が残っていない。
- 公開画面と管理画面が `RegisteredByDisplayName` を表示する。
- `CanEdit` に応じて編集UIが表示される。
- TypeScriptの型検査とプロダクションビルドが成功する。

### 回帰確認

- `/portal/world-data` の出力が変更されていない。
- Spotと関連データの作成、更新、削除に関する認可テストが成功する。

## 受け入れ条件

- 公開画面と管理画面の登録者欄にDiscordユーザーIDが表示されない。
- 登録者の最新VRChat Display Nameが表示される。
- 表示名を解決できないデータは `不明なユーザー` と表示される。
- Spot系の外部APIレスポンスに登録者のDiscordユーザーIDが含まれない。
- 所有者と管理者だけが従来どおり編集できる。
- ポータルJSONの出力に回帰がない。
