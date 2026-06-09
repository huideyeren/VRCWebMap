# エージェント向けプロジェクト指示

このリポジトリで作業するときは、Kawa の contract-first / usecase-first アーキテクチャーに従うこと。

参照元:

- https://raw.githubusercontent.com/huideyeren/Kawa/refs/heads/main/docs/design-principles.md
- https://raw.githubusercontent.com/huideyeren/Kawa/refs/heads/main/docs/rails-like-conventions.md

## アプリケーション概要

このバックエンドは、Leaflet.js を使うフロントエンドが地図上に Spot を表示・管理するための API を提供する。

- フロントエンドは Leaflet.js で地図を表示する想定。
- `Spot` は地図上の基本地点であり、緯度、経度、名称、説明を持つ。
- 1つの `Spot` には、1つの都道府県コードまたは地域コードが紐づく。
- 都道府県コードと地域カテゴリの対応は `Models/AreaDefinition.cs` と `Models/AreaDefinitions.cs` に定義する。
- 地域カテゴリは、将来実装する VRChat ワールドポータルシステムのカテゴリとして利用する。
- 三重県は現状 `AreaCategory.Chubu` として扱う。ただし、将来的に `AreaCategory.Kansai` へ移す可能性がある。
- 1つの `Spot` には、0件以上の `VRChatWorld` が紐づく。
- `VRChatWorld` は将来のポータル JSON の world record の基礎モデルとして扱う。
- `VRChatWorld` は登録者を追跡するため、登録者 ID を保持する。
- `VRChatWorld.IsPrivate` は基本 `false` とし、ポータル用 JSON では `false` を `public`、`true` を `private` として扱う。
- 地図アプリへ `VRChatWorld` を出力するときは、VRChat world ID だけではなく `https://vrchat.com/home/world/{VRChatWorldId}/info` 形式のワールドページ URL を使う。
- ポータル用 JSON は `Categorys[] -> Worlds[]` の形で出力し、`Category` には `AreaCategory` の日本語表示名を使う。
- ポータル用 JSON の出力 endpoint は `POST /portal/world-data` とする。
- ポータル用 JSON は幻会興業さんの `PortalLibrarySystem（WPPLS）` 向けに出力する。参照: https://booth.pm/ja/items/6659099
- 1つの `Spot` には、0件以上の `Restaurant` が紐づく。
- 1つの `Spot` には、0件以上の `Comment` が紐づく。
- `VRChatWorld`、`Restaurant`、`Comment` は `Spot` に従属する情報として扱う。
- `VRChatWorld`、`Restaurant`、`Comment` の登録は、先に `Spot` を登録してから行う。登録 request には必ず `SpotId` を含める。
- `VRChatWorld`、`Restaurant`、`Comment` は登録者 ID を保持する。
- `Spot`、`VRChatWorld`、`Restaurant`、`Comment` の登録は誰でも可能とする。
- 一覧と詳細閲覧は誰でも可能とする。
- `Spot` 詳細閲覧では、Spot 本体に加えて紐づく `VRChatWorld`、`Restaurant`、`Comment` をすべて含める。
- `Spot`、`VRChatWorld`、`Restaurant`、`Comment` の更新・削除は、管理者または対象データを登録したユーザーだけに許可する。
- 認証基盤が未実装の間、更新・削除 request には `ActorUserId` と `ActorIsAdmin` を明示的に含める。将来 Discord 認証を実装したら、この値は transport adapter 側で認証情報から組み立てる。
- ユーザー登録とログインは Discord アカウント連携を想定する。
- Discord ユーザーは将来的な投稿者、編集者、権限管理、監査情報の主体になる想定。ただし、認証・認可を実装するまでは UseCase に transport や認証 provider の型を直接漏らさない。

このアプリケーションでは、Spot を中心に VRChat ワールド、現実の飲食店情報、自由コメントを地図上で参照できる状態にすることを目的とする。

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
- Swagger / ReDoc のためだけに central contract を分岐させない。
- Swagger UI / ReDoc を production で公開する場合は明示的な opt-in とする。

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
