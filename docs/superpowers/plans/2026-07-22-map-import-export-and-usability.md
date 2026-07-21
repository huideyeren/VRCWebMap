# Map Import, Export, and Usability Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make first-time Spot registration easier with aerial imagery and precise zoom controls, and enable safe KML/KMZ import/export interoperability with Google My Maps and uMap.

**Architecture:** Keep KML parsing and distance-based duplicate detection in transport-independent Spot UseCases. The web layer only exposes those contracts, while the React/Leaflet map provides candidate selection, KML download, map-layer switching, and explicit zoom controls. KML export uses a configured public base URL so every exported Placemark preserves source, registrant, and a reliable link to its source Spot.

**Tech Stack:** .NET 10, Kawa.Abstractions/Kawa.Web, xUnit, React 19, TypeScript, Leaflet 1.9, Vite, OpenStreetMap tiles, Geospatial Information Authority of Japan tiles.

## Global Constraints

- Follow the existing contract-first/usecase-first structure: Contracts and UseCases must not depend on ASP.NET Core or Leaflet types.
- Preserve `KmlSpotImportParser` support for `.kml` and `.kmz`, the 5 MiB input limit, the 5,000 Point Placemark limit, WGS84 coordinates, and current unsupported-feature warnings.
- Any Discord-authenticated user with a registered VRChat Display Name may import; preview and import remain authorization-protected endpoints.
- Treat a candidate as near-duplicate solely when its surface distance from an existing Spot is less than or equal to 50.0 m. Name, description, and area code do not affect the decision.
- Near-duplicate candidates are initially unselected but can be explicitly selected and imported. Candidates farther than 50.0 m are initially selected.
- Re-evaluate near-duplicates immediately before persistence. The request records the exact nearby Spot IDs confirmed for each selected candidate; if the current ID set includes an unconfirmed Spot, do not persist any candidate and return a reconfirmation response.
- KML export is selection-based and may include any selected public Spot. Every Placemark description must include the source `VRC Web Map`, the registrant display name, and an absolute `?spotId={id}` link built from `App:PublicBaseUrl`.
- Do not add Google Maps API usage, OSM API authentication, automatic OSM publishing, OSM XML export, uMap account integration, GeoJSON export, or support for non-Point KML geometries.
- Display OpenStreetMap and Geospatial Information Authority of Japan attribution without hiding it behind application UI.
- Use the repository's prescribed restore/build/test commands for .NET. Use PNPM, never npm, for frontend validation.

---

## Planned File Structure

| Path | Responsibility |
|---|---|
| `Contracts/Spots/PreviewKmlImport.cs` | Exposes parser candidate IDs, default selection, and nearby public Spot summaries. |
| `Contracts/Spots/ImportKmlSpots.cs` | Carries explicitly selected candidate IDs and returns created/skipped/reconfirmation information. |
| `Contracts/Spots/ExportKmlSpots.cs` | Defines the selected public Spot IDs and KML document response. |
| `UseCases/Spots/KmlSpotImportParser.cs` | Supplies stable source indexes for parsed Point Placemark candidates. |
| `UseCases/Spots/KmlSpotDuplicateMatcher.cs` | Pure Haversine distance matching and preview/reconfirmation classification. |
| `UseCases/Spots/PreviewKmlImportUseCase.cs` | Authorizes writers, parses a KML file, and attaches duplicate summaries. |
| `UseCases/Spots/ImportKmlSpotsUseCase.cs` | Re-parses, verifies the selected candidates, rechecks current data, and atomically writes accepted Spots. |
| `UseCases/Spots/KmlSpotExporter.cs` | Builds XML-safe Point Placemark KML with provenance text and extension data. |
| `UseCases/Spots/ExportKmlSpotsUseCase.cs` | Resolves selected public Spots and registrant display names, then invokes the exporter. |
| `Options/AppOptions.cs` | Holds the configured canonical public base URL. |
| `Program.cs` | Binds `App` options and registers the export UseCase. |
| `Endpoints/Web/SpotsEndpoints.cs` | Maps the KML export contract with the other Spot endpoints. |
| `appsettings.json`, `appsettings.Development.json`, `README.md` | Documents and supplies safe development values for `App:PublicBaseUrl`. |
| `VrcWebMap.Backend.Tests/UseCases/Spots/KmlImportUseCaseTests.cs` | Covers distance classification, selection, and reconfirmation behavior. |
| `VrcWebMap.Backend.Tests/UseCases/Spots/ExportKmlSpotsUseCaseTests.cs` | Covers selected-Spot KML and required provenance. |
| `src/kml-import.ts` | Isolates preview selection and reconfirmation state transformations from React rendering. |
| `src/kml-import.test.mjs` | Verifies default candidate selection and reconfirmation state handling. |
| `src/map-controls.ts` | Declares Leaflet base-layer, zoom-bound, and Japan-bounds configuration. |
| `src/map-controls.test.mjs` | Verifies public configuration values and attribution strings. |
| `src/main.tsx` | Hosts the common import/export panel, selected Spot state, KML download, tile switcher, scale, and visible zoom controls. |
| `src/admin.tsx` | Removes the admin-only KML tab and its import-panel dependency. |
| `wwwroot/styles.css` | Styles the shared transfer panel and visible map-control group across desktop and mobile. |
| `wwwroot/guide.html` | Documents import selection, KML export, uMap/Google My Maps compatibility, and map layer controls. |

## Task 1: Add Stable KML Candidate IDs and 50 m Duplicate Classification

**Files:**
- Create: `UseCases/Spots/KmlSpotDuplicateMatcher.cs`
- Modify: `Contracts/Spots/PreviewKmlImport.cs`
- Modify: `UseCases/Spots/KmlSpotImportParser.cs`
- Modify: `VrcWebMap.Backend.Tests/UseCases/Spots/KmlImportUseCaseTests.cs`

**Interfaces:**
- Produces: `KmlSpotDuplicateMatcher.FindNearDuplicates(PreviewKmlImport.KmlImportSpotCandidate candidate, IEnumerable<Spot> existingSpots)` returning sorted `KmlSpotDuplicateMatcher.NearbySpot[]`.
- Produces: `PreviewKmlImport.KmlImportSpotCandidate.SourceIndex`, `IsSelectedByDefault`, and `NearbySpots`.
- Consumes: `Spot.Id`, `Spot.Name`, `Spot.Latitude`, and `Spot.Longitude` only for duplicate matching.

- [ ] **Step 1: Write failing duplicate-classifier tests**

Add tests that instantiate `Spot` records at calculated Haversine distances and assert the public matching contract:

```csharp
[Fact]
public void FindNearDuplicates_AtExactlyFiftyMeters_ReturnsExistingSpot()
{
    var candidate = Candidate(latitude: 35.681236, longitude: 139.767125);
    var existing = SpotAtDistance(candidate.Latitude, candidate.Longitude, meters: 50.0);

    var matches = KmlSpotDuplicateMatcher.FindNearDuplicates(candidate, [existing]);

    var match = Assert.Single(matches);
    Assert.Equal(existing.Id, match.Id);
    Assert.InRange(match.DistanceMeters, 49.99, 50.01);
}

[Fact]
public void FindNearDuplicates_BeyondFiftyMeters_ReturnsNoSpot()
{
    var candidate = Candidate(latitude: 35.681236, longitude: 139.767125);
    var existing = SpotAtDistance(candidate.Latitude, candidate.Longitude, meters: 50.1);

    Assert.Empty(KmlSpotDuplicateMatcher.FindNearDuplicates(candidate, [existing]));
}
```

- [ ] **Step 2: Run the focused test to verify it fails**

Run:

```bash
dotnet test VrcWebMap.Backend.Tests/VrcWebMap.Backend.Tests.csproj \
  --no-restore \
  -p:RestorePackagesPath=/private/tmp/nuget-packages \
  --filter FullyQualifiedName~KmlImportUseCaseTests
```

Expected: FAIL because `KmlSpotDuplicateMatcher` and the richer candidate contract do not yet exist.

- [ ] **Step 3: Define the contract additions**

Extend `PreviewKmlImport.KmlImportSpotCandidate` without introducing HTTP types:

```csharp
public sealed record KmlImportSpotCandidate(
    int SourceIndex,
    string Name,
    string Description,
    double Latitude,
    double Longitude,
    int AreaCode,
    bool IsSelectedByDefault,
    NearbySpot[] NearbySpots,
    string[] Warnings);

public sealed record NearbySpot(
    Guid Id,
    string Name,
    double Latitude,
    double Longitude,
    double DistanceMeters);
```

Update `KmlSpotImportParser.ParseKml` to increment `SourceIndex` in document Point Placemark order, set `IsSelectedByDefault: true`, and return `NearbySpots: []`. Do not make the parser query repositories or compute distances.

- [ ] **Step 4: Implement the pure distance matcher**

Create `KmlSpotDuplicateMatcher.cs` using a fixed Earth radius and final Haversine calculation:

```csharp
internal static class KmlSpotDuplicateMatcher
{
    internal const double NearDuplicateDistanceMeters = 50.0;
    private const double EarthRadiusMeters = 6_371_008.8;

    public static PreviewKmlImport.NearbySpot[] FindNearDuplicates(
        PreviewKmlImport.KmlImportSpotCandidate candidate,
        IEnumerable<Spot> existingSpots) =>
        existingSpots
            .Select(spot => new PreviewKmlImport.NearbySpot(
                spot.Id, spot.Name, spot.Latitude, spot.Longitude,
                CalculateDistanceMeters(candidate.Latitude, candidate.Longitude, spot.Latitude, spot.Longitude)))
            .Where(spot => spot.DistanceMeters <= NearDuplicateDistanceMeters)
            .OrderBy(spot => spot.DistanceMeters)
            .ThenBy(spot => spot.Id)
            .ToArray();

    internal static double CalculateDistanceMeters(double latitudeA, double longitudeA, double latitudeB, double longitudeB)
    {
        // Convert WGS84 degrees to radians before applying the Haversine formula.
        var latitudeDelta = DegreesToRadians(latitudeB - latitudeA);
        var longitudeDelta = DegreesToRadians(longitudeB - longitudeA);
        var a = Math.Pow(Math.Sin(latitudeDelta / 2), 2) +
            Math.Cos(DegreesToRadians(latitudeA)) * Math.Cos(DegreesToRadians(latitudeB)) *
            Math.Pow(Math.Sin(longitudeDelta / 2), 2);
        return 2 * EarthRadiusMeters * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;
}
```

- [ ] **Step 5: Run the focused test to verify it passes**

Run the Task 1 command again.

Expected: PASS, including existing parser tests after their constructor assertions are updated.

- [ ] **Step 6: Commit the focused boundary**

```bash
git add Contracts/Spots/PreviewKmlImport.cs UseCases/Spots/KmlSpotImportParser.cs UseCases/Spots/KmlSpotDuplicateMatcher.cs VrcWebMap.Backend.Tests/UseCases/Spots/KmlImportUseCaseTests.cs
git commit -m "feat: classify nearby KML import candidates"
```

## Task 2: Make Import Selection Safe for All Writers and Reconfirmation-Aware

**Files:**
- Modify: `Contracts/Spots/ImportKmlSpots.cs`
- Modify: `UseCases/Spots/PreviewKmlImportUseCase.cs`
- Modify: `UseCases/Spots/ImportKmlSpotsUseCase.cs`
- Modify: `VrcWebMap.Backend.Tests/UseCases/Spots/KmlImportUseCaseTests.cs`

**Interfaces:**
- Consumes: `KmlSpotDuplicateMatcher.FindNearDuplicates` and parser `SourceIndex` from Task 1.
- Produces: `ImportKmlSpots.Request(string FileName, string ContentBase64, int DefaultAreaCode, int[] SelectedSourceIndexes, NearDuplicateConfirmation[] Confirmations)`, where `NearDuplicateConfirmation` contains a source index and the exact nearby Spot IDs the user reviewed.
- Produces: `ImportKmlSpots.Response(SpotData[] Spots, int SkippedCount, string[] Warnings, int UnsupportedPlacemarkCount, PreviewKmlImport.KmlImportSpotCandidate[] ReconfirmationRequiredItems)`.

- [ ] **Step 1: Write failing authorization and selection tests**

Replace the existing admin-only test with these behaviors:

```csharp
[Fact]
public async Task Preview_Writer_AnnotatesNearbyCandidateAndDeselectsItByDefault()
{
    var repository = new FakeSpotRepository(ExistingSpotAt(35.697484, 139.582739));
    var result = await new PreviewKmlImportUseCase(repository, Writer("general-user", isAdmin: false))
        .ExecuteAsync(Request(SampleKml()));

    var item = Assert.Single(result.Value!.Items);
    Assert.False(item.IsSelectedByDefault);
    Assert.Single(item.NearbySpots);
}

[Fact]
public async Task Import_SelectedNearDuplicate_ExplicitConfirmation_CreatesSpot()
{
    var repository = new FakeSpotRepository(ExistingSpotAt(35.697484, 139.582739));
    var result = await CreateImportUseCase(repository, isAdmin: false).ExecuteAsync(
        new ImportKmlSpots.Request("spots.kml", ToBase64(SampleKml()), AreaCodes.Japan.Tokyo, [0],
            [new ImportKmlSpots.NearDuplicateConfirmation(0, [existing.Id])]));

    Assert.True(result.IsSuccess);
    Assert.Single(result.Value!.Spots);
}
```

Add a test where a second nearby Spot is inserted after preview and before import. Assert that the result succeeds as a protocol response but has `ReconfirmationRequiredItems`, contains no created `Spots`, and `repository.SavedSpots` is empty.

- [ ] **Step 2: Run the focused test to verify it fails**

Run the Task 1 focused .NET test command.

Expected: FAIL because current preview requires administrators, has no repository dependency, and import persists every parsed item.

- [ ] **Step 3: Extend the import contracts and preview UseCase**

Add XML comments for every added public record member. Change `PreviewKmlImportUseCase` to receive `ISpotRepository` and transform parser results as follows:

```csharp
var items = parseResult.Items
    .Select(item =>
    {
        var nearbySpots = KmlSpotDuplicateMatcher.FindNearDuplicates(item, spots.List());
        return item with
        {
            NearbySpots = nearbySpots,
            IsSelectedByDefault = nearbySpots.Length == 0
        };
    })
    .ToArray();
```

Use `CurrentActorPolicy.RequireWriter`; remove the separate `IsAdmin` refusal from preview and import.

- [ ] **Step 4: Implement selected-only import and re-evaluation**

In `ImportKmlSpotsUseCase`, reject duplicate source-index values and source indexes absent from the parsed file with `KawaErrorKind.Validation`. Build the selected sequence in parser order, then re-evaluate its current nearby matches:

```csharp
var selectedItems = parseResult.Items
    .Where(item => selectedSourceIndexes.Contains(item.SourceIndex))
    .ToArray();
var confirmationsBySourceIndex = request.Confirmations
    .ToDictionary(item => item.SourceIndex, item => item.NearbySpotIds.ToHashSet());
var unconfirmed = selectedItems
    .Where(item => KmlSpotDuplicateMatcher.FindNearDuplicates(item, spots.List())
        .Any(nearby => !confirmationsBySourceIndex
            .GetValueOrDefault(item.SourceIndex, [])
            .Contains(nearby.Id)))
    .ToArray();

if (unconfirmed.Length > 0)
{
    return Task.FromResult(KawaResult<ImportKmlSpots.Response>.Success(
        new([], selectedItems.Length, parseResult.Warnings, parseResult.UnsupportedPlacemarkCount,
            AnnotateDuplicates(unconfirmed, spots.List()))));
}
```

Only call `spots.Upsert` after the complete selected sequence has passed this check. Populate `SkippedCount` as parsed candidates not selected by the user. Reuse `PublicResourceMapper` for returned created Spots.

- [ ] **Step 5: Run focused tests to verify they pass**

Run:

```bash
dotnet test VrcWebMap.Backend.Tests/VrcWebMap.Backend.Tests.csproj \
  --no-restore \
  -p:RestorePackagesPath=/private/tmp/nuget-packages \
  --filter FullyQualifiedName~KmlImportUseCaseTests
```

Expected: PASS, with general writers allowed and all existing KML parser compatibility tests still green.

- [ ] **Step 6: Commit the safe import flow**

```bash
git add Contracts/Spots/ImportKmlSpots.cs Contracts/Spots/PreviewKmlImport.cs UseCases/Spots/PreviewKmlImportUseCase.cs UseCases/Spots/ImportKmlSpotsUseCase.cs VrcWebMap.Backend.Tests/UseCases/Spots/KmlImportUseCaseTests.cs
git commit -m "feat: let writers review KML import duplicates"
```

## Task 3: Export Selected Public Spots as Provenance-Preserving KML

**Files:**
- Create: `Contracts/Spots/ExportKmlSpots.cs`
- Create: `Options/AppOptions.cs`
- Create: `UseCases/Spots/KmlSpotExporter.cs`
- Create: `UseCases/Spots/ExportKmlSpotsUseCase.cs`
- Create: `VrcWebMap.Backend.Tests/UseCases/Spots/ExportKmlSpotsUseCaseTests.cs`
- Modify: `Program.cs`
- Modify: `Endpoints/Web/SpotsEndpoints.cs`
- Modify: `appsettings.json`
- Modify: `appsettings.Development.json`
- Modify: `README.md`

**Interfaces:**
- Produces: `ExportKmlSpots.Request(Guid[] SpotIds)` and `ExportKmlSpots.Response(string FileName, string Content, Guid[] MissingSpotIds)`.
- Produces: `AppOptions.PublicBaseUrl` as a canonical absolute `https` production URL without a trailing slash.
- Consumes: `ISpotRepository.List()`, `IDiscordUserRepository.List()`, and `IOptions<AppOptions>`.

- [ ] **Step 1: Write failing export tests**

Create tests covering selected-only output and required provenance:

```csharp
[Fact]
public async Task ExecuteAsync_SelectedSpot_ExportsKmlWithProvenanceAndDirectLink()
{
    var spot = Spot("井の頭公園駅", "owner-user");
    var useCase = new ExportKmlSpotsUseCase(
        new FakeSpotRepository(spot),
        FakeDiscordUserRepository.WithVRChatDisplayName("owner-user", "Karina"),
        Options.Create(new AppOptions { PublicBaseUrl = "https://maps.example.test" }));

    var result = await useCase.ExecuteAsync(new ExportKmlSpots.Request([spot.Id]));

    Assert.True(result.IsSuccess);
    Assert.Contains("出典: VRC Web Map", result.Value!.Content);
    Assert.Contains("登録者: Karina", result.Value.Content);
    Assert.Contains($"https://maps.example.test/?spotId={spot.Id}", result.Value.Content);
    Assert.Contains($"<Data name=\"vrcwebmap:spotId\"><value>{spot.Id}</value>", result.Value.Content);
}

[Fact]
public async Task ExecuteAsync_UnselectedSpot_IsNotExported()
{
    // Create two records, request one ID, and assert the other name and ID are absent.
}
```

- [ ] **Step 2: Run the focused test to verify it fails**

Run:

```bash
dotnet test VrcWebMap.Backend.Tests/VrcWebMap.Backend.Tests.csproj \
  --no-restore \
  -p:RestorePackagesPath=/private/tmp/nuget-packages \
  --filter FullyQualifiedName~ExportKmlSpotsUseCaseTests
```

Expected: FAIL because the export contract and UseCase do not exist.

- [ ] **Step 3: Add canonical URL options and the export contract**

Create the options type and bind it in `Program.cs`:

```csharp
public sealed class AppOptions
{
    public string PublicBaseUrl { get; init; } = string.Empty;
}

builder.Services.Configure<AppOptions>(builder.Configuration.GetSection("App"));
```

Add `App:PublicBaseUrl` to the settings files: `http://localhost:5021` for development and a blank documented production value. Add the required production environment variable `App__PublicBaseUrl=https://maps.example.com` to the README configuration section.

Define `ExportKmlSpots` as a normal contract class in `Contracts/Spots/`, and map `POST /spots/export/kml` in `SpotsEndpoints`. The endpoint remains readable without authorizing a user because it exports only already-public data.

- [ ] **Step 4: Implement XML-safe KML construction**

Create `KmlSpotExporter` with `XDocument`/`XElement` so Spot content and display names are XML-escaped by construction. It must produce a KML 2.2 document and each selected Spot must contain:

```xml
<Placemark>
  <name>Spot name</name>
  <description><![CDATA[Spot description

出典: VRC Web Map
登録者: Karina
元Spot: https://maps.example.com/?spotId=...]]></description>
  <ExtendedData>
    <Data name="vrcwebmap:spotId"><value>...</value></Data>
  </ExtendedData>
  <Point><coordinates>139.767125,35.681236,0</coordinates></Point>
</Placemark>
```

`ExportKmlSpotsUseCase` must validate that `PublicBaseUrl` is an absolute HTTP or HTTPS URI, normalize its trailing slash once, select only existing requested IDs, preserve request ID order, resolve registrant names through `PublicResourceMapper`, and return missing IDs without failing a partially valid request.

- [ ] **Step 5: Register and verify endpoint compatibility**

Register `IUseCase<ExportKmlSpots.Request, ExportKmlSpots.Response>` in `AddUseCases`. Extend the public contract test table if the response exposes public resource DTOs; otherwise add a direct contract-shape assertion for the KML string and `MissingSpotIds` properties.

Run:

```bash
dotnet test VrcWebMap.Backend.Tests/VrcWebMap.Backend.Tests.csproj \
  --no-restore \
  -p:RestorePackagesPath=/private/tmp/nuget-packages \
  --filter "FullyQualifiedName~ExportKmlSpotsUseCaseTests|FullyQualifiedName~PublicResourceContractTests"
```

Expected: PASS.

- [ ] **Step 6: Commit selected KML export**

```bash
git add Contracts/Spots/ExportKmlSpots.cs Options/AppOptions.cs UseCases/Spots/KmlSpotExporter.cs UseCases/Spots/ExportKmlSpotsUseCase.cs Endpoints/Web/SpotsEndpoints.cs Program.cs appsettings.json appsettings.Development.json README.md VrcWebMap.Backend.Tests/UseCases/Spots/ExportKmlSpotsUseCaseTests.cs VrcWebMap.Backend.Tests/Contracts/PublicResourceContractTests.cs
git commit -m "feat: export selected spots as attributed KML"
```

## Task 4: Move KML Transfer to the Common Map Experience

**Files:**
- Create: `src/kml-import.ts`
- Create: `src/kml-import.test.mjs`
- Modify: `src/main.tsx`
- Modify: `src/admin.tsx`
- Modify: `wwwroot/styles.css`
- Modify: `wwwroot/guide.html`

**Interfaces:**
- Consumes: `POST /spots/import/kml/preview`, `POST /spots/import/kml`, and `POST /spots/export/kml` from Tasks 2-3.
- Produces: `buildImportPayload(file, defaultAreaCode, selectedSourceIndexes, confirmations)` and `getDefaultSelectedSourceIndexes(items)` in `src/kml-import.ts`.
- Produces: map-screen `KmlTransferPanel` that handles import preview, confirmation, selection, and selected Spot export download.

- [ ] **Step 1: Write failing frontend state tests**

Create `src/kml-import.test.mjs` using Node's test runner:

```javascript
import assert from "node:assert/strict";
import test from "node:test";
import { getDefaultSelectedSourceIndexes, getConfirmations } from "./kml-import.ts";

test("nearby import candidates are excluded until explicitly checked", () => {
    const items = [
        { sourceIndex: 0, isSelectedByDefault: true, nearbySpots: [] },
        { sourceIndex: 1, isSelectedByDefault: false, nearbySpots: [{ id: "nearby" }] }
    ];

    assert.deepEqual(getDefaultSelectedSourceIndexes(items), [0]);
    assert.deepEqual(getConfirmations(items, new Set([0, 1])), [
        { sourceIndex: 1, nearbySpotIds: ["nearby"] }
    ]);
});
```

- [ ] **Step 2: Run the frontend test to verify it fails**

Run:

```bash
pnpm test -- kml-import.test.mjs
```

Expected: FAIL because the transfer-state module does not exist.

- [ ] **Step 3: Implement isolated import selection helpers**

Create `src/kml-import.ts` with the exported helpers from Step 1. Keep all network access and React state out of this module. A near-duplicate is confirmed only if its `sourceIndex` is both selected and has at least one `nearbySpots` entry.

- [ ] **Step 4: Replace the admin-only panel with the common panel**

In `src/main.tsx`:

- Replace `KmlImportPanel` with a `KmlTransferPanel` rendered from the map screen's menu/panel area when `currentUser?.hasVRChatDisplayName` is true.
- Keep file reading and base64 generation in the component, but send `selectedSourceIndexes` and the exact per-candidate nearby-Spot confirmations returned by the helper module.
- Render every candidate with a checkbox. Initialize state from `isSelectedByDefault`; visually label near-duplicate rows with the nearby Spot name and rounded metre distance.
- When an import result has `reconfirmationRequiredItems`, replace the current preview with those server-provided items, select none by default, and tell the user that another registration changed the duplicate situation.
- Add selected Spot checkboxes to `SpotList` or a dedicated export section. Disable the export action when no Spot is selected; post `{ spotIds: selectedIds }`, then use the existing Blob/object URL download pattern with the response filename and KML content.

In `src/admin.tsx`, remove `KmlImportPanel` import, the `kml` tab button, and its conditional content. Do not remove the administrator's normal Spot and user management features.

- [ ] **Step 5: Add visual styling and update user guidance**

Add `kml-transfer-panel`, `kml-candidate`, `kml-candidate--nearby`, `spot-export-selection`, and responsive styles in `wwwroot/styles.css`. The near-duplicate state must have a text label in addition to color.

Revise `wwwroot/guide.html` to state that writers can import KML/KMZ; candidates within 50m are unchecked until confirmed; selected Spots export to KML for Google My Maps and uMap; every exported item includes source, registrant, and a link back to VRC Web Map.

- [ ] **Step 6: Run frontend validation**

Run:

```bash
pnpm test
pnpm typecheck
pnpm build
```

Expected: all commands exit 0 and `wwwroot/assets/app.js`, `admin.js`, and `app.css` are regenerated from the TypeScript sources.

- [ ] **Step 7: Commit the shared transfer UI**

```bash
git add src/kml-import.ts src/kml-import.test.mjs src/main.tsx src/admin.tsx wwwroot/styles.css wwwroot/guide.html wwwroot/assets/app.js wwwroot/assets/admin.js wwwroot/assets/app.css
git commit -m "feat: add common KML transfer controls"
```

## Task 5: Improve Map Registration Precision with Layers and Visible Zoom Controls

**Files:**
- Create: `src/map-controls.ts`
- Create: `src/map-controls.test.mjs`
- Modify: `src/main.tsx`
- Modify: `wwwroot/styles.css`
- Modify: `wwwroot/guide.html`

**Interfaces:**
- Produces: `JapanMapBounds`, `MapZoomOptions`, and `createBaseLayers()` from `src/map-controls.ts`.
- Consumes: Leaflet's `L.tileLayer`, `L.control.layers`, `L.control.zoom`, and `L.control.scale` in `src/main.tsx`.
- Produces: `setDraftCoordinatesFromMapCenter(map, previousDraft)` for the registration form's explicit centre-coordinate action.

- [ ] **Step 1: Write failing map-control configuration tests**

Create a source-level Node test that verifies the stable policy values:

```javascript
import assert from "node:assert/strict";
import test from "node:test";
import { JapanMapBounds, MapZoomOptions, GsiSeamlessPhotoUrl } from "./map-controls.ts";

test("map controls constrain Japan-focused navigation to quarter zoom levels", () => {
    assert.equal(MapZoomOptions.zoomSnap, 0.25);
    assert.equal(MapZoomOptions.zoomDelta, 0.25);
    assert.equal(MapZoomOptions.maxZoom, 19);
    assert.ok(JapanMapBounds.southWest.lat < 25);
    assert.ok(GsiSeamlessPhotoUrl.includes("seamlessphoto"));
});
```

- [ ] **Step 2: Run the focused test to verify it fails**

Run:

```bash
pnpm test -- map-controls.test.mjs
```

Expected: FAIL because the map-control module does not exist.

- [ ] **Step 3: Define map-layer and zoom configuration**

Create `src/map-controls.ts` with explicit Leaflet-compatible configuration:

```typescript
export const JapanMapBounds = L.latLngBounds([20, 122], [47, 154]);
export const MapZoomOptions = {
    minZoom: 5,
    maxZoom: 19,
    zoomSnap: 0.25,
    zoomDelta: 0.25,
    maxBounds: JapanMapBounds,
    maxBoundsViscosity: 1.0
};
export const GsiSeamlessPhotoUrl = "https://cyberjapandata.gsi.go.jp/xyz/seamlessphoto/{z}/{x}/{y}.jpg";
```

Export `createBaseLayers()` returning named `L.TileLayer` instances for `標準地図` and `航空写真`. Give the OSM layer its existing attribution and give the GSI layer the required `地理院タイル` attribution. Keep `maxNativeZoom` and `maxZoom` aligned with each provider's published availability.

- [ ] **Step 4: Wire controls into Leaflet and registration**

Replace the inline map setup in `src/main.tsx` with the configuration from Task 5:

```typescript
const map = L.map(mapElementRef.current, {
    zoomControl: false,
    ...MapZoomOptions
}).setView(TokyoStation, 6);
const baseLayers = createBaseLayers();
baseLayers["標準地図"].addTo(map);
L.control.layers(baseLayers, undefined, { position: "topright" }).addTo(map);
L.control.zoom({ position: "topright", zoomInText: "+", zoomOutText: "−" }).addTo(map);
L.control.scale({ position: "bottomright", imperial: false }).addTo(map);
```

Place the map control group above other right-side overlays with CSS, enlarge the buttons to touch-friendly targets, and preserve attribution visibility. Add a `地図中心を座標に反映` button in `SpotForm`; it copies `mapRef.current.getCenter()` into the active draft after `roundCoordinate` processing.

- [ ] **Step 5: Run all frontend checks and inspect the rendered result**

Run:

```bash
pnpm test
pnpm typecheck
pnpm build
```

Expected: all commands exit 0. Start the development app and visually verify standard/aerial switching, `+`/`−` at upper right, a metric scale at lower right, visible attribution, constrained map drag, 0.25 zoom transitions, and centre-coordinate registration.

- [ ] **Step 6: Update guidance and commit the usability work**

Update the map-registration section in `wwwroot/guide.html` to explain the standard/aerial toggle, visible zoom buttons, scale indicator, and centre-coordinate action.

```bash
git add src/map-controls.ts src/map-controls.test.mjs src/main.tsx wwwroot/styles.css wwwroot/guide.html wwwroot/assets/app.js wwwroot/assets/app.css
git commit -m "feat: improve map layers and zoom controls"
```

## Task 6: Run the Full Regression Suite and Deliver User-Facing Documentation

**Files:**
- Modify: `README.md`
- Modify: `wwwroot/guide.html`
- Modify: `docs/superpowers/specs/2026-07-22-map-import-export-and-usability-design.md` only if verification reveals a corrected implementation detail.

**Interfaces:**
- Consumes: all implementation work from Tasks 1-5.
- Produces: verified OpenAPI contract, generated frontend assets, and user-facing instructions matching the delivered behavior.

- [ ] **Step 1: Run backend tests**

Run:

```bash
dotnet test VrcWebMap.Backend.Tests/VrcWebMap.Backend.Tests.csproj \
  --no-restore \
  -p:RestorePackagesPath=/private/tmp/nuget-packages
```

Expected: PASS.

- [ ] **Step 2: Run the complete frontend verification**

Run:

```bash
pnpm test
pnpm typecheck
pnpm build
```

Expected: all commands exit 0 and generated files match their TypeScript source changes.

- [ ] **Step 3: Verify the generated API contract and help text**

Start the app in Development and inspect `/openapi/v1.json`. Confirm it contains:

```text
/spots/import/kml/preview
/spots/import/kml
/spots/export/kml
```

Confirm the API schemas expose `sourceIndex`, `isSelectedByDefault`, `nearbySpots`, selected source indexes, reconfirmation candidates, and export filename/content/missing Spot IDs. In the browser, confirm the guide names the 50m rule, explicit confirmation, KML compatibility with Google My Maps/uMap, provenance, and map controls.

- [ ] **Step 4: Commit final documentation or generated-surface corrections**

Only if Steps 1-3 required a documentation or generated asset correction:

```bash
git add README.md wwwroot/guide.html wwwroot/assets docs/superpowers/specs/2026-07-22-map-import-export-and-usability-design.md
git commit -m "docs: verify map import export workflow"
```

If no correction was necessary, do not create an empty commit.

## Plan Self-Review

- **Spec coverage:** Tasks 1-2 implement parser reuse, all-writer KML import, 50m-only duplicate detection, default exclusion, explicit confirmation, and execution-time re-evaluation. Task 3 implements selected KML output plus source, registrant, and canonical origin links. Task 4 moves KML transfer out of the administrator-only screen and documents Google My Maps/uMap usage. Task 5 implements GSI aerial imagery, Japan-focused bounds, fine zoom steps, visible zoom controls, scale, and centre registration. Task 6 validates the full backend, generated client surface, UI, and documentation.
- **Placeholder scan:** No implementation steps defer behavior or hide validation/error handling behind generic wording.
- **Type consistency:** Task 1 establishes candidate IDs and nearby summaries; Task 2 carries those IDs through selection and reconfirmation; Task 3 uses only Spot IDs and a KML response; Task 4 consumes those exact properties; Task 5 is independent of the transfer contracts.
