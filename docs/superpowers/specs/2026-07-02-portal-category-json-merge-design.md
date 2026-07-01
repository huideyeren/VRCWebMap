# PortalCategory・WPPLS JSONマージ機能 設計

## 背景

地図上の場所に紐づかない VRChat ワールドも、PortalLibrarySystem（WPPLS）の
`WorldData.json` に出力できるようにする。

このワールド群は、利用者から見ると「地図に表示されないSpot」のように扱う。
ただし、通常の `Spot` は座標と地域コードを必須とするため、永続化モデルでは
座標を持たない `PortalCategory` として分離する。

本設計は次の設計を前提とし、その将来拡張部分を具体化する。

- `docs/superpowers/specs/2026-07-02-wppls-world-data-design.md`

WPPLS の正式な JSON 仕様:

- https://wppls.genkaikogyo-ultd.com/docs/setup/json

## 目的

- 地図外ワールドを、利用者が名前を付けたWPPLSカテゴリ単位で管理できる。
- PersonalカテゴリとPublicカテゴリの所有権・編集権限を明確に分離する。
- Personalカテゴリを、ログイン中ユーザー本人のVRChat Display Nameを使った
  WPPLS Roleで保護する。
- 既存のWPPLS JSONへ、本システムのCategoryとRoleを安全に追加できる。
- 通常の地図、Spot検索、地域カテゴリへPortalCategoryを混入させない。

## 用語

### PortalCategory

地図外ワールドをまとめる、座標を持たないカテゴリである。
1件の `PortalCategory` を、WPPLS JSONの1件の `Category` として出力する。

### Personal

所有者本人のVRChat Display Nameに対応するWPPLS Roleを持つカテゴリである。
WPPLS上では、そのRoleを取得した本人だけに表示される。

### Public

`PermittedRoles` を持たず、WPPLSを利用する全員に表示されるカテゴリである。

### VRChat Display Name

VRChat内で `Networking.LocalPlayer.displayName` として取得される表示名である。
本設計で「VRChat名」や「Roleに使う名前」と記載する場合は、ログイン用username
ではなくVRChat Display Nameを指す。

## データモデル

### PortalCategory

新しい `PortalCategory` modelを追加する。

- `Id: Guid`
- `RegisteredByUserId: string`
- `OwnerUserId: string?`
- `Name: string`
- `NormalizedName: string`
- `Visibility: PortalCategoryVisibility`

`PortalCategoryVisibility` は次の2値を持つ。

- `Personal`
- `Public`

`RegisteredByUserId` は実際にカテゴリを作成したユーザーの監査情報である。
`OwnerUserId` はPersonalカテゴリの所有者であり、Role生成と認可に使用する。

- Personalでは `OwnerUserId` を必須とする。
- Publicでは `OwnerUserId` をnullとする。

### カテゴリ名

- 作成・更新時に前後の空白を除去する。
- 空文字列を許可しない。
- 大文字・小文字を区別せず、全PortalCategory間で一意とする。
- `北海道`、`関東`など、既存の地域カテゴリ表示名との重複も禁止する。
- PostgreSQLでは正規化済みカテゴリ名へ一意indexを設定する。
- InMemory repositoryでも同じ一意性規則を適用する。

### 不変プロパティ

作成後は次の値を変更できない。

- `Visibility`
- `RegisteredByUserId`
- `OwnerUserId`

更新contractにはこれらの値を含めず、カテゴリ名だけを変更可能にする。
PersonalとPublicを切り替える場合は、別のカテゴリを作成してワールドを
明示的に移す。

### VRChatWorldの所属先

既存の `VRChatWorld` modelを通常SpotとPortalCategoryで共有する。

- `SpotId: Guid?`
- `PortalCategoryId: Guid?`

次の排他的関係をUseCase validationとPostgreSQL CHECK制約で保証する。

- 通常の地図ワールドは `SpotId` だけを持つ。
- 地図外ワールドは `PortalCategoryId` だけを持つ。
- 両方を持つ状態を禁止する。
- どちらも持たない状態を禁止する。

ワールドの次の情報は、所属先にかかわらず既存modelを共有する。

- 登録者ID
- VRChat world ID
- ワールド名
- 推奨人数・最大人数
- 説明
- 対応platform
- VRChat上のrelease status

`VRChatWorld.RegisteredByUserId` は実際に登録したユーザーの監査情報として保持する。
PortalCategory所属ワールドの編集権限は、個々の登録者ではなくカテゴリの
Visibilityと所有者から判定する。

## PostgreSQL schema

次の変更を行う。

- `PortalCategories` tableを追加する。
- `VRChatWorlds.SpotId` をnullableへ変更する。
- `VRChatWorlds.PortalCategoryId` nullable列を追加する。
- `PortalCategoryId` から `PortalCategories.Id` への外部キーを追加する。
- `PortalCategoryId` の外部キーは削除をcascadeせず、カテゴリにワールドが残る場合は
  DBでも削除を拒否する。
- `SpotId` と `PortalCategoryId` の片方だけが非nullであるCHECK制約を追加する。
- PortalCategoryの正規化済みカテゴリ名へ一意indexを追加する。

既存の `VRChatWorld` は現在の `SpotId` を保持し、`PortalCategoryId` をnullとする。
データ初期化は行わない。

このプロジェクトは現時点でEF Core migrationsではなく `EnsureCreated()` と
`PostgreSqlSchemaInitializer` を使っている。既存volumeを維持できるよう、
table、列、外部キー、index、CHECK制約を冪等に補修する。

## ContractとUseCase

通常Spot用のcontractへnullableな所属先を公開しない。
PortalCategoryとそのワールドには専用contract・UseCaseを用意する。

### PortalCategory

- Create
- List
- Update
- Delete

### PortalCategory所属ワールド

- Create
- Update
- Delete
- Move

通常Spot用の `CreateVRChatWorld`、`UpdateVRChatWorld`、
`DeleteVRChatWorld` は、通常Spot所属ワールドだけを扱う。

Portal用contractは、クライアントから次の値を受け取らない。

- 登録者ID
- 現在ユーザーの管理者状態
- WPPLS Role
- `PermittedRoles`

これらはcookie sessionからDBの最新ユーザー状態を解決し、UseCaseが決定する。

Personalカテゴリ作成contractは、管理者が別ユーザーを所有者として選ぶ場合に限り
`OwnerUserId` を受け取れる形にする。一般ユーザーの作成では、入力された所有者IDを
信用せず、現在ユーザー本人を所有者とする。

## 認証と認可

PortalCategoryと所属ワールドへの書き込みは、DiscordログインとVRChat Display Nameの
登録を必須とする。

### Personalカテゴリ

- VRChat Display Nameを登録済みの一般ユーザーは複数作成できる。
- 所有者は、自分のカテゴリ名と所属ワールドを作成・編集・削除できる。
- 管理者は、すべてのPersonalカテゴリと所属ワールドを操作できる。
- 管理者は、VRChat Display Nameを登録済みのユーザーを所有者として
  Personalカテゴリを作成できる。

### Publicカテゴリ

- 管理者だけが作成できる。
- 管理者だけがカテゴリ名と所属ワールドを作成・編集・削除できる。
- 一般ユーザーは閲覧だけできる。

### 削除

所属ワールドが1件でも存在するPortalCategoryの削除は `Conflict` とする。
所属ワールドを先に削除してからカテゴリを削除する。

ワールドを別のPortalCategoryへ移す場合は専用UseCaseを使う。

- 一般ユーザーは、自分が所有するPersonalカテゴリ間だけで移動できる。
- 管理者は、操作権限を持つ任意のPortalCategory間で移動できる。
- 移動元と移動先の両方について認可を確認してから所属先を更新する。

## WPPLS JSON生成

### 地域カテゴリ

通常Spotに紐づくワールドは、既存どおり地域カテゴリへまとめる。
地域カテゴリには `PermittedRoles` を付けない。

### Public PortalCategory

- `Category` は `PortalCategory.Name` とする。
- `Worlds` はそのカテゴリに所属するワールドとする。
- `PermittedRoles` を付けない。
- ワールドが0件でもCategoryを出力する。

### Personal PortalCategory

- `Category` は `PortalCategory.Name` とする。
- `Worlds` はそのカテゴリに所属するワールドとする。
- Categoryの `PermittedRoles` に、所有者のVRChat Display Nameを1件設定する。
- World単位の `PermittedRoles` は設定しない。
- ワールドが0件でもCategoryを出力する。

### Roles

Personalカテゴリを出力する場合、所有者ごとに1件のRoleを生成する。

```json
{
  "RoleName": "VRChat Display Name",
  "DisplayNames": ["VRChat Display Name"]
}
```

- `RoleName` は `OwnerUserId` から解決した所有者のVRChat Display Nameとする。
- `DisplayNames` に同じVRChat Display Nameを1件設定する。
- 同じ所有者が複数のPersonalカテゴリを持つ場合もRoleは1件だけ生成する。
- Roleは保存せず、最新のユーザー情報から出力時に生成する。
- 所有者がVRChat Display Nameを変更した場合、次回出力からCategoryとRoleの
  参照を新しいDisplay Nameへ揃える。

### ログイン状態別の出力

未ログイン時:

- 通常Spot由来の地域カテゴリを出力する。
- Public PortalCategoryを出力する。
- Personal PortalCategoryを出力しない。
- システム由来のRolesを出力しない。

ログイン時:

- 通常Spot由来の地域カテゴリを出力する。
- Public PortalCategoryを出力する。
- ログイン中ユーザー本人が所有するPersonal PortalCategoryだけを出力する。
- 本人のPersonalカテゴリが1件以上ある場合だけ、本人Roleを出力する。

管理者も、JSON出力では他人のPersonalカテゴリとRoleを取得しない。
管理権限と個人向けJSON出力範囲を分離する。

### 表示順

システム単独のJSONでは、次の順にCategoryを出力する。

1. 既存の固定順による地域カテゴリ
2. Public PortalCategoryをカテゴリ名順
3. ログイン中ユーザー本人のPersonal PortalCategoryをカテゴリ名順

各カテゴリ内のWorldはワールド名順とする。
`ShowPrivateWorld` は常に `true`、`ReverseCategorys` は常に `false` とする。

## 既存JSONとのマージ

### 入力方法

利用者はブラウザでローカルの `WorldData.json` を選択する。
ファイル内容はマージUseCaseへ送信するが、DB・filesystem・cacheへ保存しない。
ファイル本文やPersonalカテゴリ情報をapplication logへ出力しない。

JSONマージは未ログインでも利用できる。

### マージ結果

- 元JSONの未知プロパティを保持する。
- 元JSONの `ReverseCategorys` は値と省略状態を維持する。
- `ShowPrivateWorld` は存在有無にかかわらず `true` にする。
- 元JSONの `Categorys` 配列末尾へ、現在のログイン状態で出力可能な
  システムCategoryを追加する。
- 元JSONの `Roles` 配列末尾へ、現在のログイン状態で出力可能な
  システムRoleを追加する。
- 元JSONに `Roles` がなく、追加するシステムRoleもない場合は `Roles` を追加しない。
- 元JSONに `Roles` がなく、追加するシステムRoleがある場合は配列を作成する。

未ログインでマージした場合、システム側からは地域カテゴリとPublicカテゴリだけを
追加し、PersonalカテゴリとRoleは追加しない。元JSONに含まれるRoleはそのまま保持する。

### Category衝突

マージ後のCategory名は、前後空白を除いた値を大文字・小文字を区別せず比較する。
元JSON内、システム出力内、両者間のいずれでも重複がある場合は
マージ全体をValidation errorとし、ファイルを生成しない。

### Role衝突

システムRoleと同じ `RoleName` が元JSONに存在する場合:

- `DisplayNames` の集合も同一なら、既存Roleを再利用して重複追加しない。
- `DisplayNames` が異なるなら、Personalカテゴリの誤公開を防ぐため
  Validation errorとし、ファイルを生成しない。

RoleNameとDisplayNamesは、前後空白を除いた文字列をordinal比較する。
元JSON内に同じRoleNameで異なる定義が複数ある場合もValidation errorとする。

### 入力validation

- UTF-8換算で5 MiBを超える入力を拒否する。
- JSON object以外を拒否する。
- `Categorys` が存在しない、または配列でない場合は拒否する。
- `Roles` が存在し、配列でない場合は拒否する。
- Category、World、Roleの必須構造が不正な場合は拒否する。
- JSON parse errorをクライアント向けValidation errorへ変換する。
- parse例外、JSON本文、内部stack traceをレスポンスへ返さない。

未知プロパティを保持するため、マージ処理は `JsonNode` を使う。
業務ルールはUseCaseまたは純粋なmerge serviceに置き、EndpointやReactへ置かない。

## Web endpoint

既存のKawa.Webパターンに従い、Endpointはrouteとtransport metadataだけを持つ。

- PortalCategory CRUD endpoint
- PortalCategory所属ワールド CRUD endpoint
- システムWorldData出力 endpoint
- 既存WorldDataマージ endpoint

マージendpointは認証を必須にしない。
認証済みの場合はcookie sessionから最新ユーザーを解決し、本人のPersonalカテゴリを
追加対象に含める。

## フロントエンド

`/portal.html` にPortal専用画面を追加する。
通常地図画面と管理者専用画面へPortal管理UIを混在させない。

### 全利用者

- システム生成 `WorldData.json` をダウンロードできる。
- ローカルの既存 `WorldData.json` を選択できる。
- マージ結果をpreviewできる。
- validation errorと衝突箇所を確認できる。
- 成功時だけマージ済みJSONをダウンロードできる。

### ログイン済み一般ユーザー

- 自分のPersonalカテゴリを複数作成できる。
- 自分のPersonalカテゴリ名を変更できる。
- 自分のPersonalカテゴリと所属ワールドを削除できる。
- Publicカテゴリを閲覧できるが変更できない。

### 管理者

- Publicカテゴリと所属ワールドを作成・編集・削除できる。
- 全ユーザーのPersonalカテゴリと所属ワールドを管理できる。
- Personalカテゴリ作成時に所有者を選択できる。
- JSON出力とマージでは、管理者本人のPersonalカテゴリだけが追加される。

地図画面のWorldDataダウンロード導線はPortal専用画面へのリンクに置き換える。

## 通常地図からの分離

PortalCategoryは次へ含めない。

- `ListSpots`
- Spot検索
- Leaflet marker
- 地域別Spot一覧
- KML import/export
- Spot詳細
- PlaceInfo
- WebLink
- Comment

通常SpotとPortalCategoryは別repository method・UseCase・contractで扱う。

## エラー

- 未ログインの管理操作: `Forbidden`
- VRChat Display Name未登録のPersonal操作: `Forbidden`
- 一般ユーザーによるPublic変更: `Forbidden`
- 所有者以外によるPersonal変更: `Forbidden`
- 存在しないカテゴリ・ワールド: `NotFound`
- カテゴリ名重複: `Conflict`
- ワールドが残るカテゴリ削除: `Conflict`
- VRChatWorld所属先の排他条件違反: `Validation`
- 不正・過大なマージJSON: `Validation`
- Category・Role衝突: `Validation`

## テスト

### Model・repository・schema

- PortalCategoryの保存・取得・更新・削除
- 正規化済みカテゴリ名の一意性
- 地域カテゴリ表示名との衝突
- 既存VRChatWorldのSpot所属維持
- PortalCategory所属ワールドの保存
- SpotIdとPortalCategoryIdの排他制約
- InMemoryとPostgreSQLの同一挙動
- 既存PostgreSQL volumeを保持した冪等schema更新

### UseCaseと認可

- 一般ユーザーが複数のPersonalカテゴリを作成できる。
- 一般ユーザーは自分のPersonalカテゴリとワールドだけを変更できる。
- 管理者はPublicと全Personalを管理できる。
- 一般ユーザーはPublicを変更できない。
- Visibilityと所有者を更新できない。
- ワールドが残るカテゴリを削除できない。

### JSON出力

- 未ログイン時は地域とPublicだけを出力する。
- ログイン時は本人のPersonalだけを追加する。
- 管理者でも他人のPersonalを出力しない。
- 同一所有者の複数PersonalカテゴリでRoleを1件だけ生成する。
- Personal CategoryのPermittedRolesとRoleNameが一致する。
- DisplayNamesに最新のVRChat Display Nameを設定する。
- World単位のPermittedRolesを出力しない。
- CategoryとWorldの順序が決定的である。

### JSONマージ

- 未ログインでも利用できる。
- 元JSONの未知プロパティを保持する。
- ReverseCategorysの値と省略状態を保持する。
- ShowPrivateWorldをtrueにする。
- CategorysとRolesの末尾へ追加する。
- 同一定義Roleを再利用する。
- 異なるRole定義の衝突を拒否する。
- Category名重複を拒否する。
- 不正構造、parse error、5 MiB超過を拒否する。
- エラー時に部分的なマージ結果を返さない。

### HTTP・frontend

- OpenAPI contract
- Swagger UI
- ReDoc
- 未認証でのJSON出力・マージ
- 認証済み一般ユーザーと管理者のPortal画面
- frontend typecheck
- production build
- 通常地図・検索・Spot詳細への非混入

## 実装順序

1. `2026-07-02-wppls-world-data-design.md` の正式仕様対応を実装する。
2. PortalCategory model・repository・PostgreSQL schemaを追加する。
3. PortalCategoryとPortal所属ワールドのUseCase・認可を追加する。
4. ログイン状態別のWorldData生成とRole生成を追加する。
5. JSON merge UseCaseを追加する。
6. Portal専用画面を追加する。
7. OpenAPI・実行時・PostgreSQL回帰検証を行う。

## 非目標

- 外部URLからのJSON取得
- アップロードJSONの永続化
- PersonalとPublicの作成後切り替え
- PortalCategory所有者の作成後変更
- World単位のPermittedRoles
- VRChat APIによるDisplay Nameやrelease statusの自動取得
- WPPLSサムネイル動画の生成・マージ
