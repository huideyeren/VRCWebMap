# VrcWebMap.Backend

VrcWebMap.Backend is a prototype backend for a map application whose frontend
uses leaflet.js to display spots on a map.

The backend manages map spots that can be enriched with VRChat worlds and their
metadata, real-world place information, Web links, and free-form comments.
`PlaceInfo` stores a place name, address, and Markdown-friendly business
information so lunch hours, dinner hours, regular holidays, and notes can be
written together. `WebLink` stores a site name and URL independently from
`PlaceInfo`, so links can also describe non-restaurant information. A single
`Spot` is the central map point, belongs to one prefecture or regional area,
and can have many related `VRChatWorld`, `PlaceInfo`, `WebLink`, and `Comment`
records.

The application follows Kawa's contract-first and usecase-first style:

- `Contracts/` contains Kawa request and response contracts.
- `UseCases/` contains transport-independent application flows.
- `Endpoints/Web/` exposes use cases through Kawa.Web endpoints.
- `Models/` contains simple C# records for the current data shape.
- `Stores/` contains the current in-memory repository implementation.

## Status

This project is experimental. The current storage implementation is in-memory
and is intended for prototyping only.

## Requirements

- .NET 10 SDK
- Docker / Docker Compose for PostgreSQL-backed local runs

## Database

The application uses the in-memory repository by default. To run with
PostgreSQL, set:

```text
Database__Provider=PostgreSQL
ConnectionStrings__Postgres=Host=localhost;Port=5432;Database=vrcwebmap;Username=vrcwebmap;Password=vrcwebmap
```

The PostgreSQL implementation uses EF Core with Npgsql and creates the current
schema at startup with `EnsureCreated()` for prototype use.

Run the application with PostgreSQL through Docker Compose:

```bash
docker compose up --build
```

Redis is optional and currently not required by the application. If cache
support is needed later, start the Redis service with:

```bash
docker compose --profile cache up --build
```

## Discord Users

User registration and login are intended to use Discord OAuth. Application
users must be members of the configured Discord guild.

The Discord OAuth transport should request `identify` and `guilds.members.read`,
verify the user's membership in `Discord:RequiredGuildId` through the Discord
API, then pass only the verified result to `RegisterDiscordUserUseCase`. Do not
trust guild membership values submitted directly by a browser client.

Initial administrators are configured by immutable Discord user ID through
`Discord:InitialAdminUserIds`. Administrators can then grant or revoke
administrator status from `/admin.html`. A Discord Bot is not required.

After login, users register their unique VRChat Display Name. Reading remains
available before registration, but creating or editing map content requires a
registered VRChat Display Name.

In the Development environment, Discord OAuth may be unavailable. The backend
therefore exposes development-only sample users:

- `GET /auth/dev/login/admin`: signs in as `dev-admin-user`, an application administrator.
- `GET /auth/dev/login/user`: signs in as `dev-general-user`, a normal user.
- `GET /auth/dev/users`: returns the available development sample users.

These endpoints are registered only when `IWebHostEnvironment.IsDevelopment()`
is true.

### Discord Authentication Settings

Create a Discord application in the Discord Developer Portal, then configure an
OAuth2 redirect URL that points to this backend:

```text
http://localhost:5021/auth/discord/callback
```

For local development, set the following configuration values with environment
variables or user secrets. Do not commit real client secrets.

```text
Discord__ClientId=<discord-application-client-id>
Discord__ClientSecret=<discord-application-client-secret>
Discord__RedirectUri=http://localhost:5021/auth/discord/callback
Discord__RequiredGuildId=<discord-guild-id>
Discord__InitialAdminUserIds__0=<initial-administrator-discord-user-id>
```

The OAuth login URL should request these scopes:

```text
identify guilds.members.read
```

Do not add the `bot` scope to the login URL. Guild membership is checked with
the user's OAuth access token, and application administrator status is managed
inside VRC Web Map.

## Restore, Build, and Test

In this Codex workspace, NuGet package writes are routed to `/private/tmp` to
avoid sandbox write restrictions.

```bash
dotnet restore \
  --source /private/tmp/nuget-local \
  --packages /private/tmp/nuget-packages \
  -p:NuGetAudit=false

dotnet build --no-restore \
  -p:RestorePackagesPath=/private/tmp/nuget-packages
```

Run tests:

```bash
dotnet test VrcWebMap.Backend.Tests/VrcWebMap.Backend.Tests.csproj \
  --no-restore \
  -p:RestorePackagesPath=/private/tmp/nuget-packages
```

Outside this sandbox, standard `dotnet restore`, `dotnet build`, and
`dotnet test` should be usable if the local NuGet cache is writable.

## API

Discord authentication endpoints are exposed by the web transport:

- `GET /auth/discord/login`
- `GET /auth/discord/callback`
- `POST /auth/logout`
- `GET /auth/me`
- `GET /auth/dev/users` Development only
- `GET /auth/dev/login/{userKind}` Development only

Spot management endpoints are exposed through Kawa use cases:

- `POST /spots/list`
- `POST /spots/get`
- `POST /spots/create`
- `POST /spots/update`
- `POST /spots/delete`

Spot-related information is registered after creating a `Spot`. These endpoints
therefore require `spotId` in the request body:

- `POST /vrchat-worlds/create`
- `POST /vrchat-worlds/update`
- `POST /vrchat-worlds/delete`
- `POST /place-infos/create`
- `POST /place-infos/update`
- `POST /place-infos/delete`
- `POST /web-links/create`
- `POST /web-links/update`
- `POST /web-links/delete`
- `POST /comments/create`
- `POST /comments/update`
- `POST /comments/delete`

Update and delete endpoints require `actorUserId` and `actorIsAdmin`. Only an
administrator or the user who registered the target data can update or delete it.
List and detail endpoints are public. Spot detail responses include the spot and
its related VRChat worlds, place infos, Web links, and comments.

The temporary frontend shows an administrator-only editing panel when
`/auth/me` reports `isAdmin: true`. This panel can update or delete `Spot`,
`VRChatWorld`, `PlaceInfo`, `WebLink`, and `Comment` records. Regular users can
still use the public registration forms.

`Spot` deletion is blocked when related `VRChatWorld`, `PlaceInfo`, `WebLink`,
or `Comment` records still exist. Delete the related records first.

The map uses a default center when no location context is available. If browser
geolocation succeeds, the frontend centers the map around the current position
without saving that location to the server. If the URL contains `?spotId={id}`,
`?spot={id}`, or `#spot={id}`, that Spot is loaded and used as the map center
instead. Selecting a Spot updates the URL to `?spotId={id}` for direct linking.

Web links and VRChat world page URLs can show an OGP preview through
`POST /web-links/preview`. The preview is fetched server-side and is not stored
as part of `WebLink` or `VRChatWorld`. The preview fetch accepts only public
`http` / `https` URLs and rejects localhost or private network addresses.

Portal export endpoints are also exposed through Kawa use cases:

- `POST /portal/world-data`

`/portal/world-data` returns VRChat worlds grouped by Japanese regional
category names using the official `WorldData.json` shape. The request body is
an empty JSON object. The response always sets `ShowPrivateWorld` to `true`,
keeps both public and private VRChat releases selectable, and writes the raw
`wrld_...` value to each world `ID`. `ReleaseStatus` describes the VRChat
release status; it is not a WPPLS access-control setting. `Roles` and
`PermittedRoles` are intentionally omitted until off-map world registration is
implemented.

This portal export is intended for Genkai Kogyo's `PortalLibrarySystem (WPPLS)`,
a VRChat portal system distributed on BOOTH:
https://booth.pm/ja/items/6659099

Kawa API catalog and OpenAPI endpoints are also mapped by the application.
Swagger UI and ReDoc are enabled in development.
The temporary frontend shows a development-only Swagger button in the top menu
when those documentation pages are enabled.

## Temporary Frontend

`src/` contains the React + Leaflet frontend source. PNPM and Vite bundle it to
`wwwroot/assets/`, and ASP.NET Core serves the built files through static file
middleware.

- Right-click the map to open a Spot registration form for that location.
- Click a Spot marker to show the Spot and related data in the right pane.
- Use `pnpm install` followed by `pnpm build` to create the frontend bundle for
  local development.
- Dependency management uses PNPM. Commit `pnpm-lock.yaml`, and do not use
  `npm install` or `npm ci` for this project.
- The frontend entry point is `src/main.tsx` so UI work can move toward typed
  React components incrementally.
- Docker builds run the PNPM/Vite frontend build before publishing the ASP.NET
  Core application.
- PNPM supply-chain controls such as `minimumReleaseAge`, `trustPolicy`,
  `blockExoticSubdeps`, and explicit build-script approval should be considered
  before adding frontend dependencies.
- ASP.NET Core sends a Content Security Policy that keeps scripts, styles, and
  API connections on the current origin. Map tile images may still load from
  external HTTPS tile providers.

## License

This project is licensed under the MIT License. See `LICENSE`.

Dependency license notes are listed in `NOTICE`.

## Data and Trademark Notes

This repository's MIT License applies to the source code in this project.

VRChat names, identifiers, trademarks, logos, and any data obtained from VRChat
or third-party services are not granted by this repository's license. If this
project later imports or republishes external data, confirm the applicable
terms of service and data licensing separately.

PlaceInfo business information is free-form Markdown-friendly text. WebLink
records can point to external services such as Gurunavi, Tabelog, Retty, X, or
Instagram, and those linked services may be subject to their own terms. Treat
those rights and usage conditions separately from this source-code license.

---

# VrcWebMap.Backend 日本語版

VrcWebMap.Backend は、フロントエンド側で leaflet.js を使って地図を表示する
アプリケーションのためのプロトタイプバックエンドです。

このバックエンドは、地図上のスポットを管理します。各スポットには、VRChat
ワールドとその情報、現実側の場所情報、Web リンク、その他の自由コメントを追記できる形とします。
`PlaceInfo` は場所名、住所、Markdown 対応の営業情報を保持し、昼営業、夜営業、
定休日、臨時休業などをまとめて記載できる形にします。`WebLink` はサイト名と URL を
保持し、飲食店以外の関連サイトも扱える形にします。
1つの `Spot` が地図上の中心地点であり、都道府県または地域情報が1つ紐づきます。
また、複数の `VRChatWorld`、`PlaceInfo`、`WebLink`、`Comment` を紐づけられます。

このアプリケーションは、Kawa の contract-first / usecase-first スタイルに従います。

- `Contracts/` は Kawa の request / response contract を配置します。
- `UseCases/` は transport に依存しないアプリケーションフローを配置します。
- `Endpoints/Web/` は Kawa.Web endpoint として UseCase を公開します。
- `Models/` は現在のデータ形状を表す単純な C# record を配置します。
- `Stores/` は現在のインメモリ repository 実装を配置します。

## 状態

このプロジェクトは実験段階です。現在の storage 実装はインメモリであり、
プロトタイピング用途を想定しています。

## 必要環境

- .NET 10 SDK
- PostgreSQL 付きでローカル実行する場合は Docker / Docker Compose

## データベース

既定ではインメモリ repository を使います。PostgreSQL で実行する場合は、以下を設定します。

```text
Database__Provider=PostgreSQL
ConnectionStrings__Postgres=Host=localhost;Port=5432;Database=vrcwebmap;Username=vrcwebmap;Password=vrcwebmap
```

PostgreSQL 実装は EF Core + Npgsql を使います。現段階ではプロトタイプ用途として、
起動時に `EnsureCreated()` で現在の schema を作成します。

Docker Compose で PostgreSQL 付きのアプリケーションを起動できます。

```bash
docker compose up --build
```

Redis は任意で、現時点のアプリケーションでは必須ではありません。後でキャッシュが必要になった場合は、
以下の profile で Redis も起動できます。

```bash
docker compose --profile cache up --build
```

## Discord ユーザー

ユーザー登録とログインは Discord OAuth を使う想定です。アプリケーションを利用できる
ユーザーは、設定された Discord サーバーに参加している Discord ユーザーに限定します。

Discord OAuth transport は `identify` と `guilds.members.read` を要求し、Discord API で
`Discord:RequiredGuildId` への参加を確認してから、確認済みの結果だけを
`RegisterDiscordUserUseCase` に渡します。ブラウザクライアントから送られたサーバー参加状態は信用しません。

初期管理者は `Discord:InitialAdminUserIds` に不変の Discord ユーザー ID を設定して確立します。
以後は `/admin.html` のユーザー管理から、管理者が他ユーザーの管理者権限を付与・解除します。
Discord Bot のサーバー導入は不要です。

ログイン後、利用者は一意の VRChat Display Name を手動登録します。未登録でも閲覧できますが、
Spot と関連情報の登録・編集には VRChat 表示名の登録が必要です。

Development 環境では Discord OAuth を使えない場合があります。そのため、開発環境限定で
以下のサンプルユーザーを用意します。

- `GET /auth/dev/login/admin`: `dev-admin-user` としてログインします。アプリケーション管理者です。
- `GET /auth/dev/login/user`: `dev-general-user` としてログインします。一般ユーザーです。
- `GET /auth/dev/users`: 利用可能な開発用サンプルユーザーを返します。

これらの endpoint は `IWebHostEnvironment.IsDevelopment()` が `true` の場合だけ登録します。

### Discord 認証設定

Discord Developer Portal で Discord application を作成し、OAuth2 redirect URL に
このバックエンドの callback URL を設定します。

```text
http://localhost:5021/auth/discord/callback
```

ローカル開発では、環境変数または user secrets で以下を設定します。実際の client secret は
コミットしません。

```text
Discord__ClientId=<discord-application-client-id>
Discord__ClientSecret=<discord-application-client-secret>
Discord__RedirectUri=http://localhost:5021/auth/discord/callback
Discord__RequiredGuildId=<discord-guild-id>
Discord__InitialAdminUserIds__0=<initial-administrator-discord-user-id>
```

OAuth login URL では以下の scope を要求します。

```text
identify guilds.members.read
```

ログイン URL に `bot` scope は追加しません。Discord サーバー参加確認はユーザーのOAuth access tokenで行い、
アプリケーション管理者権限はVRC Web Map内で管理します。

## Restore / Build / Test

この Codex workspace では、サンドボックスの書き込み制限を避けるため、
NuGet package の書き込み先を `/private/tmp` にしています。

```bash
dotnet restore \
  --source /private/tmp/nuget-local \
  --packages /private/tmp/nuget-packages \
  -p:NuGetAudit=false

dotnet build --no-restore \
  -p:RestorePackagesPath=/private/tmp/nuget-packages
```

テストを実行します。

```bash
dotnet test VrcWebMap.Backend.Tests/VrcWebMap.Backend.Tests.csproj \
  --no-restore \
  -p:RestorePackagesPath=/private/tmp/nuget-packages
```

このサンドボックス外では、通常の `dotnet restore`、`dotnet build`、
`dotnet test` がローカル NuGet cache に書き込める環境であれば利用できます。

## API

Discord 認証 endpoint は Web transport として公開されます。

- `GET /auth/discord/login`
- `GET /auth/discord/callback`
- `POST /auth/logout`
- `GET /auth/me`
- `GET /auth/dev/users` Development 環境のみ
- `GET /auth/dev/login/{userKind}` Development 環境のみ

Spot 管理 endpoint は Kawa UseCase として公開されます。

- `POST /spots/list`
- `POST /spots/get`
- `POST /spots/create`
- `POST /spots/update`
- `POST /spots/delete`

Spot に紐づく情報は、先に `Spot` を作成してから登録します。そのため、以下の
endpoint は request body に `spotId` を必要とします。

- `POST /vrchat-worlds/create`
- `POST /vrchat-worlds/update`
- `POST /vrchat-worlds/delete`
- `POST /place-infos/create`
- `POST /place-infos/update`
- `POST /place-infos/delete`
- `POST /web-links/create`
- `POST /web-links/update`
- `POST /web-links/delete`
- `POST /comments/create`
- `POST /comments/update`
- `POST /comments/delete`

更新・削除 endpoint は `actorUserId` と `actorIsAdmin` を必要とします。
管理者、または対象データを登録したユーザーだけが更新・削除できます。
一覧と詳細閲覧は誰でも可能です。Spot 詳細レスポンスには、Spot 本体に加えて
紐づく VRChat ワールド、場所情報、Web リンク、コメントを含めます。

仮フロントエンドでは、`/auth/me` が `isAdmin: true` を返す場合のみ管理者編集パネルを
表示します。このパネルでは `Spot`、`VRChatWorld`、`PlaceInfo`、`WebLink`、`Comment`
の更新・削除ができます。一般ユーザーは公開登録フォームを利用します。

`Spot` に紐づく `VRChatWorld`、`PlaceInfo`、`WebLink`、`Comment` が残っている場合、
`Spot` の削除は拒否します。先に関連データを削除してください。

地図は位置指定がない場合、デフォルト中心を使います。ブラウザの位置情報が取得できた場合は、
現在地周辺を中心にしますが、その位置情報はサーバーへ保存しません。URL に `?spotId={id}`、
`?spot={id}`、または `#spot={id}` が含まれる場合は、その Spot を読み込んで地図の中心にします。
Spot を選択すると、直リンク用に URL を `?spotId={id}` へ更新します。

Web リンクと VRChat ワールドページ URL は `POST /web-links/preview` で OGP preview を
表示できます。preview は server-side で取得し、`WebLink` や `VRChatWorld` の保存データには
含めません。preview 取得は public な `http` / `https` URL のみに限定し、localhost や
private network address は拒否します。

ポータル出力 endpoint も Kawa UseCase として公開されます。

- `POST /portal/world-data`

`/portal/world-data` は、VRChat ワールドを日本語の地域カテゴリ名ごとにまとめ、
正式な `WorldData.json` 形式で返します。request body は空の JSON object です。
response の `ShowPrivateWorld` は常に `true` で、VRChat 上の public/private release
をどちらも選択可能にし、各 world の `ID` には `wrld_...` 形式の値を出力します。
`ReleaseStatus` は VRChat 上の公開状態であり、WPPLS の閲覧権限ではありません。
地図外ワールド登録を実装するまでは `Roles` と `PermittedRoles` を出力しません。

このポータル出力は、幻会興業さんの `PortalLibrarySystem（WPPLS）` 向けです。
BOOTH の配布ページ:
https://booth.pm/ja/items/6659099

Kawa API catalog と OpenAPI endpoint もアプリケーションに map されています。
開発環境では Swagger UI と ReDoc が有効です。
仮フロントエンドの上部メニューには、開発環境でのみ Swagger へ移動するボタンを表示します。

## 仮フロントエンド

`src/` には React + Leaflet フロントエンドのソースを置いています。PNPM と Vite で
`wwwroot/assets/` へ bundle し、ASP.NET Core の静的ファイル配信で表示します。

- 地図を右クリックすると、その位置の Spot 登録フォームを表示します。
- Spot marker をクリックすると、右ペインに Spot と関連データを表示します。
- ローカル開発で frontend bundle を作る場合は、`pnpm install` の後に `pnpm build` を実行します。
- 依存管理には PNPM を使い、`pnpm-lock.yaml` をコミットします。このプロジェクトでは
  `npm install` / `npm ci` は使いません。
- frontend の entry point は `src/main.tsx` です。今後の UI 改善では、段階的に型付きの React
  component へ寄せていけます。
- Docker build では、ASP.NET Core application を publish する前に PNPM/Vite の frontend build
  を実行します。
- frontend dependency を追加する前に、PNPM の `minimumReleaseAge`、`trustPolicy`、
  `blockExoticSubdeps`、build script の明示許可などの supply-chain 対策を検討します。
- ASP.NET Core は Content Security Policy を返し、script、style、API 接続先を現在の origin
  に制限します。地図 tile 画像は外部 HTTPS tile provider から読み込む可能性があります。

## ライセンス

このプロジェクトは MIT License です。詳細は `LICENSE` を参照してください。

依存関係のライセンスに関する注記は `NOTICE` に記載しています。

## データと商標に関する注意

このリポジトリの MIT License は、このプロジェクトのソースコードに適用されます。

VRChat の名称、識別子、商標、ロゴ、および VRChat または第三者サービスから取得した
データは、このリポジトリのライセンスでは許諾されません。将来的に外部データを
import または再公開する場合は、対象サービスの利用規約とデータライセンスを別途確認してください。

場所情報の営業情報は Markdown 対応の自由記述です。`WebLink` から参照するぐるなび、
食べログ、Retty、X、Instagram などの外部サービスに由来するデータやリンクは、
それぞれの規約の対象となる場合があります。これらの権利と利用条件は、
このソースコードのライセンスとは別に扱ってください。
