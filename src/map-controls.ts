// Keep navigation centered on Japan while retaining Okinawa and the Ogasawara islands.
export const JapanMapBounds = {
    southWest: [20, 122],
    northEast: [47, 154]
};

export const MapZoomOptions = {
    minZoom: 5,
    maxZoom: 19,
    zoomSnap: 0.25,
    zoomDelta: 0.25,
    maxBoundsViscosity: 1.0
};

export const GsiSeamlessPhotoUrl = "https://cyberjapandata.gsi.go.jp/xyz/seamlessphoto/{z}/{x}/{y}.jpg";
