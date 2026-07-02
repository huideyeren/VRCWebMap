# エージェント向けプロジェクト指示

このリポジトリで作業するときは、Kawa の contract-first / usecase-first アーキテクチャーに従うこと。

参照元:

- https://raw.githubusercontent.com/huideyeren/Kawa/refs/heads/main/docs/design-principles.md
- https://raw.githubusercontent.com/huideyeren/Kawa/refs/heads/main/docs/rails-like-conventions.md

## アプリケーション概要

このバックエンドは、Leaflet.js を使うフロントエンドが地図上に Spot を表示・管理するための API を提供する。

- フロントエンドは Leaflet.js で地図を表示する想定。
- 将来のフロントエンドは Node.js、React、Leaflet.js で実装する想定。
- フロントエンドの API client は、このバックエンドが出力する OpenAPI YAML から生成する想定。
- `Spot` は地図上の基本地点であり、緯度、経度、名称、説明を持つ。
- 1つの `Spot` には、1つの都道府県コードまたは地域コードが紐づく。
- 地図は位置指定がない場合、デフォルト中心を使う。ブラウザ位置情報が取得できた場合は現在地周辺を中心にするが、現在地はサーバーへ保存しない。
- URL に `?spotId={id}`、`?spot={id}`、または `#spot={id}` が含まれる場合は、その Spot を読み込んで地図の中心にする。Spot 選択時は直リンク用に `?spotId={id}` を URL に反映する。
- 都道府県コードと地域カテゴリの対応は `Models/AreaDefinition.cs` と `Models/AreaDefinitions.cs` に定義する。
- 地域カテゴリは、将来実装する VRChat ワールドポータルシステムのカテゴリとして利用する。
- 三重県は現状 `AreaCategory.Chubu` として扱う。ただし、将来的に `AreaCategory.Kansai` へ移す可能性がある。
- 1つの `Spot` には、0件以上の `VRChatWorld` が紐づく。
- `VRChatWorld` は将来のポータル JSON の world record の基礎モデルとして扱う。
- `VRChatWorld` は登録者を追跡するため、登録者 ID を保持する。
- `VRChatWorld.IsPrivate` は VRChat 上の release status を表す。`false` を `public`、`true` を `private` としてポータル用 JSON に出力し、WPPLS の閲覧権限には使用しない。
- 地図アプリへ `VRChatWorld` を出力するときは、VRChat world ID だけではなく `https://vrchat.com/home/world/{VRChatWorldId}/info` 形式のワールドページ URL を使う。
- `VRChatWorld` のワールドページ URL は `/web-links/preview` で OGP preview を取得して表示できる。ただし preview は保存データには含めない。
- ポータル用 JSON は `Categorys[] -> Worlds[]` の形で出力し、`Category` には `AreaCategory` の日本語表示名を使う。
- ポータル用 JSON の出力 endpoint は `POST /portal/world-data` とする。
- ポータル用 JSON は幻会興業さんの `PortalLibrarySystem（WPPLS）` 向けに出力する。参照: https://booth.pm/ja/items/6659099
- ポータル用 JSON の `ShowPrivateWorld` は常に `true` とし、public/private release の両方を選択可能にする。
- ポータル用 JSON の world `ID` には URL ではなく `wrld_...` 形式の VRChat world ID を出力する。
- 現時点では `Roles` と `PermittedRoles` を出力しない。将来の地図外ワールド登録では、管理者は全体公開またはロール限定、一般ユーザーはロール限定だけを登録できるようにする。
- 将来のロール限定登録では、登録者本人の VRChat Display Name を `RoleName` と `DisplayNames` に使い、対応する `PermittedRoles` から参照する。
- 1つの `Spot` には、0件以上の `PlaceInfo` が紐づく。
- `PlaceInfo` の営業情報は、開店時刻・閉店時刻・定休日の個別プロパティではなく、Markdown 対応の `BusinessInformation` 文字列として保持する。昼営業、夜営業、定休日、臨時休業などを自由に併記できる形にする。
- 1つの `Spot` には、0件以上の `WebLink` が紐づく。
- `WebLink` はサイト名と URL のみを持つ Web サイト情報として扱う。飲食店だけでなく、場所や VRChat ワールドに関連する任意の外部サイトを扱える形にする。
- `WebLink` と `VRChatWorld` の OGP preview は `/web-links/preview` で server-side に取得する一時表示情報として扱い、保存データには含めない。SSRF 対策として public な `http` / `https` URL のみを許可し、localhost/private network address は拒否する。
- 1つの `Spot` には、0件以上の `Comment` が紐づく。
- `VRChatWorld`、`PlaceInfo`、`WebLink`、`Comment` は `Spot` に従属する情報として扱う。
- `VRChatWorld`、`PlaceInfo`、`WebLink`、`Comment` の登録は、先に `Spot` を登録してから行う。登録 request には必ず `SpotId` を含める。
- `VRChatWorld`、`PlaceInfo`、`WebLink`、`Comment` は登録者 ID を保持する。
- `Spot`、`VRChatWorld`、`PlaceInfo`、`WebLink`、`Comment` の登録は誰でも可能とする。
- 一覧と詳細閲覧は誰でも可能とする。
- `Spot` 詳細閲覧では、Spot 本体に加えて紐づく `VRChatWorld`、`PlaceInfo`、`WebLink`、`Comment` をすべて含める。
- `Spot`、`VRChatWorld`、`PlaceInfo`、`WebLink`、`Comment` の更新・削除は、管理者または対象データを登録したユーザーだけに許可する。
- `Spot` に紐づく `VRChatWorld`、`PlaceInfo`、`WebLink`、`Comment` が1件でも存在する場合、`Spot` の削除は `Conflict` として拒否する。関連データを先に削除してから `Spot` を削除する。
- 仮フロントエンドでは、`/auth/me` が `isAdmin: true` を返す場合のみ管理者編集パネルを表示し、各データの更新・削除 UI を提供する。
- 書き込み request に `ActorUserId`、`ActorIsAdmin`、`RegisteredByUserId` を含めない。現在ユーザーは transport adapter が cookie session の Discord ユーザー ID からDBの最新状態を解決し、`ICurrentActorAccessor` 経由で UseCase に渡す。
- ユーザー登録とログインは Discord アカウント連携を想定する。
- アプリケーションを利用できるユーザーは、設定された Discord サーバーに参加している Discord ユーザーに限定する。
- Discord OAuth では `identify` と `guilds.members.read` scope を使い、transport adapter が Discord API で対象サーバー参加を確認する。
- 初期管理者は `Discord:InitialAdminUserIds` に設定した Discord ユーザー ID で確立する。Discord Bot と guild role は管理者判定に使わない。
- 初期管理者以外の管理者権限は、管理者が `/admin.html` のユーザー管理から付与・解除する。
- 利用者は一意の VRChat Display Name を手動登録する。未登録でも閲覧できるが、Spot と関連情報の登録・更新・削除はできない。
- Discord API の確認結果だけを `RegisterDiscordUserUseCase` に渡す。クライアントから自己申告された参加状態を信用しない。
- Development 環境では Discord OAuth を使えない場合があるため、`/auth/dev/login/admin` と `/auth/dev/login/user` で開発用サンプルユーザーを登録・ログインできるようにする。
- 開発用サンプルユーザーは `dev-admin-user`（管理者）と `dev-general-user`（一般ユーザー）とする。これらの endpoint は Development 環境でのみ登録し、production では公開しない。
- Discord ユーザーは将来的な投稿者、編集者、権限管理、監査情報の主体になる想定。ただし、認証・認可を実装するまでは UseCase に transport や認証 provider の型を直接漏らさない。

このアプリケーションでは、Spot を中心に VRChat ワールド、現実側の場所情報、Web サイト情報、自由コメントを地図上で参照できる状態にすることを目的とする。

## 基本方針

- ASP.NET Core は基盤として使い、Kawa はアプリケーションの流れを整理する薄い層として扱う。
- 設計の中心は HTTP エンドポイントではなく、`Request`、`Response`、`UseCase` とする。
- Web、RPC、CLI、Worker は UseCase を外部へ公開するための Transport Adapter として扱う。
- UseCase は HTTP、Minimal API、ASP.NET Core、RPC、CLI、Worker の型や概念に依存させない。
- 予測可能な業務失敗は例外ではなく `KawaResult<T>` と `KawaError` で表現する。
- 依存方向と責務の知識は外側から内側へ流す。Domain や UseCase が Web 層を知ってはいけない。

## 読む順序

Kawa アプリケーションを理解するときは、原則として次の順に読む。

1. `Contracts/`
2. `UseCases/`
3. `Endpoints/`
4. `Transports/`
5. `Models/`
6. `Stores/`

`Contracts/` と `UseCases/` が中心であり、`Endpoints/` と `Transports/` は外側のアダプター層である。

## ディレクトリ責務

- `Contracts/`: UseCase の入力と出力を表す公開境界。transport 固有 DTO ではない。
- `UseCases/`: transport 非依存のアプリケーションフロー。業務目的を完了する最小単位。
- `Endpoints/Web/`: Kawa.Web / Minimal API で UseCase を HTTP に公開する薄い層。
- `Transports/{Transport}/`: 必要になった場合のみ、`KawaResult<T>` を transport 固有の結果へ変換する層。
- `Models/`: 現在のドメイン/データ形状を表す単純な C# record や enum。
- `Stores/`: リポジトリや外部永続化の実装。現在はプロトタイプ用のインメモリ実装。
- `VrcWebMap.Backend.Tests/`: UseCase とモデル中心のテスト。

## データベース方針

- 既定では `InMemorySpotRepository` を使う。
- `Database:Provider` が `PostgreSQL` で `ConnectionStrings:Postgres` が設定されている場合は、EF Core + Npgsql の `PostgreSqlSpotRepository` を使う。
- PostgreSQL は Docker Compose で起動する。
- 現段階では migrations ではなく `EnsureCreated()` で schema を作成する。永続運用へ移る前に migrations へ移行する。
- Redis は任意。現時点では実装に組み込まない。
- Redis を使うのは、`/portal/world-data` など読み取りが重い endpoint で PostgreSQL read や JSON 生成が明確なボトルネックになった場合に限定する。
- Redis を導入する場合は、まず `/portal/world-data` の短時間キャッシュから検討する。`Spot`、`VRChatWorld`、`PlaceInfo`、`WebLink`、`Comment` の更新・削除時に該当キャッシュを確実に無効化する設計を先に用意する。
- Docker Compose の `cache` profile は、将来キャッシュが必要になった場合に Redis を起動するための保留構成として扱う。
- DB実装を追加しても、UseCase は `ISpotRepository` にのみ依存させる。UseCase から EF Core や Npgsql を直接参照しない。

## Contracts の規約

- ファイルは `Contracts/{Area}/{Action}.cs` に置く。
- 1つの UseCase contract は1ファイルにまとめる。
- 型は `public static class {Action}` の中に `Request` と `Response` を定義する。
- `Request` / `Response` は serialization-friendly かつ C# friendly な record / class / enum / DTO にする。
- Contracts は ASP.NET Core、Minimal API、MagicOnion、CLI parser、Worker SDK に依存させない。
- Swagger / ReDoc / HTTP の都合だけで作った DTO を中心にしない。

例:

```csharp
namespace VrcWebMap.Backend.Contracts.Spots;

public static class CreateSpot
{
    public sealed record Request(...);

    public sealed record Response(...);
}
```

## UseCases の規約

- ファイルは `UseCases/{Area}/{Action}UseCase.cs` に置く。
- 型名は `{Action}UseCase` とする。
- `IUseCase<TRequest, TResponse>` を実装する。
- `TRequest` と `TResponse` は `Contracts/` の型を使う。
- 戻り値は `KawaResult<TResponse>` にする。
- UseCase は `HttpContext`、`IResult`、HTTP status code、route、RPC status、CLI exit code、queue ack/nack を知らない。
- UseCase は依存する repository、policy、service、domain function を明示的に合成し、業務フローが読めるようにする。
- DI 設定の中へ業務順序や判断ロジックを隠さない。
- 予測可能な validation、not found、conflict、authorization などは `KawaError` で返す。

## Endpoints の規約

- `Endpoints/Web/` は route 宣言と UseCase 公開だけに限定する。
- Endpoint では業務ロジックを書かない。
- Endpoint では validation rule の本体を書かない。
- Endpoint では error mapping の分岐を直接増やさない。変換は Kawa.Web または transport mapper に寄せる。
- Web endpoint は原則として `MapKawaPost<TUseCase>` など Kawa.Web のマッピングを使う。
- Endpoint 固有の metadata は route name、tags、summary、authorization policy など transport metadata に留める。

## OpenAPI / Swagger / ReDoc

- OpenAPI の中心は endpoint 実装ではなく `Contracts/` の `Request` / `Response`。
- `Endpoints/Web/` は URL、HTTP method、公開名、tag などを宣言する層。
- OpenAPI YAML は Node.js / React / Leaflet.js フロントエンドの API client 生成元として扱う。
- 生成 client の都合だけで UseCase contract を歪めない。必要な場合は frontend 側の生成設定や adapter で吸収する。
- Swagger / ReDoc のためだけに central contract を分岐させない。
- Swagger UI / ReDoc を production で公開する場合は明示的な opt-in とする。

## フロントエンド依存管理

- Node.js / React / Leaflet.js の正式な frontend build を追加する場合は PNPM を使う。
- `npm install` / `npm ci` は使わない。
- `pnpm-lock.yaml` は必ずコミットし、CI と Docker build では lockfile を固定して install する。
- PNPM の supply-chain 対策として、`minimumReleaseAge`、`minimumReleaseAgeStrict`、`trustPolicy`、`blockExoticSubdeps`、`onlyBuiltDependencies` / `ignoredBuiltDependencies` の利用を検討する。
- install script を持つ dependency は supply-chain risk が高いため、必要な package だけを明示的に許可する。
- `pnpm install` など外部 registry へ接続する操作は network 権限が必要になるため、Codex は最初から承認を求めてよい。
- Node.js build を導入するまでの仮 frontend は CDN 依存を最小化し、CSP で `self`、利用中 CDN、地図 tile provider だけに接続先を制限する。
- CDN を使う場合はバージョンを固定し、可能な asset には SRI を付ける。React/Leaflet を正式運用する段階では PNPM build artifact に置き換える。

## 新機能追加の順序

新しい機能は Endpoint から作り始めない。次の順で追加する。

1. `Contracts/{Area}/{Action}.cs` に `Request` と `Response` を定義する。
2. `UseCases/{Area}/{Action}UseCase.cs` に UseCase を実装する。
3. UseCase のテストを書く。
4. 必要な transport の `Endpoints/` に公開口を追加する。
5. transport mapper を追加した場合は mapper のテストを書く。

## 命名規則

- Contract: `Contracts/Spots/CreateSpot.cs`
- Contract type: `CreateSpot.Request` / `CreateSpot.Response`
- UseCase: `UseCases/Spots/CreateSpotUseCase.cs`
- UseCase type: `CreateSpotUseCase`
- Web endpoint: `Endpoints/Web/SpotsEndpoints.cs`
- Transport mapper: `Transports/Web/WebTransportMapper.cs`

既存コードでは `Models/` と `Stores/` が同一プロジェクト内にあるため、現時点ではこの構成を維持する。将来プロジェクト分割する場合も、Contracts を C# friendly な境界として保つ。

## テスト方針

- まず UseCase 単位でテストする。
- HTTP、DI、DB、外部 API を準備しないと検証できない形に業務ロジックを置かない。
- Domain rule や validation は純粋な関数や小さな責務へ切り出し、UseCase から明示的に合成する。
- Endpoint テストは route binding や transport 変換の確認が必要な場合に追加する。

## 禁止事項

- Endpoint や controller に業務ロジックを置く。
- UseCase から ASP.NET Core / Minimal API / HTTP 型を参照する。
- transport 固有 DTO を central contract として昇格させる。
- OpenAPI の都合だけで Contracts を歪める。
- 予測可能な業務失敗を通常フローとして例外で表現する。
- DI 設定へ業務フローを隠す。
- 実際に交換可能性がない場所へ不要な抽象化を追加する。

## Sandbox と権限

- 基本的に Codex は sandbox 内で作業する。
- まず sandbox 内で実行できるコマンドは sandbox 内で実行する。
- `dotnet test` の test host がローカル socket bind を必要とする場合、Docker / Docker Compose を操作する場合、Git index やリモートへ書き込む場合、外部ネットワークへ接続する場合は、sandbox の制限で失敗することがある。
- 上記のように socket、network、Docker、Git 書き込み、外部 API 呼び出しなどで権限が必要な局面では、Codex はユーザーに承認を求めてよい。
- 明らかに sandbox 内で実行できない操作は、sandbox 内での失敗を待たずに最初から承認を求めてよい。
- sandbox 制限で失敗したコマンドを権限付きで再実行する場合は、何のために権限が必要かを短く説明する。

## 実行コマンド

この Codex workspace では NuGet package 書き込みを `/private/tmp` に逃がす。

```bash
dotnet restore \
  --source /private/tmp/nuget-local \
  --packages /private/tmp/nuget-packages \
  -p:NuGetAudit=false

dotnet build --no-restore \
  -p:RestorePackagesPath=/private/tmp/nuget-packages

dotnet test VrcWebMap.Backend.Tests/VrcWebMap.Backend.Tests.csproj \
  --no-restore \
  -p:RestorePackagesPath=/private/tmp/nuget-packages
```
