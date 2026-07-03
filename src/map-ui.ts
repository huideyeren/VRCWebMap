import L from "leaflet";

export type MarkerKind = "world" | "place" | "default";

export function getMarkerKind(spot): MarkerKind {
    if (spot.hasVRChatWorld) {
        return "world";
    }

    if (spot.hasPlaceInfo) {
        return "place";
    }

    return "default";
}

export function createSpotIcon(spot) {
    const kind = getMarkerKind(spot);
    return L.divIcon({
        className: "spot-div-icon",
        html: `<span class="spot-pin spot-pin--${kind}" aria-hidden="true"><span></span></span>`,
        iconSize: [30, 38],
        iconAnchor: [15, 36],
        popupAnchor: [0, -34]
    });
}

export function getCurrentPosition(): Promise<GeolocationPosition> {
    if (!navigator.geolocation) {
        return Promise.reject(new Error("このブラウザーは位置情報に対応していません。"));
    }

    return new Promise((resolve, reject) => {
        navigator.geolocation.getCurrentPosition(
            resolve,
            (error) => {
                if (error.code === error.PERMISSION_DENIED) {
                    reject(new Error("現在地へ戻るには、ブラウザーの位置情報を許可してください。"));
                    return;
                }

                if (error.code === error.TIMEOUT) {
                    reject(new Error("現在地を取得できませんでした。少し待ってから再試行してください。"));
                    return;
                }

                reject(new Error("現在地を取得できませんでした。"));
            },
            {
                enableHighAccuracy: false,
                maximumAge: 300000,
                timeout: 5000
            }
        );
    });
}
