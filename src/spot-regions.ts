const UndefinedCategoryKey = "undefined";

type SpotWithAreaCode = {
    areaCode: number;
};

type SpotRegionSpot = SpotWithAreaCode & {
    id: string;
    name: string;
    [key: string]: unknown;
};

type AreaMetadata = {
    areaCode: number;
    category: number | string;
    categoryName: string;
    categoryOrder: number;
};

type SpotRegionGroup = {
    key: string;
    name: string;
    order: number;
    spots: SpotRegionSpot[];
};

/**
 * SpotのareaCodeに対応する地域カテゴリキーを返します。
 * 未定義のareaCodeは専用グループへまとめます。
 */
export function getSpotCategoryKey(
    spot: SpotWithAreaCode,
    areas: AreaMetadata[]) {
    const area = areas.find((candidate) => candidate.areaCode === spot.areaCode);
    return area ? String(area.category) : UndefinedCategoryKey;
}

/**
 * Spot一覧を、backendが返す地域カテゴリ順にグループ化します。
 */
export function groupSpotsByArea(
    spots: SpotRegionSpot[],
    areas: AreaMetadata[]) {
    const areaByCode = new Map(areas.map((area) => [area.areaCode, area]));
    const groups = new Map<string, SpotRegionGroup>();

    for (const spot of spots) {
        const area = areaByCode.get(spot.areaCode);
        const key = area ? String(area.category) : UndefinedCategoryKey;
        const group = groups.get(key) ?? {
            key,
            name: area?.categoryName ?? "未定義地域",
            order: area?.categoryOrder ?? Number.MAX_SAFE_INTEGER,
            spots: []
        };

        group.spots.push(spot);
        groups.set(key, group);
    }

    return [...groups.values()]
        .sort((left, right) =>
            left.order - right.order ||
            left.name.localeCompare(right.name, "ja"));
}
