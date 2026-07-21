export function getDefaultSelectedSourceIndexes(items) {
    return items
        .filter((item) => item.isSelectedByDefault)
        .map((item) => item.sourceIndex);
}

export function getConfirmations(items, selectedSourceIndexes) {
    return items
        .filter((item) => selectedSourceIndexes.has(item.sourceIndex) && item.nearbySpots?.length)
        .map((item) => ({
            sourceIndex: item.sourceIndex,
            nearbySpotIds: item.nearbySpots.map((spot) => spot.id)
        }));
}
