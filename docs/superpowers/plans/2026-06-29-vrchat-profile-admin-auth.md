# VRChat Profile and Administrator Authentication Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Discord Botを使わず、VRChat表示名を登録したユーザーだけが書き込みでき、管理者が独立した全画面管理画面から他ユーザーの管理者権限を管理できるようにする。

**Architecture:** DiscordユーザーIDを不変の主体IDとして維持し、VRChat表示名と管理者状態を `DiscordUser` に保存する。UseCaseはtransport非依存の `ICurrentActorAccessor` から現在ユーザーを取得し、HTTP requestからユーザーIDや管理者フラグを除外する。管理画面は `/admin.html` の独立Vite entryとして実装する。

**Tech Stack:** .NET 10、Kawa.Web 0.3.1、EF Core/Npgsql、PostgreSQL 18、React 19、TypeScript、Vite、PNPM 11

## Global Constraints

- Kawaのcontract-first / usecase-first順序に従う。
- UseCaseへASP.NET CoreまたはHTTP型を漏らさない。
- VRChat表示名はtrim、Unicode Form KC、`ToUpperInvariant()` で一意性を判定する。
- 閲覧は表示名未登録でも許可し、書き込みは拒否する。
- 初期管理者は `Discord:InitialAdminUserIds` のDiscordユーザーIDから確立する。
- Discord OAuth scopeは `identify guilds.members.read` とし、Bot tokenとguild roles APIを使わない。
- frontend依存管理とbuildにはPNPMだけを使う。
- PostgreSQL既存データを初期化しない。

---

### Task 1: ユーザーモデル、repository、PostgreSQL schema

**Files:**
- Modify: `Models/DiscordUser.cs`
- Modify: `UseCases/Users/IDiscordUserRepository.cs`
- Modify: `Stores/InMemoryDiscordUserRepository.cs`
- Modify: `Stores/PostgreSqlDiscordUserRepository.cs`
- Modify: `Stores/AppDbContext.cs`
- Modify: `Stores/PostgreSqlSchemaInitializer.cs`
- Modify: `VrcWebMap.Backend.Tests/TestDoubles/FakeDiscordUserRepository.cs`
- Modify: `VrcWebMap.Backend.Tests/Stores/PostgreSqlSchemaInitializerTests.cs`
- Create: `VrcWebMap.Backend.Tests/Stores/InMemoryDiscordUserRepositoryTests.cs`

**Interfaces:**
- Produces: `DiscordUser.VRChatDisplayName`, `DiscordUser.NormalizedVRChatDisplayName`
- Produces: `IDiscordUserRepository.List()`
- Produces: `IDiscordUserRepository.TryGetByNormalizedVRChatDisplayName(string, out DiscordUser?)`

- [ ] **Step 1: schema SQLとin-memory検索の失敗テストを書く**

```csharp
[Fact]
public void EnsureDiscordProfileSchemaSql_AddsNullableColumnsAndUniquePartialIndex()
{
    Assert.Contains("ADD COLUMN IF NOT EXISTS \"VRChatDisplayName\"", PostgreSqlSchemaInitializer.EnsureDiscordProfileSchemaSql);
    Assert.Contains("ADD COLUMN IF NOT EXISTS \"NormalizedVRChatDisplayName\"", PostgreSqlSchemaInitializer.EnsureDiscordProfileSchemaSql);
    Assert.Contains("CREATE UNIQUE INDEX IF NOT EXISTS", PostgreSqlSchemaInitializer.EnsureDiscordProfileSchemaSql);
    Assert.Contains("WHERE \"NormalizedVRChatDisplayName\" IS NOT NULL", PostgreSqlSchemaInitializer.EnsureDiscordProfileSchemaSql);
}
```

- [ ] **Step 2: 対象テストを実行してコンパイル失敗を確認する**

Run:

```bash
dotnet test VrcWebMap.Backend.Tests/VrcWebMap.Backend.Tests.csproj --no-restore -p:RestorePackagesPath=/private/tmp/nuget-packages --filter "PostgreSqlSchemaInitializerTests|InMemoryDiscordUserRepositoryTests"
```

Expected: 新しいプロパティまたはAPIが未定義のためFAIL。

- [ ] **Step 3: モデル、repository、schema補修を実装する**

`DiscordUser` の末尾へ次を追加する。

```csharp
string? VRChatDisplayName,
string? NormalizedVRChatDisplayName
```

repositoryへ一覧と正規化名検索を追加し、PostgreSQLでは `AsNoTracking()` を使う。`AppDbContext` は両列を最大100文字とし、正規化列にnullを除外したunique indexを設定する。既存DB向けに `ALTER TABLE ... ADD COLUMN IF NOT EXISTS` と `CREATE UNIQUE INDEX IF NOT EXISTS` を起動時に実行する。

- [ ] **Step 4: 対象テストを再実行する**

Expected: PASS。

- [ ] **Step 5: コミットする**

```bash
git add Models/DiscordUser.cs UseCases/Users/IDiscordUserRepository.cs Stores VrcWebMap.Backend.Tests
git commit -m "feat: persist VRChat user profiles"
```

### Task 2: Bot不要のDiscord登録と初期管理者

**Files:**
- Modify: `Options/DiscordOptions.cs`
- Modify: `Contracts/Users/RegisterDiscordUser.cs`
- Modify: `UseCases/Users/RegisterDiscordUserUseCase.cs`
- Modify: `Services/DiscordApiClient.cs`
- Modify: `Endpoints/Web/DiscordAuthEndpoints.cs`
- Modify: `VrcWebMap.Backend.Tests/UseCases/Users/RegisterDiscordUserUseCaseTests.cs`
- Create: `VrcWebMap.Backend.Tests/Services/DiscordApiClientTests.cs`

**Interfaces:**
- Produces: `DiscordOptions.InitialAdminUserIds`
- Changes: `RegisterDiscordUser.Request` no longer accepts `IsAdmin`
- Preserves: existing `IsAdmin`, `VRChatDisplayName`, normalized display name

- [ ] **Step 1: 初期管理者と既存権限維持の失敗テストを書く**

```csharp
[Fact]
public async Task ExecuteAsync_InitialAdminId_CreatesAdministrator()
{
    var useCase = new RegisterDiscordUserUseCase(repository, Options.Create(new DiscordOptions
    {
        InitialAdminUserIds = ["123"]
    }));
    var result = await useCase.ExecuteAsync(CreateRequest("123"));
    Assert.True(result.Value!.User.IsAdmin);
}
```

既存管理者の再ログインで `IsAdmin` とVRChat表示名が維持されるテストも追加する。

- [ ] **Step 2: 対象テストを実行してFAILを確認する**

Expected: constructorまたはoptions propertyが未定義のためFAIL。

- [ ] **Step 3: 登録UseCaseとDiscord clientを実装する**

`InitialAdminUserIds` は `string[]` とする。登録時の管理者状態は `isInitialAdmin || existing?.IsAdmin == true` とする。`HasAdminRoleAsync`、`BotToken`、`AdminRoleName` とcallback内のrole判定を削除する。

- [ ] **Step 4: OAuth URLに `bot` が含まれず必要scopeだけであることをテストする**

```csharp
Assert.Contains("identify", uri.Query);
Assert.Contains("guilds.members.read", uri.Query);
Assert.DoesNotContain("bot", uri.Query);
```

- [ ] **Step 5: 対象テストを再実行する**

Expected: PASS。

- [ ] **Step 6: コミットする**

```bash
git add Options Contracts/Users/RegisterDiscordUser.cs UseCases/Users/RegisterDiscordUserUseCase.cs Services/DiscordApiClient.cs Endpoints/Web/DiscordAuthEndpoints.cs VrcWebMap.Backend.Tests
git commit -m "feat: replace Discord bot role authentication"
```

### Task 3: 現在ユーザー、VRChat表示名、管理者UseCases

**Files:**
- Create: `UseCases/Users/CurrentActor.cs`
- Create: `UseCases/Users/ICurrentActorAccessor.cs`
- Create: `Contracts/Users/UpdateVRChatDisplayName.cs`
- Create: `Contracts/Users/ListUsers.cs`
- Create: `Contracts/Users/SetUserAdminStatus.cs`
- Create: `UseCases/Users/VRChatDisplayNameNormalizer.cs`
- Create: `UseCases/Users/UpdateVRChatDisplayNameUseCase.cs`
- Create: `UseCases/Users/ListUsersUseCase.cs`
- Create: `UseCases/Users/SetUserAdminStatusUseCase.cs`
- Create: `VrcWebMap.Backend.Tests/TestDoubles/FakeCurrentActorAccessor.cs`
- Create: `VrcWebMap.Backend.Tests/UseCases/Users/UpdateVRChatDisplayNameUseCaseTests.cs`
- Create: `VrcWebMap.Backend.Tests/UseCases/Users/ListUsersUseCaseTests.cs`
- Create: `VrcWebMap.Backend.Tests/UseCases/Users/SetUserAdminStatusUseCaseTests.cs`

**Interfaces:**
- Produces: `CurrentActor(string DiscordUserId, bool IsAdmin, bool HasVRChatDisplayName)`
- Produces: `ICurrentActorAccessor.GetCurrent()`
- Produces: profile/admin contracts and UseCases

- [ ] **Step 1: 表示名検証と権限操作の失敗テストを書く**

テストには、4文字未満、15文字超過、trim、Form KC、大文字小文字重複、一般ユーザー拒否、初期管理者解除拒否、自己解除拒否、他ユーザーへの付与と解除を含める。

- [ ] **Step 2: 新しいUseCaseテストを実行してFAILを確認する**

Expected: 新規型が未定義のためFAIL。

- [ ] **Step 3: contractとUseCaseを実装する**

表示名の文字数は `StringInfo.GetTextElementEnumerator` で数える。重複時は `KawaErrorKind.Conflict`、一般ユーザーは `Forbidden`、対象不在は `NotFound`、保護対象解除は `Conflict` を返す。

- [ ] **Step 4: 新しいUseCaseテストを再実行する**

Expected: PASS。

- [ ] **Step 5: コミットする**

```bash
git add Contracts/Users UseCases/Users VrcWebMap.Backend.Tests
git commit -m "feat: add VRChat profiles and administrator management"
```

### Task 4: サーバー側主体解決と既存書き込みContract

**Files:**
- Create: `Endpoints/Web/HttpCurrentActorAccessor.cs`
- Create: `Endpoints/Web/UsersEndpoints.cs`
- Modify: all create/update/delete/import contracts under `Contracts/Spots`, `Contracts/VRChatWorlds`, `Contracts/PlaceInfos`, `Contracts/WebLinks`, `Contracts/Comments`
- Modify: corresponding UseCases
- Modify: corresponding tests
- Modify: `Endpoints/Web/SpotsEndpoints.cs`
- Modify: `Endpoints/Web/SpotContentEndpoints.cs`
- Modify: `Program.cs`

**Interfaces:**
- Consumes: `ICurrentActorAccessor`
- Changes: public mutation requests contain business data only
- Produces: authenticated `/users/profile`, `/users/list`, `/users/admin-status`

- [ ] **Step 1: 表示名未登録と偽装不能性の失敗テストを書く**

Create/Update/Delete/KML usecase testsは `FakeCurrentActorAccessor` を注入し、未登録actorで `Forbidden`、一般actorで所有者編集可能、管理者actorで管理操作可能になることを確認する。

- [ ] **Step 2: 対象テストを実行してFAILを確認する**

Expected: constructorとrequest shapeの不一致でFAIL。

- [ ] **Step 3: mutation contractから主体入力を削除する**

`RegisteredByUserId`、`ActorUserId`、`ActorIsAdmin` を外部requestから削除する。UseCaseは `ICurrentActorAccessor.GetCurrent()` を呼び、nullまたは表示名未登録なら `Forbidden` を返す。所有者IDは `actor.DiscordUserId`、管理者判定は `actor.IsAdmin` を使う。

- [ ] **Step 4: HTTP actor adapterとUsers endpointを登録する**

`HttpCurrentActorAccessor` は `ClaimTypes.NameIdentifier` からDiscord IDを取り、`IDiscordUserRepository` の最新値を返す。`UsersEndpoints` はKawa endpointとして3つのUseCaseを公開し、authorizationを必須にする。

- [ ] **Step 5: `/auth/me` をDB正本へ変更する**

`AuthSession.CurrentUserResponse` に `VRChatDisplayName` と `HasVRChatDisplayName` を追加し、endpointはrepositoryの最新ユーザーを返す。Cookieには不変IDだけを必須claimとして保存する。

- [ ] **Step 6: 全UseCaseテストを実行する**

Expected: PASS。

- [ ] **Step 7: コミットする**

```bash
git add Contracts UseCases Endpoints Program.cs VrcWebMap.Backend.Tests
git commit -m "refactor: derive write authorization from session"
```

### Task 5: プロフィールUIとfrontend共有モジュール

**Files:**
- Create: `src/api.ts`
- Create: `src/auth.ts`
- Create: `src/profile.tsx`
- Modify: `src/main.tsx`
- Modify: `wwwroot/styles.css`

**Interfaces:**
- Produces: `loadCurrentUser`, `postJson`, authenticated user TypeScript types
- Produces: `ProfileSettings`
- Consumes: `/users/profile`

- [ ] **Step 1: API helperとprofile componentを分離する**

`postJson` と認証型を共有moduleへ移し、地図と管理画面の両方から利用可能にする。

- [ ] **Step 2: ハンバーガーメニューへプロフィール設定を追加する**

未登録時は登録案内を表示し、書き込みUIを無効化する。表示名は `vrChatDisplayName ?? displayName ?? username` の順で表示する。

- [ ] **Step 3: TypeScript typecheckを実行する**

Run: `pnpm typecheck`

Expected: PASS。

- [ ] **Step 4: コミットする**

```bash
git add src wwwroot/styles.css
git commit -m "feat: add VRChat profile settings"
```

### Task 6: `/admin.html` 全画面管理UI

**Files:**
- Create: `src/admin.tsx`
- Create: `src/admin.css`
- Create: `src/components/AdminEditors.tsx`
- Create: `wwwroot/admin.html`
- Modify: `src/main.tsx`
- Modify: `vite.config.ts`
- Modify: `wwwroot/styles.css`

**Interfaces:**
- Consumes: `/auth/me`, `/users/list`, `/users/admin-status`, Spot/content/KML APIs
- Produces: independent Vite entry `admin.js`

- [ ] **Step 1: Viteへadmin entryを追加する**

```ts
input: {
  app: "src/main.tsx",
  admin: "src/admin.tsx"
}
```

出力名はentry名から `app.js` と `admin.js` を生成する。

- [ ] **Step 2: 管理画面の認証状態を実装する**

未ログインはログイン案内、一般ユーザーは拒否、管理者だけタブUIを表示する。

- [ ] **Step 3: Spot管理テーブルと編集領域を実装する**

名前検索、地域絞り込み、全幅編集領域を提供し、既存editorを共有componentへ移す。

- [ ] **Step 4: ユーザー管理テーブルを実装する**

初期管理者と自分自身の解除をdisabledにし、操作後にユーザー一覧を再取得する。

- [ ] **Step 5: KML importを移設する**

preview/import後にSpot一覧を再取得する。

- [ ] **Step 6: 地図画面から埋め込み管理画面を削除する**

ハンバーガーメニューは `/admin.html` へのanchorにする。

- [ ] **Step 7: typecheckとbuildを実行する**

```bash
pnpm typecheck
pnpm build
```

Expected: `wwwroot/assets/app.js` と `wwwroot/assets/admin.js` が生成される。

- [ ] **Step 8: コミットする**

```bash
git add src vite.config.ts wwwroot
git commit -m "feat: add full-screen administration console"
```

### Task 7: 文書と統合検証

**Files:**
- Modify: `README.md`
- Modify: `wwwroot/guide.html`
- Modify: `wwwroot/terms.html`
- Modify: `wwwroot/privacy.html`
- Modify: `appsettings.json`
- Modify: `appsettings.Development.json`
- Modify: `docker-compose.yml`

- [ ] **Step 1: Bot設定を初期管理者設定へ置き換える**

README、設定例、Composeへ `Discord__InitialAdminUserIds__0` を記載し、Bot tokenと管理者ロール説明を削除する。

- [ ] **Step 2: 利用者向け文書を更新する**

VRChat表示名、書き込み条件、管理者権限の管理方法、保存する情報を反映する。

- [ ] **Step 3: backendとfrontendを検証する**

```bash
dotnet build --no-restore -p:RestorePackagesPath=/private/tmp/nuget-packages
dotnet test VrcWebMap.Backend.Tests/VrcWebMap.Backend.Tests.csproj --no-restore -p:RestorePackagesPath=/private/tmp/nuget-packages
pnpm typecheck
pnpm build
```

Expected: すべて成功。

- [ ] **Step 4: Docker Composeで既存volumeから起動する**

`docker compose up -d --build` 後、ログイン、プロフィール登録、管理者付与、Spot編集、KML previewを確認する。DB volumeは削除しない。

- [ ] **Step 5: コミットする**

```bash
git add README.md appsettings*.json docker-compose.yml wwwroot
git commit -m "docs: document VRChat profile authentication"
```
