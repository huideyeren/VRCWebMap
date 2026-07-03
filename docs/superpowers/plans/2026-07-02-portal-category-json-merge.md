# PortalCategory・WPPLS JSONマージ Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 地図外ワールドをPersonal/PublicのPortalCategoryとして管理し、ログイン状態に応じたWPPLS Role生成と既存WorldData.jsonへの匿名マージを提供する。

**Architecture:** 座標を持たない `PortalCategory` を通常の `Spot` から分離し、`VRChatWorld` は `SpotId` または `PortalCategoryId` の片方だけを持つ。Portal用CRUDは専用contract/use caseに置き、WorldData生成とJSONマージは認証が任意の独立UseCaseとして構成する。

**Tech Stack:** .NET 10、Kawa.Web 0.3.1、EF Core 10、Npgsql/PostgreSQL 18、System.Text.Json/JsonNode、xUnit 2.9.3、React 19、TypeScript、Vite、PNPM 11

## Global Constraints

- 先に `docs/superpowers/plans/2026-07-02-vrcwebmap-delivery-roadmap.md` の
  Phase 1〜5を完了する。
- Kawaのcontract-first / usecase-first構成を維持し、Endpointへ業務ロジックを置かない。
- `PortalCategory.Visibility`、`RegisteredByUserId`、`OwnerUserId` は作成後変更しない。
- Personalは所有者と管理者、Publicは管理者だけが変更できる。
- 一般ユーザーは複数のPersonalカテゴリを作成できる。
- PortalCategory名は大文字・小文字を区別せず全体で一意とし、地域カテゴリ名も予約する。
- `VRChatWorld` は `SpotId` または `PortalCategoryId` の片方だけを持つ。
- `ReleaseStatus` はVRChat上の公開状態、`PermittedRoles` はWPPLSの閲覧権限として分離する。
- RoleNameとDisplayNamesにはVRChat Display Nameを使う。
- 未ログイン時もWorldData出力とJSONマージを許可するが、PersonalカテゴリとシステムRoleは出力しない。
- マージ元JSONは保存・cache・log出力しない。UTF-8で5 MiBを上限とする。
- NuGet packageは `/private/tmp/nuget-packages` を使用する。

---

## File Structure

### Domain・Storage

- `Models/PortalCategory.cs`: PortalCategoryとvisibility。
- `Models/VRChatWorld.cs`: nullableな2種類の所属先。
- `Models/AreaCategoryDisplayNames.cs`: 先行する地域別一覧計画で追加される地域カテゴリ表示名の共通定義。
- `UseCases/PortalCategories/IPortalCategoryRepository.cs`: PortalCategory永続化境界。
- `Stores/InMemoryPortalCategoryRepository.cs`: InMemory実装。
- `Stores/PostgreSqlPortalCategoryRepository.cs`: PostgreSQL実装。
- `Stores/AppDbContext.cs`: table、FK、index、CHECK制約。
- `Stores/PostgreSqlSchemaInitializer.cs`: 既存volume向け冪等schema補修。

### Contracts・UseCases

- `Contracts/VRChatWorlds/VRChatWorldData.cs`: 所属先を漏らさない共通response DTO。
- `Contracts/PortalCategories/*.cs`: Category CRUD。
- `Contracts/PortalWorlds/*.cs`: Portal所属World CRUD/Move。
- `UseCases/PortalCategories/*.cs`: Category CRUD、名前規則、認可。
- `UseCases/PortalWorlds/*.cs`: Portal World CRUD/Move、認可。
- `UseCases/Portal/GetWorldDataUseCase.cs`: 地域/Public/本人PersonalとRoleの生成。
- `UseCases/Portal/WorldDataJsonMerger.cs`: JsonNodeによる純粋なマージ処理。
- `Contracts/Portal/MergeWorldData.cs`: マージrequest/response。
- `UseCases/Portal/MergeWorldDataUseCase.cs`: サイズ・構造validationとシステムデータ合成。

### Transport・UI

- `Endpoints/Web/PortalCategoryEndpoints.cs`: Portal CRUD route。
- `Endpoints/Web/PortalEndpoints.cs`: WorldDataとmerge route。
- `Serialization/AppJsonSerializerContext.cs`: 新contractのsource generation。
- `Program.cs`: DIとendpoint mapping。
- `src/portal.tsx`: Portal専用React画面。
- `wwwroot/portal.html`: Portal画面entry。
- `vite.config.ts`: portal bundle。

---

### Task 0: 先行する5計画を完了する

**Files:**
- Execute plan: `docs/superpowers/plans/2026-07-02-registrant-display-name.md`
- Execute plan: `docs/superpowers/plans/2026-07-02-regional-spot-list.md`
- Execute plan: `docs/superpowers/plans/2026-07-02-collapsible-spot-forms.md`
- Execute plan: `docs/superpowers/plans/2026-07-02-wppls-world-data.md`
- Execute plan: `docs/superpowers/plans/2026-07-02-postgresql-s3-backup.md`

**Interfaces:**
- Produces: 空の `GetWorldData.Request`、固定 `ShowPrivateWorld: true`、Roleなしの基礎WorldData。

- [ ] **Step 1: UI・contractの先行3計画を順に実行する**

Run the exact tasks in this order:

```text
docs/superpowers/plans/2026-07-02-registrant-display-name.md
docs/superpowers/plans/2026-07-02-regional-spot-list.md
docs/superpowers/plans/2026-07-02-collapsible-spot-forms.md
```

Expected: 公開DTO、共通地域定義、折りたたみUIが実装済みになる。

- [ ] **Step 2: WPPLS基礎計画を実行する**

```text
docs/superpowers/plans/2026-07-02-wppls-world-data.md
```

Expected: 固定 `ShowPrivateWorld: true` と正式JSON shapeが実装済みになる。

- [ ] **Step 3: DB schema変更前にバックアップ計画を実行する**

```text
docs/superpowers/plans/2026-07-02-postgresql-s3-backup.md
```

Expected: backup/list/restoreとMinIO結合試験が完了し、schema変更前に復元経路を使える。

- [ ] **Step 4: 基礎回帰を確認する**

```bash
dotnet test VrcWebMap.Backend.Tests/VrcWebMap.Backend.Tests.csproj \
  --no-restore \
  -p:RestorePackagesPath=/private/tmp/nuget-packages
pnpm typecheck
pnpm build
git status --short
```

Expected: backend tests、typecheck、buildが成功し、`git status --short`は空。

---

### Task 1: PortalCategory model、World所属先、PostgreSQL schemaを追加する

**Files:**
- Create: `Models/PortalCategory.cs`
- Modify: `Models/VRChatWorld.cs`
- Create: `UseCases/PortalCategories/IPortalCategoryRepository.cs`
- Create: `Stores/InMemoryPortalCategoryRepository.cs`
- Create: `Stores/PostgreSqlPortalCategoryRepository.cs`
- Modify: `Stores/AppDbContext.cs`
- Modify: `Stores/PostgreSqlSchemaInitializer.cs`
- Create: `VrcWebMap.Backend.Tests/Models/PortalCategoryTests.cs`
- Create: `VrcWebMap.Backend.Tests/Stores/InMemoryPortalCategoryRepositoryTests.cs`
- Modify: `VrcWebMap.Backend.Tests/Stores/PostgreSqlSchemaInitializerTests.cs`
- Modify: all `new VRChatWorld(...)` call sites

**Interfaces:**
- Produces: `PortalCategory`, `PortalCategoryVisibility`, `IPortalCategoryRepository`。

- [ ] **Step 1: modelとrepositoryの失敗テストを書く**

Create `PortalCategoryTests.cs`:

```csharp
using VrcWebMap.Backend.Models;

namespace VrcWebMap.Backend.Tests.Models;

public sealed class PortalCategoryTests
{
    [Fact]
    public void PersonalCategory_KeepsOwnerAndNormalizedName()
    {
        var category = new PortalCategory(
            Guid.NewGuid(),
            "creator",
            "owner",
            "個人用",
            "個人用".ToUpperInvariant(),
            PortalCategoryVisibility.Personal);

        Assert.Equal("owner", category.OwnerUserId);
        Assert.Equal(PortalCategoryVisibility.Personal, category.Visibility);
    }
}
```

Create `InMemoryPortalCategoryRepositoryTests.cs` with:

```csharp
[Fact]
public void UpsertAndTryGet_RoundTripsCategory()
{
    var repository = new InMemoryPortalCategoryRepository();
    var category = new PortalCategory(
        Guid.NewGuid(), "creator", "owner", "個人用", "個人用",
        PortalCategoryVisibility.Personal);

    repository.Upsert(category);

    Assert.True(repository.TryGet(category.Id, out var loaded));
    Assert.Equal(category, loaded);
    Assert.True(repository.TryGetByNormalizedName("個人用", out _));
}
```

Add a schema test asserting:

```csharp
var sql = PostgreSqlSchemaInitializer.EnsurePortalCategorySchemaSql;
Assert.Contains("CREATE TABLE IF NOT EXISTS \"PortalCategories\"", sql);
Assert.Contains("ALTER COLUMN \"SpotId\" DROP NOT NULL", sql);
Assert.Contains("\"PortalCategoryId\" uuid", sql);
Assert.Contains("CHECK", sql);
Assert.Contains("\"SpotId\" IS NOT NULL", sql);
Assert.Contains("\"PortalCategoryId\" IS NOT NULL", sql);
Assert.Contains("ON DELETE RESTRICT", sql);
Assert.Contains("\"IX_PortalCategories_NormalizedName\"", sql);
```

- [ ] **Step 2: test failureを確認する**

```bash
dotnet test VrcWebMap.Backend.Tests/VrcWebMap.Backend.Tests.csproj \
  --no-restore \
  -p:RestorePackagesPath=/private/tmp/nuget-packages \
  --filter "PortalCategoryTests|InMemoryPortalCategoryRepositoryTests|PostgreSqlSchemaInitializerTests"
```

Expected: 新しい型とrepositoryが存在しないためcompile failure。

- [ ] **Step 3: domain型を追加する**

`Models/PortalCategory.cs`:

```csharp
namespace VrcWebMap.Backend.Models;

public enum PortalCategoryVisibility
{
    Personal,
    Public
}

public sealed record PortalCategory(
    Guid Id,
    string RegisteredByUserId,
    string? OwnerUserId,
    string Name,
    string NormalizedName,
    PortalCategoryVisibility Visibility);
```

Replace the positional ownership fields in `VRChatWorld` with:

```csharp
Guid? SpotId,
Guid? PortalCategoryId,
string RegisteredByUserId,
```

Update normal-world constructor calls by inserting `PortalCategoryId: null` after `SpotId`.
Portal-world constructors added later use `SpotId: null` and a non-null `PortalCategoryId`.

- [ ] **Step 4: repository境界と実装を追加する**

`IPortalCategoryRepository.cs`:

```csharp
using System.Diagnostics.CodeAnalysis;
using VrcWebMap.Backend.Models;

namespace VrcWebMap.Backend.UseCases.PortalCategories;

public interface IPortalCategoryRepository
{
    PortalCategory[] List();
    bool TryGet(Guid id, [NotNullWhen(true)] out PortalCategory? category);
    bool TryGetByNormalizedName(string normalizedName, [NotNullWhen(true)] out PortalCategory? category);
    void Upsert(PortalCategory category);
    bool Delete(Guid id);
}
```

Implement both repositories with deterministic `Name` ordering. InMemory uses
`ConcurrentDictionary<Guid, PortalCategory>`; PostgreSQL uses
`db.PortalCategories.AsNoTracking()` and the same add/update pattern as
`PostgreSqlSpotRepository`.

- [ ] **Step 5: EF modelと冪等schema補修を追加する**

Add `DbSet<PortalCategory> PortalCategories` and configure:

```csharp
modelBuilder.Entity<PortalCategory>(entity =>
{
    entity.HasKey(category => category.Id);
    entity.Property(category => category.RegisteredByUserId).HasMaxLength(128).IsRequired();
    entity.Property(category => category.OwnerUserId).HasMaxLength(128);
    entity.Property(category => category.Name).HasMaxLength(200).IsRequired();
    entity.Property(category => category.NormalizedName).HasMaxLength(200).IsRequired();
    entity.HasIndex(category => category.NormalizedName).IsUnique();
    entity.ToTable(table => table.HasCheckConstraint(
        "CK_PortalCategories_OwnerMatchesVisibility",
        "(\"Visibility\" = 0 AND \"OwnerUserId\" IS NOT NULL) OR " +
        "(\"Visibility\" = 1 AND \"OwnerUserId\" IS NULL)"));
});
```

Configure the two optional `VRChatWorld` foreign keys with Spot cascade and
PortalCategory restrict, and add this check:

```csharp
entity.ToTable(table => table.HasCheckConstraint(
    "CK_VRChatWorlds_ExactlyOneParent",
    "(\"SpotId\" IS NOT NULL AND \"PortalCategoryId\" IS NULL) OR " +
    "(\"SpotId\" IS NULL AND \"PortalCategoryId\" IS NOT NULL)"));
```

Add `EnsurePortalCategorySchemaSql` containing idempotent PostgreSQL DDL for the
table, nullable `SpotId`, `PortalCategoryId`, FK, checks, and unique index.
The SQL must use this shape:

```sql
CREATE TABLE IF NOT EXISTS "PortalCategories" (
    "Id" uuid NOT NULL,
    "RegisteredByUserId" character varying(128) NOT NULL,
    "OwnerUserId" character varying(128),
    "Name" character varying(200) NOT NULL,
    "NormalizedName" character varying(200) NOT NULL,
    "Visibility" integer NOT NULL,
    CONSTRAINT "PK_PortalCategories" PRIMARY KEY ("Id")
);

ALTER TABLE "VRChatWorlds"
    ALTER COLUMN "SpotId" DROP NOT NULL,
    ADD COLUMN IF NOT EXISTS "PortalCategoryId" uuid;

CREATE UNIQUE INDEX IF NOT EXISTS "IX_PortalCategories_NormalizedName"
    ON "PortalCategories" ("NormalizedName");

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'FK_VRChatWorlds_PortalCategories_PortalCategoryId'
    ) THEN
        ALTER TABLE "VRChatWorlds"
            ADD CONSTRAINT "FK_VRChatWorlds_PortalCategories_PortalCategoryId"
            FOREIGN KEY ("PortalCategoryId") REFERENCES "PortalCategories" ("Id")
            ON DELETE RESTRICT;
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'CK_VRChatWorlds_ExactlyOneParent'
    ) THEN
        ALTER TABLE "VRChatWorlds"
            ADD CONSTRAINT "CK_VRChatWorlds_ExactlyOneParent"
            CHECK (
                ("SpotId" IS NOT NULL AND "PortalCategoryId" IS NULL) OR
                ("SpotId" IS NULL AND "PortalCategoryId" IS NOT NULL)
            );
    END IF;
END $$;
```

Add the analogous idempotent
`CK_PortalCategories_OwnerMatchesVisibility` constraint. Call this SQL from
`PostgreSqlSchemaInitializer.EnsureCreated` before search-index creation.

- [ ] **Step 6: compile errors from nullable SpotIdを修正する**

Use:

```bash
rg -n "new VRChatWorld|\\.SpotId" UseCases Stores VrcWebMap.Backend.Tests
```

For normal world ID sets use:

```csharp
var worldSpotIds = spots.ListWorlds()
    .Where(world => world.SpotId.HasValue)
    .Select(world => world.SpotId!.Value)
    .ToHashSet();
```

For normal related-data filters use `world.SpotId == spotId`; this remains valid.
Every normal-world test fixture must pass `PortalCategoryId: null`.

- [ ] **Step 7: testsとbuildを実行する**

```bash
dotnet test VrcWebMap.Backend.Tests/VrcWebMap.Backend.Tests.csproj \
  --no-restore \
  -p:RestorePackagesPath=/private/tmp/nuget-packages \
  --filter "PortalCategoryTests|InMemoryPortalCategoryRepositoryTests|PostgreSqlSchemaInitializerTests"
dotnet build --no-restore -p:RestorePackagesPath=/private/tmp/nuget-packages
```

Expected: 対象tests PASS、build成功。

- [ ] **Step 8: commit**

```bash
git add Models UseCases/PortalCategories Stores VrcWebMap.Backend.Tests
git commit -m "feat: add portal category persistence"
```

---

### Task 2: PortalCategory CRUDと認可を実装する

**Files:**
- Create: `Contracts/PortalCategories/PortalCategoryData.cs`
- Create: `Contracts/PortalCategories/CreatePortalCategory.cs`
- Create: `Contracts/PortalCategories/ListPortalCategories.cs`
- Create: `Contracts/PortalCategories/UpdatePortalCategory.cs`
- Create: `Contracts/PortalCategories/DeletePortalCategory.cs`
- Create: `UseCases/PortalCategories/PortalCategoryName.cs`
- Create: `UseCases/PortalCategories/PortalCategoryAuthorization.cs`
- Create: `UseCases/PortalCategories/*UseCase.cs`
- Create: `VrcWebMap.Backend.Tests/UseCases/PortalCategories/*Tests.cs`

**Interfaces:**
- Produces: Category CRUD contractsとUseCases。

- [ ] **Step 1: 名前規則と認可の失敗テストを書く**

Create tests covering these exact cases:

```csharp
[Theory]
[InlineData("関東")]
[InlineData("  ")]
public void Validate_RejectsReservedOrBlankName(string name)
{
    Assert.NotNull(PortalCategoryName.Validate(name));
}

[Fact]
public void CanMutate_PersonalOwnerAndAdminOnly()
{
    var category = Personal(owner: "owner");
    Assert.True(PortalCategoryAuthorization.CanMutate(category, Actor("owner")));
    Assert.True(PortalCategoryAuthorization.CanMutate(category, Actor("admin", isAdmin: true)));
    Assert.False(PortalCategoryAuthorization.CanMutate(category, Actor("other")));
}
```

Create UseCase tests for:

- general user creates Personal owned by self;
- admin creates Public with null owner;
- admin creates Personal for a display-name-registered user;
- general user cannot create Public;
- duplicate normalized name returns `Conflict`;
- update changes only Name;
- delete with portal worlds returns `Conflict`;
- owner/admin permissions;
- list returns Public+own Personal for general, all for admin, Public for anonymous.

- [ ] **Step 2: tests failを確認する**

```bash
dotnet test VrcWebMap.Backend.Tests/VrcWebMap.Backend.Tests.csproj \
  --no-restore \
  -p:RestorePackagesPath=/private/tmp/nuget-packages \
  --filter PortalCategories
```

Expected: contract、policy、UseCase未定義によるcompile failure。

- [ ] **Step 3: contractsを追加する**

Use this shared response:

```csharp
public sealed record PortalCategoryData(
    Guid Id,
    string Name,
    PortalCategoryVisibility Visibility,
    string RegisteredByDisplayName,
    string? OwnerDisplayName,
    bool CanEdit,
    VRChatWorldData[] Worlds);
```

Public response does not expose `RegisteredByUserId` or `OwnerUserId`.
Resolve both display names with the existing `PublicResourceMapper` user
dictionary. Public categories set `OwnerDisplayName=null`; Personal categories
resolve `OwnerUserId`. `CanEdit` comes from
`PortalCategoryAuthorization.CanMutate`.

Create requests:

```csharp
public static class CreatePortalCategory
{
    public sealed record Request(
        string Name,
        PortalCategoryVisibility Visibility,
        string? OwnerUserId = null);
    public sealed record Response(PortalCategoryData Category);
}

public static class ListPortalCategories
{
    public sealed record Request;
    public sealed record Response(PortalCategoryData[] Categories);
}

public static class UpdatePortalCategory
{
    public sealed record Request(Guid Id, string Name);
    public sealed record Response(PortalCategoryData Category);
}

public static class DeletePortalCategory
{
    public sealed record Request(Guid Id);
    public sealed record Response(Guid Id);
}
```

- [ ] **Step 4: pure policyを実装する**

`PortalCategoryName.Normalize` uses `Trim().ToUpperInvariant()`.
`Validate` rejects blank names and
`AreaCategoryDisplayNames.All.Contains(trimmed, StringComparer.OrdinalIgnoreCase)`.

`PortalCategoryAuthorization.CanMutate`:

```csharp
public static bool CanMutate(PortalCategory category, CurrentActor actor) =>
    actor.IsAdmin ||
    (category.Visibility == PortalCategoryVisibility.Personal &&
     string.Equals(category.OwnerUserId, actor.DiscordUserId, StringComparison.Ordinal));
```

- [ ] **Step 5: CRUD UseCasesを実装する**

All writes call `CurrentActorPolicy.RequireWriter`.

Create rules:

```csharp
if (request.Visibility == PortalCategoryVisibility.Public && !actor.IsAdmin)
    return Forbidden;

var ownerUserId = request.Visibility == PortalCategoryVisibility.Public
    ? null
    : actor.IsAdmin && !string.IsNullOrWhiteSpace(request.OwnerUserId)
        ? request.OwnerUserId
        : actor.DiscordUserId;
```

For Personal, resolve `ownerUserId` through `IDiscordUserRepository` and reject
when the user or VRChat Display Name is missing. Normalize the name and return
`Conflict` if repository already contains it.

Update loads the existing category, checks `CanMutate`, validates only the new
name, and preserves registered user, owner, and visibility.

Delete checks `CanMutate`, then rejects when any
`spots.ListWorlds().Any(world => world.PortalCategoryId == category.Id)`.

List applies:

```csharp
var actor = currentActor.GetCurrent();
var visible = categories.List().Where(category =>
    category.Visibility == PortalCategoryVisibility.Public ||
    actor?.IsAdmin == true ||
    string.Equals(category.OwnerUserId, actor?.DiscordUserId, StringComparison.Ordinal));
```

Map every visible category to `PortalCategoryData`, including its Portal worlds
sorted by name. Anonymous users receive Public categories with `CanEdit=false`.

- [ ] **Step 6: testsを実行する**

```bash
dotnet test VrcWebMap.Backend.Tests/VrcWebMap.Backend.Tests.csproj \
  --no-restore \
  -p:RestorePackagesPath=/private/tmp/nuget-packages \
  --filter PortalCategories
```

Expected: 全PortalCategory tests PASS。

- [ ] **Step 7: commit**

```bash
git add Contracts/PortalCategories UseCases/PortalCategories VrcWebMap.Backend.Tests/UseCases/PortalCategories
git commit -m "feat: add portal category use cases"
```

---

### Task 3: Portal所属World CRUD・Moveとresponse DTOを実装する

**Files:**
- Modify: `Contracts/VRChatWorlds/VRChatWorldData.cs`
- Modify: normal world and GetSpot response mappers only where nullable parent handling requires it
- Create: `Contracts/PortalWorlds/*.cs`
- Create: `UseCases/PortalWorlds/*.cs`
- Create: `VrcWebMap.Backend.Tests/UseCases/PortalWorlds/*Tests.cs`
- Modify: existing normal-world tests

**Interfaces:**
- Consumes: 登録者表示名計画で追加済みの、所属先を公開しない `VRChatWorldData` と `PublicResourceMapper`。
- Produces: Portal World CRUD/Move。

- [ ] **Step 1: response DTOとPortal認可の失敗テストを書く**

Write tests asserting:

- create stores `SpotId == null` and requested `PortalCategoryId`;
- Personal owner/admin can create/update/delete;
- other general user is Forbidden;
- Public mutation by general user is Forbidden;
- Move requires access to source and destination;
- general user can move only between own Personal categories;
- normal-world update/delete rejects Portal worlds with `NotFound`;
- returned `VRChatWorldData` has no `SpotId` or `PortalCategoryId` property。
- Portal world `RegisteredByDisplayName` resolves from its registrant, while
  `CanEdit` follows the parent PortalCategory permission。

- [ ] **Step 2: failureを確認する**

```bash
dotnet test VrcWebMap.Backend.Tests/VrcWebMap.Backend.Tests.csproj \
  --no-restore \
  -p:RestorePackagesPath=/private/tmp/nuget-packages \
  --filter "PortalWorlds|VRChatWorlds|GetSpot"
```

Expected: Portal contract/UseCase未定義またはDTO shape不一致でfailure。

- [ ] **Step 3: normal response mapperへnullable parent対応を追加する**

登録者表示名計画で追加済みの `PublicResourceMapper.ToVRChatWorldData` を維持する。
Normal-world UseCases must require `existing.SpotId.HasValue` and
`existing.PortalCategoryId is null`; response DTOへparent IDは追加しない。

Add an overload:

```csharp
public VRChatWorldData ToVRChatWorldData(VRChatWorld world, bool canEdit) =>
    ToVRChatWorldDataCore(world, canEdit);
```

Normal responses use the existing registrant-derived overload. Portal responses
pass `PortalCategoryAuthorization.CanMutate(category, actor)` so category
ownership controls UI editing without changing the persisted registrant.

- [ ] **Step 4: Portal World contractsを追加する**

Create/Update request fields mirror the existing world fields, but Create uses
`PortalCategoryId`; Update uses world `Id`. Move is:

```csharp
public static class MovePortalWorld
{
    public sealed record Request(Guid Id, Guid DestinationPortalCategoryId);
    public sealed record Response(VRChatWorldData World);
}
```

Delete returns the deleted world ID. No contract accepts actor IDs or
`RegisteredByUserId`.

- [ ] **Step 5: Portal World UseCasesを実装する**

For every mutation:

1. call `CurrentActorPolicy.RequireWriter`;
2. load the category;
3. call `PortalCategoryAuthorization.CanMutate`;
4. validate world ID/name/description and non-negative capacities;
5. save through `ISpotRepository.UpsertWorld`.

Create uses:

```csharp
var world = new VRChatWorld(
    Guid.NewGuid(),
    SpotId: null,
    PortalCategoryId: category.Id,
    RegisteredByUserId: actor.DiscordUserId,
    request.VRChatWorldId.Trim(),
    request.Name.Trim(),
    request.RecommendedCapacity,
    request.Capacity,
    request.Description.Trim(),
    request.PC,
    request.Android,
    request.IOS,
    request.IsPrivate);
```

Update preserves both parent IDs and original registrant. Delete only accepts
worlds with `PortalCategoryId.HasValue`. Move checks both categories, then uses:

```csharp
var moved = existing with { PortalCategoryId = destination.Id };
```

- [ ] **Step 6: testsと全buildを実行する**

```bash
dotnet test VrcWebMap.Backend.Tests/VrcWebMap.Backend.Tests.csproj \
  --no-restore \
  -p:RestorePackagesPath=/private/tmp/nuget-packages \
  --filter "PortalWorlds|VRChatWorlds|GetSpot"
dotnet build --no-restore -p:RestorePackagesPath=/private/tmp/nuget-packages
```

Expected: 対象tests PASS、build成功。

- [ ] **Step 7: commit**

```bash
git add Contracts/VRChatWorlds Contracts/PortalWorlds Contracts/Spots \
  UseCases/VRChatWorlds UseCases/PortalWorlds UseCases/Spots \
  VrcWebMap.Backend.Tests
git commit -m "feat: manage portal category worlds"
```

---

### Task 4: ログイン状態別WorldDataとRole生成を実装する

**Files:**
- Modify: `Contracts/Portal/GetWorldData.cs`
- Modify: `UseCases/Portal/GetWorldDataUseCase.cs`
- Modify: `VrcWebMap.Backend.Tests/UseCases/Portal/GetWorldDataUseCaseTests.cs`

**Interfaces:**
- Produces: nullable Roles/PermittedRolesを必要時だけ出力するWorldData。

- [ ] **Step 1: 3種類の出力scopeを失敗テストにする**

Add tests:

```text
ExecuteAsync_Anonymous_IncludesRegionsAndPublicOnly
ExecuteAsync_GeneralUser_IncludesOwnPersonalAndSingleRole
ExecuteAsync_Admin_DoesNotIncludeOtherUsersPersonal
ExecuteAsync_EmptyPortalCategory_IsStillEmitted
ExecuteAsync_PersonalCategory_UsesLatestOwnerDisplayName
```

Each test must assert serialized JSON:

- null Roles property is absent;
- Public category has no `PermittedRoles`;
- Personal category has `PermittedRoles: ["Display Name"]`;
- two Personal categories owned by one user produce one Role;
- world objects have no `PermittedRoles`;
- order is regions, Public name order, own Personal name order.

- [ ] **Step 2: tests failを確認する**

```bash
dotnet test VrcWebMap.Backend.Tests/VrcWebMap.Backend.Tests.csproj \
  --no-restore \
  -p:RestorePackagesPath=/private/tmp/nuget-packages \
  --filter GetWorldDataUseCaseTests
```

Expected: PortalCategoryとRoleが出力されないためfailure。

- [ ] **Step 3: optional WPPLS fieldsをcontractへ追加する**

Add to `Response`:

```csharp
[property: JsonPropertyName("Roles")]
[property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
GetWorldData.Role[]? Roles
```

Add to `Category`:

```csharp
[property: JsonPropertyName("PermittedRoles")]
[property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
string[]? PermittedRoles = null
```

Add `Role`:

```csharp
public sealed record Role(
    [property: JsonPropertyName("RoleName")] string RoleName,
    [property: JsonPropertyName("DisplayNames")] string[] DisplayNames);
```

World does not receive a `PermittedRoles` property.

- [ ] **Step 4: GetWorldDataUseCaseを拡張する**

Inject:

```csharp
IPortalCategoryRepository portalCategories,
IDiscordUserRepository users,
ICurrentActorAccessor currentActor
```

Build regional categories first. Select Public categories for everyone. If an
actor exists, select only Personal categories whose `OwnerUserId` equals
`actor.DiscordUserId`; never select every Personal category for admins.

Resolve the current user's latest `VRChatDisplayName`. Generate one role only
when at least one own Personal category exists:

```csharp
var role = new GetWorldData.Role(displayName, [displayName]);
```

Create every Portal category even when it has zero worlds. Public uses null
PermittedRoles; Personal uses `[displayName]`. Return Roles null when no system
role exists.

- [ ] **Step 5: testsを実行する**

```bash
dotnet test VrcWebMap.Backend.Tests/VrcWebMap.Backend.Tests.csproj \
  --no-restore \
  -p:RestorePackagesPath=/private/tmp/nuget-packages \
  --filter GetWorldDataUseCaseTests
```

Expected: 全GetWorldData tests PASS。

- [ ] **Step 6: commit**

```bash
git add Contracts/Portal/GetWorldData.cs UseCases/Portal/GetWorldDataUseCase.cs \
  VrcWebMap.Backend.Tests/UseCases/Portal/GetWorldDataUseCaseTests.cs
git commit -m "feat: export personal and public portal categories"
```

---

### Task 5: JsonNodeベースの匿名JSONマージを実装する

**Files:**
- Create: `Contracts/Portal/MergeWorldData.cs`
- Create: `UseCases/Portal/WorldDataJsonMerger.cs`
- Create: `UseCases/Portal/MergeWorldDataUseCase.cs`
- Create: `VrcWebMap.Backend.Tests/UseCases/Portal/WorldDataJsonMergerTests.cs`
- Create: `VrcWebMap.Backend.Tests/UseCases/Portal/MergeWorldDataUseCaseTests.cs`

**Interfaces:**
- Produces: `MergeWorldData.Request(string ExistingJson)`、
  `MergeWorldData.Response(string MergedJson)`。

- [ ] **Step 1: merge規則の失敗テストを書く**

Create tests for:

```text
Merge_PreservesUnknownRootAndNestedProperties
Merge_PreservesReverseCategorysAndForcesShowPrivateWorldTrue
Merge_AppendsSystemCategoriesAndRoles
Merge_CreatesRolesArrayOnlyWhenSystemRolesExist
Merge_ReusesIdenticalRole
Merge_RejectsConflictingRole
Merge_RejectsDuplicateCategoryIgnoringCaseAndWhitespace
Merge_RejectsInvalidRootCategorysOrRoles
ExecuteAsync_AllowsAnonymousAndExcludesPersonalData
ExecuteAsync_RejectsUtf8PayloadOverFiveMiB
```

The unknown-property test uses:

```json
{
  "VendorExtension": {"Keep": true},
  "Categorys": [{
    "Category": "既存",
    "Worlds": [],
    "UnknownCategoryValue": 123
  }]
}
```

and asserts both unknown properties survive.

- [ ] **Step 2: tests failを確認する**

```bash
dotnet test VrcWebMap.Backend.Tests/VrcWebMap.Backend.Tests.csproj \
  --no-restore \
  -p:RestorePackagesPath=/private/tmp/nuget-packages \
  --filter "WorldDataJsonMergerTests|MergeWorldDataUseCaseTests"
```

Expected: contract、merger、UseCase未定義によるcompile failure。

- [ ] **Step 3: merge contractとpure mergerを追加する**

Contract:

```csharp
public static class MergeWorldData
{
    public sealed record Request(string ExistingJson);
    public sealed record Response(string MergedJson);
}
```

`WorldDataJsonMerger.Merge`:

1. `JsonNode.Parse` and require `JsonObject`;
2. require `Categorys` as `JsonArray`;
3. if `Roles` exists, require `JsonArray`;
4. serialize system response and parse it as a second `JsonObject`;
5. set `ShowPrivateWorld = true`;
6. preserve `ReverseCategorys` without creating it;
7. reject duplicate trimmed category names with `OrdinalIgnoreCase`;
8. append cloned system Category nodes;
9. merge roles by exact trimmed RoleName and exact DisplayNames set;
10. return indented JSON.

Return an internal result:

```csharp
internal sealed record MergeResult(string? Json, string? ErrorMessage)
{
    public bool IsSuccess => ErrorMessage is null;
}
```

Use `DeepClone()` before moving nodes between parents.

- [ ] **Step 4: merge UseCaseを追加する**

Inject `GetWorldDataUseCase`. Validate:

```csharp
if (string.IsNullOrWhiteSpace(request.ExistingJson))
    return Validation("既存のWorldData.jsonを指定してください。");

if (Encoding.UTF8.GetByteCount(request.ExistingJson) > 5 * 1024 * 1024)
    return Validation("WorldData.jsonは5 MiB以下にしてください。");
```

Call `GetWorldDataUseCase.ExecuteAsync(new GetWorldData.Request())`; this
naturally applies anonymous/current-user scope. Pass the response to the pure
merger. Convert every parser/shape/collision error to `KawaErrorKind.Validation`
without returning stack traces or source JSON.

- [ ] **Step 5: testsを実行する**

```bash
dotnet test VrcWebMap.Backend.Tests/VrcWebMap.Backend.Tests.csproj \
  --no-restore \
  -p:RestorePackagesPath=/private/tmp/nuget-packages \
  --filter "WorldDataJsonMergerTests|MergeWorldDataUseCaseTests"
```

Expected: 全merge tests PASS。

- [ ] **Step 6: commit**

```bash
git add Contracts/Portal UseCases/Portal VrcWebMap.Backend.Tests/UseCases/Portal
git commit -m "feat: merge existing WPPLS world data"
```

---

### Task 6: DI、Endpoints、source generation、OpenAPIを配線する

**Files:**
- Create: `Endpoints/Web/PortalCategoryEndpoints.cs`
- Modify: `Endpoints/Web/PortalEndpoints.cs`
- Modify: `Serialization/AppJsonSerializerContext.cs`
- Modify: `Program.cs`
- Modify: `VrcWebMap.Backend.http`

**Interfaces:**
- Produces: `/portal-categories/*`、`/portal-worlds/*`、
  `/portal/world-data/merge`。

- [ ] **Step 1: exact routesを追加する**

Map:

```text
POST /portal-categories/list
POST /portal-categories/create
POST /portal-categories/update
POST /portal-categories/delete
POST /portal-worlds/create
POST /portal-worlds/update
POST /portal-worlds/delete
POST /portal-worlds/move
POST /portal/world-data
POST /portal/world-data/merge
```

Only list、world-data、merge are anonymous. All mutations call
`.RequireAuthorization()`.

- [ ] **Step 2: DIとwrite-route判定を追加する**

Register `IPortalCategoryRepository` next to the current database provider:

```csharp
builder.Services.AddScoped<IPortalCategoryRepository, PostgreSqlPortalCategoryRepository>();
```

or singleton `InMemoryPortalCategoryRepository`.

Register the concrete `GetWorldDataUseCase`, map its
`IUseCase<GetWorldData.Request, GetWorldData.Response>` registration to the same
scoped instance so `MergeWorldDataUseCase` can reuse it. Register every other
new `IUseCase<TRequest,TResponse>`, call
`app.MapPortalCategories()`, and add `/portal-categories/create|update|delete`
and `/portal-worlds/create|update|delete|move` to `IsWriteEndpoint`.

- [ ] **Step 3: source-generation entriesとHTTP samplesを追加する**

Add `[JsonSerializable]` entries for every new request/response/data type,
`PortalCategory[]`, `PortalCategoryVisibility`, `VRChatWorldData`,
`GetWorldData.Role` and arrays.

Add `{}` samples for list/world-data and concrete samples for category/world
CRUD and merge to `VrcWebMap.Backend.http`.

- [ ] **Step 4: buildとOpenAPIを検証する**

```bash
dotnet build --no-restore -p:RestorePackagesPath=/private/tmp/nuget-packages
dotnet test VrcWebMap.Backend.Tests/VrcWebMap.Backend.Tests.csproj \
  --no-restore \
  -p:RestorePackagesPath=/private/tmp/nuget-packages
```

Run Development on `http://127.0.0.1:5021`, then:

```bash
curl -fsS http://127.0.0.1:5021/openapi/v1.json \
  | jq -e '
      .paths["/portal-categories/list"].post
      and .paths["/portal/world-data/merge"].post
      and .components.schemas.MergeWorldDataRequest
    '
curl -fsS -o /dev/null http://127.0.0.1:5021/openapi/swagger/index.html
curl -fsS -o /dev/null http://127.0.0.1:5021/openapi/redoc/index.html
```

Expected: tests PASS、OpenAPI assertions true、Swagger/ReDoc HTTP 200。

- [ ] **Step 5: commit**

```bash
git add Endpoints Serialization Program.cs VrcWebMap.Backend.http
git commit -m "feat: expose portal category APIs"
```

---

### Task 7: Portal専用React画面を追加する

**Files:**
- Create: `src/portal.tsx`
- Create: `wwwroot/portal.html`
- Modify: `src/main.tsx`
- Modify: `vite.config.ts`
- Modify: `src/styles.css`

**Interfaces:**
- Consumes: Task 6 routes。
- Produces: anonymous download/merge UI、authenticated category management。

- [ ] **Step 1: bundle entryとHTML shellを追加する**

Add Vite input:

```typescript
portal: "src/portal.tsx"
```

Create `portal.html` using the same CSP and styles as `admin.html`, with:

```html
<body data-app="portal">
<div id="root"></div>
<script src="/assets/portal.js" type="module"></script>
</body>
```

- [ ] **Step 2: API helpersとdownload helpersを実装する**

In `portal.tsx`, implement exact helpers:

```typescript
const loadPortalCategories = () =>
    postJson("/portal-categories/list", {}).then(unwrap);

const downloadJson = (content, fileName) => {
    const blob = new Blob([content], { type: "application/json" });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = fileName;
    document.body.append(anchor);
    anchor.click();
    anchor.remove();
    URL.revokeObjectURL(url);
};

const readTextFile = (file) => file.text();
```

System download calls `/portal/world-data` with `{}` and stringifies the
returned object. Merge sends `{ existingJson }`, then downloads
`mergedJson` only on success.

- [ ] **Step 3: anonymous UIを実装する**

Render:

- `WorldData.jsonをダウンロード`;
- `.json` file input;
- selected file name and byte size;
- `マージしてダウンロード`;
- validation/collision errors;
- link back to `/`.

Do not retain file content after a successful download; clear the file state.

- [ ] **Step 4: authenticated category UIを実装する**

After `loadCurrentUser`:

- general users see Public read-only and own Personal editable;
- admin sees every category;
- create form includes Name and visibility;
- only admin may choose Public or another registered owner;
- edit form changes Name only;
- delete asks for confirmation;
- category editor contains Portal World create/update/delete/move forms;
- buttons are hidden or disabled according to returned user/category scope.

Every server error remains authoritative; UI hiding is not used as security.

- [ ] **Step 5: map navigationをPortal pageへ変更する**

Replace the old direct WorldData download button in `main.tsx` with:

```typescript
React.createElement("a", { className: "menu-button", href: "/portal.html" }, "Portal JSON")
```

Remove the map page's `downloadPortalData` state and function.

- [ ] **Step 6: styles、typecheck、buildを検証する**

Add `.portal-console` scoped responsive styles; do not change Leaflet layout.

```bash
pnpm typecheck
pnpm build
test -f wwwroot/assets/portal.js
```

Expected: typecheck/build成功、portal bundle存在。

- [ ] **Step 7: browser smoke testを行う**

Verify:

```text
/portal.html anonymous: system download and merge are enabled
/portal.html general user: own Personal CRUD, Public read-only
/portal.html admin: Public and all Personal management
/ map: markers/search/Spot details unchanged
```

- [ ] **Step 8: commit**

```bash
git add src/portal.tsx src/main.tsx src/styles.css vite.config.ts wwwroot/portal.html
git commit -m "feat: add portal world management console"
```

---

### Task 8: Documentationと全回帰・PostgreSQL検証を完了する

**Files:**
- Modify: `README.md`
- Modify: `AGENTS.md`
- Modify: `wwwroot/guide.html`
- Modify: `wwwroot/privacy.html`

**Interfaces:**
- Verifies: 全機能、security境界、既存map非混入。

- [ ] **Step 1: docsを更新する**

Document:

- Personal/Public category rules;
- owner/admin permissions and immutable visibility;
- anonymous/current-user WorldData scope;
- RoleName/DisplayNames use VRChat Display Name;
- local-file merge, 5 MiB limit, no persistence;
- duplicate Category and conflicting Role rejection;
- PostgreSQL schema update and backup recommendation before deployment.

Privacy text states uploaded JSON is processed transiently and not retained.

- [ ] **Step 2: 全自動検証を実行する**

```bash
dotnet build --no-restore -p:RestorePackagesPath=/private/tmp/nuget-packages
dotnet test VrcWebMap.Backend.Tests/VrcWebMap.Backend.Tests.csproj \
  --no-restore \
  -p:RestorePackagesPath=/private/tmp/nuget-packages
pnpm typecheck
pnpm build
```

Expected: すべて成功。

- [ ] **Step 3: PostgreSQL existing-volume相当を検証する**

Before starting Compose, take the deployment-specific backup decided in the
separate PostgreSQL operations discussion. Then:

```bash
docker compose up -d --build
docker compose exec postgres psql -U vrcwebmap -d vrcwebmap -c \
  '\\d "PortalCategories"'
docker compose exec postgres psql -U vrcwebmap -d vrcwebmap -c \
  '\\d "VRChatWorlds"'
```

Expected: PortalCategories table、nullable SpotId、PortalCategoryId、FK、
CHECK、unique indexが存在し、既存Spot/Worldデータが残る。

- [ ] **Step 4: anonymous runtimeを検証する**

```bash
curl -fsS -X POST -H 'Content-Type: application/json' -d '{}' \
  http://127.0.0.1:5021/portal/world-data \
  | jq -e '(has("Roles") | not)'

curl -fsS -X POST -H 'Content-Type: application/json' \
  -d '{"existingJson":"{\"Categorys\":[],\"VendorExtension\":true}"}' \
  http://127.0.0.1:5021/portal/world-data/merge \
  | jq -e '.mergedJson | fromjson | .VendorExtension == true'
```

Expected: anonymous output has no system Roles; merge works and preserves the
unknown property.

- [ ] **Step 5: authenticated runtimeとmap regressionを検証する**

Using Development login endpoints:

1. general user creates two Personal categories and worlds;
2. their WorldData has both categories and one Role;
3. admin login does not export the general user's Personal categories;
4. admin creates Public category and it appears anonymously;
5. general cannot mutate Public;
6. duplicate category and conflicting merge Role return predictable errors;
7. `/spots/list` count and map markers exclude every PortalCategory.

- [ ] **Step 6: Production gatingを検証する**

Run Production and verify:

```bash
curl -fsS -o /dev/null http://127.0.0.1:5021/openapi/v1.json
test "$(curl -sS -o /dev/null -w '%{http_code}' http://127.0.0.1:5021/openapi/swagger/index.html)" = "404"
test "$(curl -sS -o /dev/null -w '%{http_code}' http://127.0.0.1:5021/openapi/redoc/index.html)" = "404"
```

Expected: OpenAPI 200、Swagger/ReDoc 404。

- [ ] **Step 7: docsをcommitする**

```bash
git add README.md AGENTS.md wwwroot/guide.html wwwroot/privacy.html
git commit -m "docs: document portal category workflows"
git status --short
```

Expected: commit成功後、worktree clean。
