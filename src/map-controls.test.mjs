import assert from "node:assert/strict";
import test from "node:test";
import {
    GsiSeamlessPhotoUrl,
    JapanMapBounds,
    MapZoomOptions
} from "./map-controls.ts";

test("map controls use quarter zoom steps within Japan-focused bounds", () => {
    assert.equal(MapZoomOptions.zoomSnap, 0.25);
    assert.equal(MapZoomOptions.zoomDelta, 0.25);
    assert.equal(MapZoomOptions.minZoom, 5);
    assert.equal(MapZoomOptions.maxZoom, 19);
    assert.ok(JapanMapBounds.southWest[0] <= 20);
    assert.ok(JapanMapBounds.northEast[0] >= 46);
});

test("aerial layer uses GSI seamless photo tiles", () => {
    assert.match(GsiSeamlessPhotoUrl, /cyberjapandata\.gsi\.go\.jp\/xyz\/seamlessphoto/);
});
