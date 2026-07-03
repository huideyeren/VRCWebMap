# WPPLS WorldData JSON 正式仕様対応 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `/portal/world-data` を WPPLS の正式仕様に合わせ、private release のワールドを含む固定設定の WorldData JSON を出力する。

**Architecture:** `Contracts/Portal/GetWorldData.cs` を唯一の公開 JSON contract とし、`GetWorldDataUseCase` が Spot に紐づく全ワールドを地域別に変換する。WPPLS のロール制御は今回の contract から除外し、VRChat 上の release status は既存の `VRChatWorld.IsPrivate` から `ReleaseStatus` へ変換する。

**Tech Stack:** .NET 10、Kawa.Web 0.3.1、System.Text.Json source generation、xUnit 2.9.3、React 19、TypeScript、Vite、PNPM 11

## Global Constraints

- Kawa の contract-first / usecase-first 構成を維持する。
- `GetWorldData.Request` は入力プロパティを持たない。
- JSON の `ShowPrivateWorld` は常に `true` とする。
- public/private release の両方を出力し、`ID` は `wrld_...` 形式の値を保持する。
- `Roles` とカテゴリ・ワールドの `PermittedRoles` は今回の JSON に出力しない。
- `IsPrivate` は VRChat 上の release status だけを表し、WPPLS の閲覧権限には使わない。
- 地図外ワールド、ロール登録、サムネイル動画、release status 自動取得は実装しない。
- コメントと公開ドキュメントは個人プロジェクトの既存方針に合わせて日本語を使う。
- NuGet package は `/private/tmp/nuget-packages` を使い、restore 済みの状態では `--no-restore` を指定する。

---

## File Structure

- `Contracts/Portal/GetWorldData.cs`: 空 request と WPPLS 用 response/category/world/platform contract。
- `UseCases/Portal/GetWorldDataUseCase.cs`: Spot に紐づく全ワールドの地域別変換と固定出力設定。
- `Serialization/AppJsonSerializerContext.cs`: 残る WPPLS contract の source-generation 登録。
- `VrcWebMap.Backend.Tests/UseCases/Portal/GetWorldDataUseCaseTests.cs`: contract、変換、JSON shape、孤立データ除外の回帰テスト。
- `src/main.tsx`: 空 request の送信と release status checkbox の表示文言。
- `Models/VRChatWorld.cs`: `IsPrivate` と `ReleaseStatus` の意味を説明する model documentation。
- `Contracts/VRChatWorlds/CreateVRChatWorld.cs`: 登録時の `IsPrivate` documentation。
- `Contracts/VRChatWorlds/UpdateVRChatWorld.cs`: 更新時の `IsPrivate` documentation。
- `VrcWebMap.Backend.http`: 空 request の手動 API サンプル。
- `README.md`: 英語・日本語の WPPLS 出力仕様と release status の説明。
- `AGENTS.md`: 今後の実装でも守る WPPLS contract と将来ロール規則。

---

### Task 1: Contract、UseCase、JSON shape をテスト先行で更新する

**Files:**
- Modify: `VrcWebMap.Backend.Tests/UseCases/Portal/GetWorldDataUseCaseTests.cs`
- Modify: `Contracts/Portal/GetWorldData.cs`
- Modify: `UseCases/Portal/GetWorldDataUseCase.cs`
- Modify: `Serialization/AppJsonSerializerContext.cs`

**Interfaces:**
- Consumes: `ISpotRepository.List()`、`ISpotRepository.ListWorlds()`、`VRChatWorld.ReleaseStatus`
- Produces: `GetWorldData.Request`（空 record）、`GetWorldData.Response(bool ReverseCategorys, bool ShowPrivateWorld, GetWorldData.Category[] Categorys)`

- [ ] **Step 1: contract と出力 shape の失敗テストを書く**

`VrcWebMap.Backend.Tests/UseCases/Portal/GetWorldDataUseCaseTests.cs` に
`using System.Text.Json;` を追加する。

既存の `ExecuteAsync_ShowPrivateWorldFalse_ExcludesPrivateWorlds` を次に置き換える。

```csharp
[Fact]
public async Task ExecuteAsync_AlwaysIncludesPublicAndPrivateReleaseWorlds()
{
    var spot = new Spot(
        Guid.NewGuid(),
        "owner-user",
        "東京スポット",
        35.681236,
        139.767125,
        AreaCodes.Japan.Tokyo,
        "説明");
    var repository = new FakeSpotRepository(spot);
    repository.AddWorld(CreateWorld(spot.Id, "wrld_public", "公開ワールド"));
    repository.AddWorld(CreateWorld(spot.Id, "wrld_private", "非公開ワールド", isPrivate: true));
    var useCase = new GetWorldDataUseCase(repository);

    var result = await useCase.ExecuteAsync(new GetWorldData.Request());

    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Value);
    Assert.True(result.Value.ShowPrivateWorld);
    var category = Assert.Single(result.Value.Categorys);
    Assert.Collection(
        category.Worlds.OrderBy(world => world.ID, StringComparer.Ordinal),
        world =>
        {
            Assert.Equal("wrld_private", world.ID);
            Assert.Equal("private", world.ReleaseStatus);
        },
        world =>
        {
            Assert.Equal("wrld_public", world.ID);
            Assert.Equal("public", world.ReleaseStatus);
        });
}
```

最初のテスト `ExecuteAsync_GroupsWorldsByJapaneseAreaCategoryName` から
`Assert.Empty(result.Value.Roles);` を削除し、次の contract 検証を追加する。

```csharp
Assert.Empty(typeof(GetWorldData.Request).GetProperties());
```

さらに次の2テストを追加する。

```csharp
[Fact]
public async Task ExecuteAsync_SerializesOnlySupportedWpplsProperties()
{
    var spot = new Spot(
        Guid.NewGuid(),
        "owner-user",
        "東京スポット",
        35.681236,
        139.767125,
        AreaCodes.Japan.Tokyo,
        "説明");
    var repository = new FakeSpotRepository(spot);
    repository.AddWorld(CreateWorld(spot.Id, "wrld_private", "非公開ワールド", isPrivate: true));
    var useCase = new GetWorldDataUseCase(repository);

    var result = await useCase.ExecuteAsync(new GetWorldData.Request());

    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Value);
    using var json = JsonDocument.Parse(JsonSerializer.Serialize(result.Value));
    var root = json.RootElement;
    Assert.True(root.GetProperty("ShowPrivateWorld").GetBoolean());
    Assert.False(root.TryGetProperty("Roles", out _));

    var category = root.GetProperty("Categorys")[0];
    Assert.False(category.TryGetProperty("PermittedRoles", out _));

    var world = category.GetProperty("Worlds")[0];
    Assert.Equal("wrld_private", world.GetProperty("ID").GetString());
    Assert.Equal("private", world.GetProperty("ReleaseStatus").GetString());
    Assert.False(world.TryGetProperty("PermittedRoles", out _));
}

[Fact]
public async Task ExecuteAsync_ExcludesWorldWhoseSpotDoesNotExist()
{
    var repository = new FakeSpotRepository();
    repository.AddWorld(CreateWorld(Guid.NewGuid(), "wrld_orphan", "孤立ワールド"));
    var useCase = new GetWorldDataUseCase(repository);

    var result = await useCase.ExecuteAsync(new GetWorldData.Request());

    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Value);
    Assert.Empty(result.Value.Categorys);
}
```

- [ ] **Step 2: 対象テストを実行して失敗を確認する**

Run:

```bash
dotnet test VrcWebMap.Backend.Tests/VrcWebMap.Backend.Tests.csproj \
  --no-restore \
  -p:RestorePackagesPath=/private/tmp/nuget-packages \
  --filter GetWorldDataUseCaseTests
```

Expected: `ExecuteAsync_GroupsWorldsByJapaneseAreaCategoryName` が
`GetWorldData.Request` に `ShowPrivateWorld` が残っているため失敗し、
`ExecuteAsync_SerializesOnlySupportedWpplsProperties` が `Roles` の存在により失敗する。

- [ ] **Step 3: WPPLS contract を最小構成へ変更する**

`Contracts/Portal/GetWorldData.cs` の `Request` と `Response` を次に変更する。

```csharp
/// <summary>
/// ポータル用ワールドデータ出力の入力です。
/// </summary>
public sealed record Request;

/// <summary>
/// WorldData.json 形式のレスポンスです。
/// </summary>
/// <param name="ReverseCategorys">カテゴリ表示順を反転するかどうかです。</param>
/// <param name="ShowPrivateWorld">private release のワールドを選択可能にするかどうかです。常に <c>true</c> です。</param>
/// <param name="Categorys">地域カテゴリごとのワールド一覧です。</param>
public sealed record Response(
    [property: JsonPropertyName("ReverseCategorys")]
    bool ReverseCategorys,
    [property: JsonPropertyName("ShowPrivateWorld")]
    bool ShowPrivateWorld,
    [property: JsonPropertyName("Categorys")]
    Category[] Categorys);
```

同じファイルから `Role` record を削除する。

`World.ID` の XML documentation を次に置き換える。

```csharp
/// <param name="ID">WPPLS が使用する <c>wrld_...</c> 形式の VRChat world ID です。</param>
```

`World.ReleaseStatus` の XML documentation を次に置き換える。

```csharp
/// <param name="ReleaseStatus">VRChat 上の release status です。<c>public</c> または <c>private</c> を出力します。</param>
```

- [ ] **Step 4: UseCase を固定設定へ変更する**

`UseCases/Portal/GetWorldDataUseCase.cs` の world 抽出から
`ShowPrivateWorld` による filter を削除する。

```csharp
var worlds = spots.ListWorlds()
    .Where(world => spotById.ContainsKey(world.SpotId))
    .ToArray();
```

response 構築を次に置き換える。

```csharp
var response = new GetWorldData.Response(
    ReverseCategorys: false,
    ShowPrivateWorld: true,
    Categorys: categorys);
```

`ExecuteAsync` の request parameter documentation は次に置き換える。

```csharp
/// <param name="request">入力値を持たない出力要求です。</param>
```

- [ ] **Step 5: source-generation 登録から削除済み型を外す**

`Serialization/AppJsonSerializerContext.cs` から次の2行を削除する。

```csharp
[JsonSerializable(typeof(GetWorldData.Role), TypeInfoPropertyName = "GetWorldDataRole")]
[JsonSerializable(typeof(GetWorldData.Role[]), TypeInfoPropertyName = "GetWorldDataRoleArray")]
```

- [ ] **Step 6: 対象テストと build を実行する**

Run:

```bash
dotnet test VrcWebMap.Backend.Tests/VrcWebMap.Backend.Tests.csproj \
  --no-restore \
  -p:RestorePackagesPath=/private/tmp/nuget-packages \
  --filter GetWorldDataUseCaseTests

dotnet build --no-restore \
  -p:RestorePackagesPath=/private/tmp/nuget-packages
```

Expected: `GetWorldDataUseCaseTests` が全件 PASS し、build が error なしで成功する。

- [ ] **Step 7: contract と UseCase をコミットする**

```bash
git add \
  Contracts/Portal/GetWorldData.cs \
  UseCases/Portal/GetWorldDataUseCase.cs \
  Serialization/AppJsonSerializerContext.cs \
  VrcWebMap.Backend.Tests/UseCases/Portal/GetWorldDataUseCaseTests.cs
git commit -m "feat: align WPPLS world data contract"
```

---

### Task 2: フロントエンドを固定出力の意味へ合わせる

**Files:**
- Modify: `src/main.tsx`

**Interfaces:**
- Consumes: `POST /portal/world-data` の空 `GetWorldData.Request`
- Produces: 空 object の request body、VRChat release status を明示する checkbox label

- [ ] **Step 1: 現在の古い request と表示文言を確認する**

Run:

```bash
rg -n 'showPrivateWorld: true|\\), \"Private\"' src/main.tsx
```

Expected: `downloadPortalData` の request と `WorldFields` の checkbox label がそれぞれ1件見つかる。

- [ ] **Step 2: 空 request と明確な label へ変更する**

`downloadPortalData` の API 呼び出しを次に置き換える。

```typescript
const body = await postJson("/portal/world-data", {});
```

`WorldFields` の `isPrivate` checkbox label を次に置き換える。

```typescript
React.createElement(
    "label",
    null,
    React.createElement("input", {
        type: "checkbox",
        checked: value.isPrivate,
        onChange: update("isPrivate")
    }),
    "VRChat上で非公開（private release）"
)
```

- [ ] **Step 3: frontend の型検査と production build を実行する**

Run:

```bash
pnpm typecheck
pnpm build
```

Expected: 両方が exit code 0 で成功し、Vite が `wwwroot/assets/app.js` と
`wwwroot/assets/admin.js` を生成する。`wwwroot/assets/` は `.gitignore` 対象なので
source commit には含めない。

- [ ] **Step 4: 古い request と曖昧な label が消えたことを確認する**

Run:

```bash
rg -n 'showPrivateWorld: true|\\), \"Private\"' src/main.tsx
rg -n 'VRChat上で非公開（private release）' src/main.tsx
```

Expected: 最初の `rg` は一致なし、2つ目は1件一致する。

- [ ] **Step 5: frontend 変更をコミットする**

```bash
git add src/main.tsx
git commit -m "fix: clarify VRChat private release status"
```

---

### Task 3: 公開ドキュメントを更新し、HTTP/OpenAPI surface を検証する

**Files:**
- Modify: `Models/VRChatWorld.cs`
- Modify: `Contracts/VRChatWorlds/CreateVRChatWorld.cs`
- Modify: `Contracts/VRChatWorlds/UpdateVRChatWorld.cs`
- Modify: `VrcWebMap.Backend.http`
- Modify: `README.md`
- Modify: `AGENTS.md`

**Interfaces:**
- Consumes: Task 1 の `GetWorldData.Request` と `GetWorldData.Response`
- Produces: 開発者・運用者向けの正式な WPPLS contract 説明と手動 request sample

- [ ] **Step 1: model と登録 contract の意味を明文化する**

`Models/VRChatWorld.cs` の `IsPrivate` parameter documentation を次に置き換える。

```csharp
/// <param name="IsPrivate">VRChat 上の release status が private の場合は <c>true</c> です。WPPLS の閲覧権限には使用しません。</param>
```

`ReleaseStatus` property documentation を次に置き換える。

```csharp
/// <summary>
/// WPPLS に出力する VRChat 上の release status です。
/// </summary>
```

`Contracts/VRChatWorlds/CreateVRChatWorld.cs` と
`Contracts/VRChatWorlds/UpdateVRChatWorld.cs` の `IsPrivate` parameter documentation を
次に置き換える。

```csharp
/// <param name="IsPrivate">VRChat 上の release status が private の場合は <c>true</c> です。</param>
```

- [ ] **Step 2: HTTP sample を空 request へ変更する**

`VrcWebMap.Backend.http` の `/portal/world-data` request body を次に置き換える。

```json
{}
```

- [ ] **Step 3: AGENTS.md に現在と将来の境界を記録する**

`VRChatWorld.IsPrivate` の既存 bullet を次に置き換える。

```markdown
- `VRChatWorld.IsPrivate` は VRChat 上の release status を表す。`false` を `public`、`true` を `private` としてポータル用 JSON に出力し、WPPLS の閲覧権限には使用しない。
```

ポータル JSON の bullet 群へ次を追加する。

```markdown
- ポータル用 JSON の `ShowPrivateWorld` は常に `true` とし、public/private release の両方を選択可能にする。
- ポータル用 JSON の world `ID` には URL ではなく `wrld_...` 形式の VRChat world ID を出力する。
- 現時点では `Roles` と `PermittedRoles` を出力しない。将来の地図外ワールド登録では、管理者は全体公開またはロール限定、一般ユーザーはロール限定だけを登録できるようにする。
- 将来のロール限定登録では、登録者本人の VRChat Display Name を `RoleName` と `DisplayNames` に使い、対応する `PermittedRoles` から参照する。
```

- [ ] **Step 4: README の英語・日本語説明を正式仕様へ更新する**

英語の `/portal/world-data` 説明を次の内容へ置き換える。

```markdown
`/portal/world-data` returns VRChat worlds grouped by Japanese regional
category names using the official `WorldData.json` shape. The request body is
an empty JSON object. The response always sets `ShowPrivateWorld` to `true`,
keeps both public and private VRChat releases selectable, and writes the raw
`wrld_...` value to each world `ID`. `ReleaseStatus` describes the VRChat
release status; it is not a WPPLS access-control setting. `Roles` and
`PermittedRoles` are intentionally omitted until off-map world registration is
implemented.
```

日本語の `/portal/world-data` 説明を次の内容へ置き換える。

```markdown
`/portal/world-data` は、VRChat ワールドを日本語の地域カテゴリ名ごとにまとめ、
正式な `WorldData.json` 形式で返します。request body は空の JSON object です。
response の `ShowPrivateWorld` は常に `true` で、VRChat 上の public/private release
をどちらも選択可能にし、各 world の `ID` には `wrld_...` 形式の値を出力します。
`ReleaseStatus` は VRChat 上の公開状態であり、WPPLS の閲覧権限ではありません。
地図外ワールド登録を実装するまでは `Roles` と `PermittedRoles` を出力しません。
```

- [ ] **Step 5: 全自動テストと build を実行する**

Run:

```bash
dotnet build --no-restore \
  -p:RestorePackagesPath=/private/tmp/nuget-packages

dotnet test VrcWebMap.Backend.Tests/VrcWebMap.Backend.Tests.csproj \
  --no-restore \
  -p:RestorePackagesPath=/private/tmp/nuget-packages

pnpm typecheck
pnpm build
```

Expected: .NET build、全 xUnit tests、TypeScript typecheck、Vite production build がすべて成功する。

- [ ] **Step 6: Development で JSON、OpenAPI、Swagger、ReDoc を検証する**

Terminal A:

```bash
env ASPNETCORE_ENVIRONMENT=Development \
  Database__Provider=InMemory \
  dotnet run --no-build --urls http://127.0.0.1:5021
```

Terminal B:

```bash
curl -fsS \
  -X POST \
  -H 'Content-Type: application/json' \
  -d '{}' \
  http://127.0.0.1:5021/portal/world-data \
  | tee /tmp/vrcwebmap-world-data.json \
  | jq -e '
      .ShowPrivateWorld == true
      and (has("Roles") | not)
      and ([.Categorys[] | has("PermittedRoles")] | all(. == false))
      and ([.Categorys[].Worlds[] | has("PermittedRoles")] | all(. == false))
      and ([.Categorys[].Worlds[].ID | startswith("wrld_")] | all)
    '

curl -fsS http://127.0.0.1:5021/openapi/v1.json \
  | tee /tmp/vrcwebmap-openapi.json \
  | jq -e '
      ((.components.schemas.GetWorldDataRequest.properties // {}) | length) == 0
      and (.components.schemas.GetWorldDataResponse.properties | has("Roles") | not)
    '

curl -fsS -o /dev/null http://127.0.0.1:5021/openapi/swagger/index.html
curl -fsS -o /dev/null http://127.0.0.1:5021/openapi/redoc/index.html
```

Expected: すべて exit code 0。WorldData JSON は固定値と省略項目を満たし、
OpenAPI request schema は空、response schema に `Roles` がなく、Swagger/ReDoc が
HTTP 200 を返す。

Terminal A の process は `Ctrl-C` で停止する。

- [ ] **Step 7: Production の OpenAPI 公開と UI 非公開を検証する**

Terminal A:

```bash
env ASPNETCORE_ENVIRONMENT=Production \
  Database__Provider=InMemory \
  dotnet run --no-build --urls http://127.0.0.1:5021
```

Terminal B:

```bash
curl -fsS -o /dev/null http://127.0.0.1:5021/openapi/v1.json
test "$(curl -sS -o /dev/null -w '%{http_code}' http://127.0.0.1:5021/openapi/swagger/index.html)" = "404"
test "$(curl -sS -o /dev/null -w '%{http_code}' http://127.0.0.1:5021/openapi/redoc/index.html)" = "404"
```

Expected: OpenAPI JSON は HTTP 200、Swagger UI と ReDoc は HTTP 404。

Terminal A の process は `Ctrl-C` で停止する。

- [ ] **Step 8: documentation と sample をコミットする**

```bash
git add \
  Models/VRChatWorld.cs \
  Contracts/VRChatWorlds/CreateVRChatWorld.cs \
  Contracts/VRChatWorlds/UpdateVRChatWorld.cs \
  VrcWebMap.Backend.http \
  README.md \
  AGENTS.md
git commit -m "docs: clarify WPPLS release status semantics"
```

- [ ] **Step 9: worktree が clean であることを確認する**

Run:

```bash
git status --short
```

Expected: 出力なし。
