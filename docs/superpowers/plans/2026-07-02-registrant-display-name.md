# 登録者VRChat表示名・公開DTO Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Spot系APIとUIから生のDiscordユーザーIDを除き、最新のVRChat Display Nameとserver-derived `CanEdit` を返す。

**Architecture:** 永続modelの `RegisteredByUserId` は認可・監査用に保持し、外部responseはresource別DTOへ変換する。UseCaseごとにDiscordユーザー辞書と現在actorを1回だけ解決する `PublicResourceMapper` を使う。

**Tech Stack:** .NET 10、Kawa.Web、System.Text.Json source generation、xUnit、React 19、TypeScript

## Global Constraints

- 公開DTOに `RegisteredByUserId` を含めない。
- `RegisteredByDisplayName` は最新値、欠損時は `不明なユーザー`。
- `CanEdit` は管理者または所有者だけtrue。未ログインはfalse。
- 更新・削除のserver-side認可は既存のDiscord ID比較を維持する。
- 管理者専用ユーザー管理contractのDiscord IDは維持する。

---

### Task 1: 公開DTOとmapperをテスト先行で追加する

**Files:**
- Create: `Contracts/Spots/SpotData.cs`
- Create: `Contracts/VRChatWorlds/VRChatWorldData.cs`
- Create: `Contracts/PlaceInfos/PlaceInfoData.cs`
- Create: `Contracts/WebLinks/WebLinkData.cs`
- Create: `Contracts/Comments/CommentData.cs`
- Create: `UseCases/Resources/PublicResourceMapper.cs`
- Create: `VrcWebMap.Backend.Tests/UseCases/Resources/PublicResourceMapperTests.cs`

**Interfaces:**
- Produces: `SpotData`、`VRChatWorldData`、`PlaceInfoData`、`WebLinkData`、`CommentData`。

- [ ] **Step 1: mapperの失敗テストを書く**

Test exact cases:

```csharp
[Fact]
public void ToSpot_UsesLatestDisplayNameAndOwnerCanEdit()
{
    var mapper = Mapper(
        users: [User("owner", "Current Name")],
        actor: new CurrentActor("owner", IsAdmin: false, HasVRChatDisplayName: true));

    var dto = mapper.ToSpot(Spot(registeredBy: "owner"));

    Assert.Equal("Current Name", dto.RegisteredByDisplayName);
    Assert.True(dto.CanEdit);
    Assert.DoesNotContain(
        typeof(SpotData).GetProperties(),
        property => property.Name == "RegisteredByUserId");
}

[Theory]
[InlineData(null)]
[InlineData("")]
public void ToSpot_UsesUnknownUserFallback(string? displayName)
{
    var mapper = Mapper(users: [User("owner", displayName)], actor: null);
    Assert.Equal("不明なユーザー", mapper.ToSpot(Spot("owner")).RegisteredByDisplayName);
}
```

Also assert admin true、third-party false、missing DiscordUser fallback and every related DTO.

- [ ] **Step 2: failureを確認する**

```bash
dotnet test VrcWebMap.Backend.Tests/VrcWebMap.Backend.Tests.csproj \
  --no-restore -p:RestorePackagesPath=/private/tmp/nuget-packages \
  --filter PublicResourceMapperTests
```

Expected: DTOとmapper未定義によるcompile failure。

- [ ] **Step 3: resource DTOを追加する**

Use these common tail fields in every DTO:

```csharp
string RegisteredByDisplayName,
bool CanEdit
```

`SpotData` contains `Id, Name, Latitude, Longitude, AreaCode, Description`。
`VRChatWorldData` contains `Id, VRChatWorldId, Name, RecommendedCapacity,
Capacity, Description, PC, Android, IOS, IsPrivate`。
Other DTOs contain their existing public model fields but omit `SpotId` and
`RegisteredByUserId`; parent context is already known from `GetSpot`.

- [ ] **Step 4: mapperを実装する**

Constructor:

```csharp
public sealed class PublicResourceMapper(
    IEnumerable<DiscordUser> users,
    CurrentActor? actor)
```

Build one dictionary:

```csharp
private readonly IReadOnlyDictionary<string, DiscordUser> usersById =
    users.ToDictionary(user => user.DiscordUserId, StringComparer.Ordinal);
```

Shared resolution:

```csharp
public string ResolveDisplayName(string? id) =>
    id is not null &&
    usersById.TryGetValue(id, out var user) &&
    !string.IsNullOrWhiteSpace(user.VRChatDisplayName)
        ? user.VRChatDisplayName
        : "不明なユーザー";

private bool CanEdit(string id) =>
    actor?.IsAdmin == true ||
    string.Equals(actor?.DiscordUserId, id, StringComparison.Ordinal);
```

Add `ToSpot` and one method per related resource.

- [ ] **Step 5: testsを実行してcommitする**

```bash
dotnet test VrcWebMap.Backend.Tests/VrcWebMap.Backend.Tests.csproj \
  --no-restore -p:RestorePackagesPath=/private/tmp/nuget-packages \
  --filter PublicResourceMapperTests
git add Contracts UseCases/Resources VrcWebMap.Backend.Tests/UseCases/Resources
git commit -m "feat: add public resource DTO mapper"
```

---

### Task 2: 全Spot系UseCase responseを公開DTOへ移行する

**Files:**
- Modify: `Contracts/Spots/ListSpots.cs`
- Modify: `Contracts/Spots/GetSpot.cs`
- Modify: Spot create/update/import response contracts
- Modify: VRChatWorld/PlaceInfo/WebLink/Comment create/update response contracts
- Modify: corresponding UseCases
- Modify: `Serialization/AppJsonSerializerContext.cs`
- Modify: relevant xUnit tests

**Interfaces:**
- Consumes: `PublicResourceMapper`。
- Produces: modelを直接公開しないSpot系contract。

- [ ] **Step 1: contract回帰テストを書く**

Add assertions:

```csharp
Assert.DoesNotContain(
    responseJson,
    "\"registeredByUserId\"",
    StringComparison.OrdinalIgnoreCase);
Assert.Contains("\"registeredByDisplayName\"", responseJson);
Assert.Contains("\"canEdit\"", responseJson);
```

Cover list、detail、create、update and KML import response。

- [ ] **Step 2: tests failを確認する**

```bash
dotnet test VrcWebMap.Backend.Tests/VrcWebMap.Backend.Tests.csproj \
  --no-restore -p:RestorePackagesPath=/private/tmp/nuget-packages \
  --filter "ListSpotsUseCaseTests|GetSpotUseCaseTests|CreateVRChatWorldUseCaseTests"
```

Expected: old model response contains raw ID。

- [ ] **Step 3: contractsをDTOへ切り替える**

- `ListSpots.Item`を `SpotData` または同じ公開fieldsへ統一する。
- `GetSpot.Response` uses `SpotData`, `VRChatWorldData[]`,
  `PlaceInfoData[]`, `WebLinkData[]`, `CommentData[]`。
- Create/update/import responses return their matching DTO。

- [ ] **Step 4: UseCasesへusers/currentActorを注入する**

For each response-producing UseCase:

```csharp
var mapper = new PublicResourceMapper(users.List(), currentActor.GetCurrent());
```

Create mapper once per execution, never once per item. Persist model first,
then map it. Existing authorization checks continue using model IDs.

- [ ] **Step 5: source generationとtestsを更新する**

Register each DTO and array in `AppJsonSerializerContext`; remove direct model
registrations only when no endpoint needs them. Update tests to assert
display name and `CanEdit`, not response raw IDs.

- [ ] **Step 6: full backend test and commit**

```bash
dotnet build --no-restore -p:RestorePackagesPath=/private/tmp/nuget-packages
dotnet test VrcWebMap.Backend.Tests/VrcWebMap.Backend.Tests.csproj \
  --no-restore -p:RestorePackagesPath=/private/tmp/nuget-packages
git add Contracts UseCases Serialization VrcWebMap.Backend.Tests
git commit -m "refactor: expose registrant display names"
```

---

### Task 3: UIをdisplay nameとCanEditへ移行する

**Files:**
- Modify: `src/main.tsx`
- Modify: `src/admin.tsx`

**Interfaces:**
- Consumes: `registeredByDisplayName`, `canEdit`。

- [ ] **Step 1: raw ID依存を特定する**

```bash
rg -n "registeredByUserId|discordUserId.*registered|追加ユーザー" src
```

Expected: renderer、admin table、editable filteringに一致。

- [ ] **Step 2: 表示と編集判定を置換する**

Every resource renderer uses:

```typescript
`追加ユーザー: ${item.registeredByDisplayName}`
```

Editable filtering uses:

```typescript
items.filter((item) => item.canEdit)
```

Do not compare `currentUser.discordUserId` with resource fields. Admin user
management continues using Discord IDs.

- [ ] **Step 3: frontend/backend回帰を実行する**

```bash
rg -n "registeredByUserId" src
pnpm typecheck
pnpm build
dotnet test VrcWebMap.Backend.Tests/VrcWebMap.Backend.Tests.csproj \
  --no-restore -p:RestorePackagesPath=/private/tmp/nuget-packages
```

Expected: first command has no resource-display matches、all validation passes。

- [ ] **Step 4: OpenAPIとruntimeを確認する**

Verify Spot schemas omit `RegisteredByUserId` and include
`RegisteredByDisplayName`/`CanEdit`; visually confirm public and admin pages
show VRChat Display Name or `不明なユーザー`.

- [ ] **Step 5: commit**

```bash
git add src/main.tsx src/admin.tsx
git commit -m "fix: show registrant VRChat display names"
git status --short
```

Expected: worktree clean。
