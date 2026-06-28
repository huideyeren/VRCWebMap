# VRChat表示名とBot不要の管理者認証 設計

## 目的

Discord OAuthによる本人認証と対象Discordサーバーへの参加確認は維持しつつ、Discord Botをサーバーへ導入せずにアプリケーション管理者を管理できるようにする。

また、利用者がVRChatの表示名（Display Name）を手動登録し、アプリケーション上の表示名として利用できるようにする。閲覧は表示名未登録でも可能だが、Spotおよび関連情報の登録・更新・削除には表示名登録を必須とする。

## スコープ

この変更に含めるものは次のとおり。

- VRChat表示名の登録・変更
- VRChat表示名のアプリケーション内での一意性保証
- Discord BotとDiscordロール照会の廃止
- 設定による初期管理者の確立
- 管理者画面からの管理者権限の付与・解除
- `/admin.html` として独立した全画面管理UI
- 書き込み時のサーバー側ユーザー識別と権限判定
- 既存PostgreSQLデータを保持したschema更新
- README、使い方、利用規約、プライバシーポリシーの更新

VRChat APIによるアカウント所有確認、管理者権限の変更履歴、汎用ロールシステムは今回の対象外とする。

## 基本方針

- DiscordユーザーIDを不変のアプリケーションユーザーIDとして使い続ける。
- VRChat表示名は画面表示用の自己申告情報として扱う。
- 管理者権限はVRChat表示名ではなくDiscordユーザーIDに紐づける。
- リクエスト本文のユーザーIDや管理者フラグを認可判断に使わない。
- 初期管理者は設定されたDiscordユーザーIDで確立し、以後の管理者は管理者画面から管理する。
- Discord OAuth scopeは `identify guilds.members.read` を維持する。
- Discord Bot tokenとguild roles APIは使用しない。

VRChat表示名はVRChat側で一意だが、このアプリケーションでは所有確認を行わない。そのため、アプリ内の一意制約は入力ミスや重複登録を防ぐものであり、VRChatアカウントの所有証明にはならない。

## データモデル

既存の `DiscordUser` に次のnullableプロパティを追加する。

- `VRChatDisplayName`: 利用者が入力した表示名。前後の空白を除いた表記を保持する。
- `NormalizedVRChatDisplayName`: 一意性判定に使う正規化値。

正規化は、前後の空白除去、Unicode Form KC正規化、`ToUpperInvariant()` の順で行う。表示には正規化前の `VRChatDisplayName` を使う。

`NormalizedVRChatDisplayName` には、nullを除外した一意インデックスを設定する。これにより、表示名未登録の既存ユーザーを複数保持しながら、登録済み表示名は大文字・小文字を区別せず一意にする。

表示名の入力は、VRChatの現在の表示名条件に合わせて4文字以上15文字以下とする。文字数はUTF-16コード単位ではなくUnicodeテキスト要素として数える。

`DiscordUser.IsAdmin` は継続利用する。権限専用テーブルや汎用ロールモデルは追加しない。

## 初期管理者

`DiscordOptions` に `InitialAdminUserIds` を追加する。環境変数では、たとえば次のように設定する。

```text
Discord__InitialAdminUserIds__0=123456789012345678
```

Discordログイン時のユーザー登録・更新では、管理者状態を次の順序で決める。

1. DiscordユーザーIDが `InitialAdminUserIds` に含まれる場合は管理者にする。
2. 既存ユーザーの場合は、DBに保存済みの `IsAdmin` を維持する。
3. 新規の一般ユーザーは `IsAdmin = false` とする。

初期管理者は設定から除かれない限り、ログイン時に必ず管理者へ戻る。管理者画面から初期管理者の権限を解除する操作は拒否する。

## Discord認証

Discord OAuthの流れは次のとおり。

1. `identify guilds.members.read` scopeでDiscord OAuthを開始する。
2. OAuth access tokenでDiscordユーザー情報を取得する。
3. OAuth access tokenで対象guildのmember情報を取得し、参加状態を確認する。
4. Discordユーザーを登録または更新する。
5. DiscordユーザーIDだけを信頼できるセッション識別子としてCookieへ保存する。

guild roles APIは呼び出さない。`BotToken`、`AdminRoleName`、`HasAdminRoleAsync` と関連する説明を削除する。

Cookie内の表示名や管理者role claimを認可の正本にはしない。各認証済みリクエストでは、CookieのDiscordユーザーIDを使ってDBから最新ユーザーを読み込む。これにより、管理者画面での権限変更を次回リクエストから反映する。

## ContractsとUseCases

Kawaのcontract-first / usecase-first方針に従い、次の機能を追加する。

### VRChat表示名の更新

- Contract: `Contracts/Users/UpdateVRChatDisplayName.cs`
- UseCase: `UseCases/Users/UpdateVRChatDisplayNameUseCase.cs`

責務:

- 対象ユーザーの存在確認
- 表示名のtrim、文字数検証、正規化
- 正規化済み表示名の重複確認
- 自分の表示名だけを更新

Web adapterは対象DiscordユーザーIDを認証セッションから設定する。ブラウザーから対象ユーザーIDを指定させない。

### ユーザー一覧

- Contract: `Contracts/Users/ListUsers.cs`
- UseCase: `UseCases/Users/ListUsersUseCase.cs`

管理者画面で必要なDiscordユーザーID、Discordユーザー名、VRChat表示名、管理者状態、初期管理者かどうかを返す。実行者が管理者でない場合は `Forbidden` を返す。

### 管理者権限の変更

- Contract: `Contracts/Users/SetUserAdminStatus.cs`
- UseCase: `UseCases/Users/SetUserAdminStatusUseCase.cs`

実行者と対象者のDiscordユーザーID、設定する管理者状態を受け取り、次を検証する。

- 実行者が最新DB状態で管理者であること
- 対象ユーザーが存在すること
- 初期管理者を一般ユーザーへ変更しないこと
- 実行者自身を一般ユーザーへ変更しないこと

管理者権限の変更は管理者画面からのみ提供する。Web adapterは実行者IDを認証セッションから組み立て、ブラウザーの自己申告を信用しない。

### 現在のユーザー

`/auth/me` はDBの最新ユーザーを取得し、次を返す。

- DiscordユーザーID
- Discordユーザー名
- VRChat表示名
- VRChat表示名登録済みかどうか
- 管理者状態

画面上の主表示名にはVRChat表示名を使う。未登録時だけDiscord表示名またはDiscordユーザー名へフォールバックする。

## 既存書き込み処理の認可

Spot、VRChatWorld、PlaceInfo、WebLink、Comment、KML importの書き込みでは、transport adapterが現在ユーザーをDBから取得し、UseCaseへ実行者情報を渡す。

外部HTTP requestでは `ActorUserId`、`ActorIsAdmin`、`RegisteredByUserId` を受け付けない。既存の中心Contractが実行者情報を必要とする場合は、薄いWeb adapterが認証済みユーザーから値を設定する。外部入力用DTOはWeb transportに閉じ込め、UseCaseやDomainへHTTP型を漏らさない。

すべての書き込みに共通して次を検証する。

- Discord認証済みであること
- 対象DiscordユーザーがDBに存在すること
- `VRChatDisplayName` が登録済みであること

未登録の場合は `Forbidden` とし、表示名登録が必要であることを識別できるエラーコードまたはメッセージを返す。

所有者判定には、表示名ではなく従来どおり不変のDiscordユーザーIDを使う。

## Web UI

### プロフィール設定

ハンバーガーメニューに「プロフィール設定」を追加する。ログイン済みユーザーは、自分のVRChat表示名を登録・変更できる。

初回ログイン後に表示名が未登録の場合は、登録フォームへの案内を表示する。閲覧機能はそのまま利用できるが、投稿・編集・削除・KML importの操作は無効化し、理由を表示する。

ユーザーチップや「ログイン中」の表示はVRChat表示名を優先する。プロフィール設定では、紐付け確認のためDiscordユーザー名も補助表示する。

### 管理者画面

管理UIは地図画面の右ペインから分離し、`/admin.html` を独立したReactエントリとして実装する。地図画面のハンバーガーメニューにある「管理用画面」は、画面内状態を切り替えるボタンではなく `/admin.html` へのリンクに変更する。

管理画面はページ幅を使う全画面レイアウトとし、共通ヘッダーに次を表示する。

- 地図へ戻るリンク
- ログイン中のVRChat表示名
- ログアウト操作

管理機能は次の3タブへ分ける。

#### Spot管理

Spot名、地域、緯度、経度、登録者ID、操作をテーブル表示する。テーブル上部にSpot名検索と地域絞り込みを置く。

「編集」を選択すると、テーブル下に全幅の編集領域を開き、Spot本体とVRChatWorld、PlaceInfo、WebLink、Commentの既存編集・削除機能を表示する。管理画面のために同じ編集機能を複製せず、再利用可能なReact componentへ分離する。

#### ユーザー管理

各ユーザーについて次をテーブル表示する。

- VRChat表示名
- Discordユーザー名
- 管理者状態
- 初期管理者かどうか
- 管理者権限を付与または解除する操作

初期管理者と現在操作中の管理者自身については、解除操作を無効化する。管理者が他ユーザーのVRChat表示名を編集する機能は追加しない。

#### KMLインポート

既存のKML previewとimport機能を地図画面から移設する。import完了後はSpot管理テーブルを再読み込みする。

管理画面はデスクトップで全幅テーブルを表示し、狭い画面ではテーブルを横スクロールできるようにする。操作列は内容が判別できる幅を維持する。

`/admin.html` はページ読み込み時に `/auth/me` を取得する。未ログインの場合はDiscordログイン案内を表示し、一般ユーザーの場合はアクセス拒否と地図へ戻るリンクを表示する。管理用APIも同じ認可をサーバー側で実施する。

## エラー処理

- 不正な表示名: `Validation`
- 正規化後の表示名重複: `Conflict`
- 未ログイン: HTTP `401 Unauthorized`
- 表示名未登録での書き込み: `Forbidden`
- 一般ユーザーによる管理者操作: `Forbidden`
- 対象ユーザー不在: `NotFound`
- 初期管理者または自分自身の解除: `Conflict`

PostgreSQLの一意制約違反も捕捉し、予測可能な `Conflict` として返す。例外詳細やDB情報はクライアントへ返さない。

## PostgreSQL schema更新

このプロジェクトは現段階ではEF Core migrationsではなく `EnsureCreated()` を使っている。`EnsureCreated()` は既存テーブルへ列を追加しないため、`PostgreSqlSchemaInitializer` に冪等なschema補修を追加する。

補修内容:

1. `DiscordUsers` へ2つのnullable列を `ADD COLUMN IF NOT EXISTS` で追加する。
2. nullを除外する一意インデックスを `CREATE UNIQUE INDEX IF NOT EXISTS` で追加する。
3. 既存のSpot全文検索インデックス補修を維持する。

既存ユーザーの列はnullのままとし、既存データを削除・初期化しない。

新規DBではEF Coreモデルと補修SQLが同じ最終schemaを作る。既存DBでは補修SQLが不足列とインデックスだけを追加する。

## In-memory repository

`IDiscordUserRepository` に、ユーザー一覧取得と正規化済みVRChat表示名による検索を追加する。In-memory実装でもPostgreSQLと同じ一意性ルールを適用し、開発環境と本番相当環境で挙動をそろえる。

## ドキュメント更新

- READMEからBot scope、Bot token、Discord管理者ロール判定の必須記述を削除する。
- 初期管理者Discord IDの設定方法を追加する。
- 使い方へVRChat表示名の登録と、未登録時は書き込みできないことを追加する。
- 利用規約の管理者判定説明を更新する。
- プライバシーポリシーへVRChat表示名の保存目的を追加し、Discordロール情報を収集する記述を削除する。

## テスト

### UseCase

- 新規・既存Discordユーザーの管理者状態決定
- 初期管理者の確立
- 既存管理者状態のログイン後の維持
- VRChat表示名の登録・変更
- trim、Unicode正規化、大文字・小文字を無視した重複
- 表示名の文字数検証
- 管理者による権限付与・解除
- 一般ユーザーによる管理者操作の拒否
- 初期管理者と自分自身の解除拒否

### Web・認証

- Bot tokenなしでDiscordログインURIとcallbackが成立すること
- `/auth/me` がDBの最新表示名と管理者状態を返すこと
- 表示名未登録でも閲覧できること
- 表示名未登録ではすべての書き込みが拒否されること
- request本文のユーザーIDや管理者フラグを認可に利用しないこと

### Store・PostgreSQL

- In-memory repositoryの表示名一意性
- PostgreSQLの既存schemaへ列と一意インデックスを追加できること
- schema補修を複数回実行しても成功すること
- 既存Discordユーザーを保持したまま起動できること

### Frontend・受け入れ

- PNPMによるTypeScript型チェックとVite build
- 一般ユーザーの表示名登録と書き込み解禁
- `/admin.html` の未ログイン・一般ユーザー・管理者表示
- Spot管理テーブルの名前検索、地域絞り込み、編集
- ユーザー管理テーブルでの権限付与・解除
- KML previewとimport
- 狭い画面での管理テーブル横スクロール
- 権限変更後の次回リクエストへの反映
- Docker ComposeでPostgreSQLを使った既存volumeからの起動確認

## 完了条件

- Discord Botをguildへ導入せずにログインと参加確認ができる。
- 初期管理者が設定されたDiscordユーザーIDから確立される。
- 管理者が管理者画面から他ユーザーの管理者権限を付与・解除できる。
- `/admin.html` でSpot、ユーザー、KML importを全画面管理できる。
- 一般ユーザーは自分のVRChat表示名だけを変更できる。
- VRChat表示名が大文字・小文字を区別せず一意になる。
- 表示名未登録ユーザーは閲覧できるが、書き込みできない。
- 表示名と管理者状態の変更が再ログインなしで次回リクエストへ反映される。
- 既存PostgreSQLデータを失わずにschema更新できる。
