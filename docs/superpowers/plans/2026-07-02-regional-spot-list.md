# Spot一覧の地域別折りたたみ Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 公開地図の通常一覧と検索結果を、backend定義の地域カテゴリ順で折りたたみ表示する。

**Architecture:** 地域表示名と順序を `AreaCategoryDisplayNames` に集約し、`/areas/list` がfrontendへmetadataを渡す。`ListSpots` はflatのまま維持し、Reactの `SpotList` が表示時にgroup化する。

**Tech Stack:** .NET 10、Kawa.Web、xUnit、React 19、TypeScript、Vite

## Global Constraints

- 三重県は中部。
- 順序は北海道、東北、関東、中部、関西、中国、四国、九州・沖縄、アジア、ヨーロッパ、アフリカ、オセアニア、北アメリカ、南アメリカ、南極。
- Spotがない地域は表示しない。
- 未定義areaは最後の `未定義地域`。
- 初期状態と検索変更時は閉じる。選択Spotの地域だけ自動で開く。
- 複数地域を同時に開ける。

---

### Task 1: 地域metadataをcontractへ公開する

**Files:**
- Create: `Models/AreaCategoryDisplayNames.cs`
- Modify: `Contracts/Areas/ListAreas.cs`
- Modify: `UseCases/Areas/ListAreasUseCase.cs`
- Modify: `UseCases/Portal/GetWorldDataUseCase.cs`
- Modify: `Serialization/AppJsonSerializerContext.cs`
- Modify: area/portal xUnit tests

- [ ] **Step 1: failure testsを書く**

Assert:

```csharp
Assert.Equal("中部", AreaCategoryDisplayNames.Get(AreaCategory.Chubu));
Assert.True(
    AreaCategoryDisplayNames.OrderOf(AreaCategory.Hokkaido) <
    AreaCategoryDisplayNames.OrderOf(AreaCategory.Tohoku));
```

`ListAreasUseCase` test asserts Tokyo item:

```csharp
Assert.Equal(AreaCategory.Kanto, tokyo.Category);
Assert.Equal("関東", tokyo.CategoryName);
Assert.Equal(2, tokyo.CategoryOrder);
```

Portal test continues asserting identical Japanese category order.

- [ ] **Step 2: failureを確認する**

```bash
dotnet test VrcWebMap.Backend.Tests/VrcWebMap.Backend.Tests.csproj \
  --no-restore -p:RestorePackagesPath=/private/tmp/nuget-packages \
  --filter "ListAreasUseCaseTests|GetWorldDataUseCaseTests|Area"
```

- [ ] **Step 3: shared metadataを実装する**

Create a static map:

```csharp
private static readonly (AreaCategory Category, string Name)[] Ordered =
[
    (AreaCategory.Hokkaido, "北海道"),
    (AreaCategory.Tohoku, "東北"),
    (AreaCategory.Kanto, "関東"),
    (AreaCategory.Chubu, "中部"),
    (AreaCategory.Kansai, "関西"),
    (AreaCategory.Chugoku, "中国"),
    (AreaCategory.Shikoku, "四国"),
    (AreaCategory.KyushuOkinawa, "九州・沖縄"),
    (AreaCategory.Asia, "アジア"),
    (AreaCategory.Europe, "ヨーロッパ"),
    (AreaCategory.Africa, "アフリカ"),
    (AreaCategory.Oceania, "オセアニア"),
    (AreaCategory.NorthAmerica, "北アメリカ"),
    (AreaCategory.SouthAmerica, "南アメリカ"),
    (AreaCategory.Antarctica, "南極")
];
```

Expose `Get`, `OrderOf`, and `All`. Replace the private order/name switch in
`GetWorldDataUseCase` with this shared type.

- [ ] **Step 4: ListAreas contractをDTO化する**

```csharp
public sealed record Item(
    int AreaCode,
    string AreaName,
    AreaCategory Category,
    string CategoryName,
    int CategoryOrder);

public sealed record Response(Item[] Areas);
```

Map every `AreaDefinitions.All` entry through the shared metadata.

- [ ] **Step 5: tests、OpenAPI、commit**

```bash
dotnet test VrcWebMap.Backend.Tests/VrcWebMap.Backend.Tests.csproj \
  --no-restore -p:RestorePackagesPath=/private/tmp/nuget-packages
git add Models Contracts/Areas UseCases/Areas UseCases/Portal Serialization VrcWebMap.Backend.Tests
git commit -m "feat: expose area category metadata"
```

Verify OpenAPI `ListAreasItem` contains all five fields and portal category
output is unchanged.

---

### Task 2: React一覧を地域別accordionへ変更する

**Files:**
- Modify: `src/main.tsx`
- Modify: `src/styles.css`

- [ ] **Step 1: grouping helperを追加する**

Inside `main.tsx`, build `areaByCode` and return groups:

```typescript
function groupSpotsByArea(spots, areas) {
    const areaByCode = new Map(areas.map((area) => [area.areaCode, area]));
    const groups = new Map();

    for (const spot of spots) {
        const area = areaByCode.get(spot.areaCode);
        const key = area?.category ?? "undefined";
        const group = groups.get(key) ?? {
            key,
            name: area?.categoryName ?? "未定義地域",
            order: area?.categoryOrder ?? Number.MAX_SAFE_INTEGER,
            spots: []
        };
        group.spots.push(spot);
        groups.set(key, group);
    }

    return [...groups.values()].sort((left, right) => left.order - right.order);
}
```

- [ ] **Step 2: SpotList stateを実装する**

`SpotList` receives `areas`, owns `Set<string> openCategories`, and resets it
when the search result identity changes. Toggle by cloning the Set. On
`selectedSpotId` change, find its group and add only that key without closing
others.

Render each heading as:

```typescript
React.createElement("button", {
    type: "button",
    "aria-expanded": isOpen,
    "aria-controls": panelId,
    onClick: () => toggle(group.key)
}, `${isOpen ? "⌄" : "›"} ${group.name} (${group.spots.length})`)
```

The controlled content uses `id={panelId}` and `hidden={!isOpen}`.

- [ ] **Step 3: call sitesと検索resetを接続する**

Pass `areas` to both normal/search `SpotList`. Use a reset key derived from the
submitted query, not each keystroke. URL direct-link selection after initial
load opens its category.

- [ ] **Step 4: stylesとvalidation**

Add `.spot-region-*` scoped styles for full-width headings, overflow wrapping,
count alignment and mobile layout.

```bash
pnpm typecheck
pnpm build
dotnet test VrcWebMap.Backend.Tests/VrcWebMap.Backend.Tests.csproj \
  --no-restore -p:RestorePackagesPath=/private/tmp/nuget-packages
```

Manually verify initial closed state, direct link, multi-open, search reset,
undefined group and keyboard operation.

- [ ] **Step 5: commit**

```bash
git add src/main.tsx src/styles.css
git commit -m "feat: group spot list by region"
git status --short
```
