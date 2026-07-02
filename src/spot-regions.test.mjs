import assert from "node:assert/strict";
import test from "node:test";
import {
    getSpotCategoryKey,
    groupSpotsByArea
} from "./spot-regions.ts";

const areas = [
    { areaCode: 13, category: 2, categoryName: "関東", categoryOrder: 2 },
    { areaCode: 23, category: 3, categoryName: "中部", categoryOrder: 3 },
    { areaCode: 27, category: 4, categoryName: "関西", categoryOrder: 4 }
];

test("groupSpotsByArea groups only populated regions in backend order", () => {
    const spots = [
        { id: "osaka", name: "大阪", areaCode: 27 },
        { id: "tokyo", name: "東京", areaCode: 13 }
    ];

    const groups = groupSpotsByArea(spots, areas);

    assert.deepEqual(
        groups.map((group) => [group.key, group.name, group.spots.map((spot) => spot.id)]),
        [
            ["2", "関東", ["tokyo"]],
            ["4", "関西", ["osaka"]]
        ]);
});

test("groupSpotsByArea puts undefined areas last", () => {
    const groups = groupSpotsByArea(
        [
            { id: "unknown", name: "未定義", areaCode: 999 },
            { id: "nagoya", name: "名古屋", areaCode: 23 }
        ],
        areas);

    assert.deepEqual(groups.map((group) => group.name), ["中部", "未定義地域"]);
    assert.equal(groups[1].key, "undefined");
});

test("getSpotCategoryKey resolves the selected spot category", () => {
    assert.equal(
        getSpotCategoryKey({ id: "tokyo", areaCode: 13 }, areas),
        "2");
    assert.equal(
        getSpotCategoryKey({ id: "unknown", areaCode: 999 }, areas),
        "undefined");
});
