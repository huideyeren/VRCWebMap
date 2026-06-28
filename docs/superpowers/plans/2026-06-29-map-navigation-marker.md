# Map Navigation and Marker Presentation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Spot Atlas案内を一時的に閉じ、現在地へ戻る操作と関連情報に応じたピン色を提供する。

**Architecture:** `ListSpots` のitemへ関連情報フラグを追加し、UseCaseが3回の一覧取得でN+1を避けて集約する。frontendは純粋な優先順位関数でmarker kindを決め、Leaflet `divIcon` とCSSで描画する。位置情報はブラウザー内だけで使用する。

**Tech Stack:** .NET 10、Kawa.Web、React 19、TypeScript、Leaflet 1.9.4、Vite、PNPM

## Global Constraints

- VRChatWorldありの紫をPlaceInfoありのオレンジより優先する。
- 位置情報をAPIへ送信または保存しない。
- Spot一覧でSpotごとのrepository呼び出しを行わない。
- Spot Atlasの閉状態を永続化しない。

---

### Task 1: Spot一覧へmarkerフラグを追加

**Files:**
- Modify: `Contracts/Spots/ListSpots.cs`
- Modify: `UseCases/Spots/ListSpotsUseCase.cs`
- Modify: `VrcWebMap.Backend.Tests/UseCases/Spots/ListSpotsUseCaseTests.cs`

**Interfaces:**
- Produces: `ListSpots.Item` containing the existing flattened Spot fields plus `HasVRChatWorld` and `HasPlaceInfo`
- Produces: `ListSpots.Response(Item[] Spots)`

- [ ] **Step 1: 関連情報フラグの失敗テストを書く**

```csharp
Assert.False(items[normal.Id].HasVRChatWorld);
Assert.False(items[normal.Id].HasPlaceInfo);
Assert.True(items[worldSpot.Id].HasVRChatWorld);
Assert.True(items[placeSpot.Id].HasPlaceInfo);
Assert.True(items[bothSpot.Id].HasVRChatWorld);
Assert.True(items[bothSpot.Id].HasPlaceInfo);
```

- [ ] **Step 2: 対象テストを実行してFAILを確認する**

Run:

```bash
dotnet test VrcWebMap.Backend.Tests/VrcWebMap.Backend.Tests.csproj --no-restore -p:RestorePackagesPath=/private/tmp/nuget-packages --filter ListSpotsUseCaseTests
```

Expected: item型またはflagsが未定義のためFAIL。

- [ ] **Step 3: ContractとUseCaseを実装する**

WorldとPlaceInfoのSpot IDをそれぞれ `HashSet<Guid>` にし、検索後のSpotから既存のJSON形状を維持したflattened itemを作ってflagsを付ける。

- [ ] **Step 4: 対象テストを再実行する**

Expected: PASS。

- [ ] **Step 5: コミットする**

```bash
git add Contracts/Spots/ListSpots.cs UseCases/Spots/ListSpotsUseCase.cs VrcWebMap.Backend.Tests/UseCases/Spots/ListSpotsUseCaseTests.cs
git commit -m "feat: expose spot marker metadata"
```

### Task 2: Spot Atlasと現在地control

**Files:**
- Create: `src/map-ui.ts`
- Modify: `src/main.tsx`
- Modify: `wwwroot/styles.css`

**Interfaces:**
- Produces: dismissible map brand state
- Produces: accessible current-location Leaflet control

- [ ] **Step 1: Spot Atlasへ閉じる操作を追加する**

`useState(true)` で表示状態を保持し、閉じるボタンでfalseにする。storage APIは呼ばない。

- [ ] **Step 2: 現在地取得をPromise化する**

成功、非対応、拒否、timeoutを日本語メッセージへ変換し、取得中stateでボタンをdisabledにする。

- [ ] **Step 3: Leaflet controlを追加する**

zoom controlと重ならない位置へbuttonを置き、成功時にzoom 13で中心移動する。

- [ ] **Step 4: typecheckを実行する**

Run: `pnpm typecheck`

Expected: PASS。

- [ ] **Step 5: コミットする**

```bash
git add src wwwroot/styles.css
git commit -m "feat: improve map navigation controls"
```

### Task 3: ピン色、凡例、更新同期

**Files:**
- Modify: `src/map-ui.ts`
- Modify: `src/main.tsx`
- Modify: `wwwroot/styles.css`

**Interfaces:**
- Produces: `getMarkerKind(spot): "world" | "place" | "default"`
- Produces: Leaflet `divIcon` factory

- [ ] **Step 1: marker優先順位関数を実装する**

```ts
export function getMarkerKind(spot: SpotListItem): MarkerKind {
  if (spot.hasVRChatWorld) return "world";
  if (spot.hasPlaceInfo) return "place";
  return "default";
}
```

- [ ] **Step 2: markerを `L.divIcon` へ置き換える**

紫、オレンジ、青のclassを付け、既存popupとclick handlerを維持する。

- [ ] **Step 3: 地図へ凡例を追加する**

青「通常Spot」、オレンジ「施設情報あり」、紫「VRChatワールドあり」を表示する。

- [ ] **Step 4: 関連情報変更後の一覧再取得を確認する**

VRChatWorldとPlaceInfoの追加・削除後、現在の検索語でSpot一覧を再取得する。

- [ ] **Step 5: 全検証を実行する**

```bash
dotnet test VrcWebMap.Backend.Tests/VrcWebMap.Backend.Tests.csproj --no-restore -p:RestorePackagesPath=/private/tmp/nuget-packages
pnpm typecheck
pnpm build
```

Expected: すべて成功。

- [ ] **Step 6: Docker Composeで受け入れ確認する**

通常Spotは青、PlaceInfo追加後はオレンジ、VRChatWorld追加後は紫、両方ありは紫になることを確認する。Spot Atlasは再読み込みで戻り、現在地はHTTPS環境またはlocalhostで動作する。

- [ ] **Step 7: コミットする**

```bash
git add src wwwroot Contracts UseCases VrcWebMap.Backend.Tests
git commit -m "feat: color map markers by linked content"
```
