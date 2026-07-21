import assert from "node:assert/strict";
import test from "node:test";
import { getConfirmations, getDefaultSelectedSourceIndexes } from "./kml-transfer.ts";

test("nearby KML candidates are excluded from the default selection", () => {
    const items = [
        { sourceIndex: 0, isSelectedByDefault: true, nearbySpots: [] },
        { sourceIndex: 1, isSelectedByDefault: false, nearbySpots: [{ id: "existing" }] }
    ];

    assert.deepEqual(getDefaultSelectedSourceIndexes(items), [0]);
});

test("selected nearby candidates become explicit confirmations", () => {
    const items = [
        { sourceIndex: 0, nearbySpots: [] },
        { sourceIndex: 1, nearbySpots: [{ id: "existing-a" }, { id: "existing-b" }] }
    ];

    assert.deepEqual(getConfirmations(items, new Set([0, 1])), [
        { sourceIndex: 1, nearbySpotIds: ["existing-a", "existing-b"] }
    ]);
});
