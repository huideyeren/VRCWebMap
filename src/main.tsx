import L from "leaflet";
import "leaflet/dist/leaflet.css";
import markerIcon2xUrl from "leaflet/dist/images/marker-icon-2x.png?url";
import markerIconUrl from "leaflet/dist/images/marker-icon.png?url";
import markerShadowUrl from "leaflet/dist/images/marker-shadow.png?url";
import React, { useEffect, useMemo, useRef, useState } from "react";
import { createRoot } from "react-dom/client";

type SelectSpotOptions = {
    updateUrl?: boolean;
    center?: boolean;
    screen?: string;
};

const TokyoStation = [35.681236, 139.767125];
const DefaultAreaCode = 13;

L.Icon.Default.mergeOptions({
    iconRetinaUrl: markerIcon2xUrl,
    iconUrl: markerIconUrl,
    shadowUrl: markerShadowUrl
});

function App() {
    const mapElementRef = useRef(null);
    const mapRef = useRef(null);
    const markersRef = useRef(new Map());
    const pendingCenterRef = useRef(null);
    const currentUserRef = useRef(null);
    const [spots, setSpots] = useState([]);
    const [selectedSpot, setSelectedSpot] = useState(null);
    const [selectedDetails, setSelectedDetails] = useState(null);
    const [draft, setDraft] = useState(null);
    const [areas, setAreas] = useState([]);
    const [screen, setScreen] = useState("map");
    const [currentUser, setCurrentUser] = useState(null);
    const [developmentApp, setDevelopmentApp] = useState(null);
    const [developmentUsers, setDevelopmentUsers] = useState([]);
    const [message, setMessage] = useState("");
    const [searchQuery, setSearchQuery] = useState("");
    const [isSaving, setIsSaving] = useState(false);
    const [isDownloadingPortal, setIsDownloadingPortal] = useState(false);
    const spotCount = spots.length;
    const actorUserId = getCurrentUserId(currentUser);
    const registrantName = getUserDisplayName(currentUser);

    useEffect(() => {
        loadCurrentUser().then(setCurrentUser).catch(() => setCurrentUser(null));
        loadDevelopmentApp().then(setDevelopmentApp).catch(() => setDevelopmentApp(null));
        loadDevelopmentUsers().then(setDevelopmentUsers).catch(() => setDevelopmentUsers([]));
        loadAreas().then(setAreas).catch(() => setAreas([]));
        loadInitialMapState().catch((error) => setMessage(error.message));
    }, []);

    useEffect(() => {
        currentUserRef.current = currentUser;
    }, [currentUser, screen]);

    useEffect(() => {
        if (mapRef.current || !mapElementRef.current) {
            return;
        }

        const map = L.map(mapElementRef.current, {
            zoomControl: false
        }).setView(TokyoStation, 6);

        L.control.zoom({ position: "bottomleft" }).addTo(map);
        L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
            maxZoom: 19,
            attribution: "&copy; OpenStreetMap contributors"
        }).addTo(map);

        map.on("contextmenu", (event) => {
            if (!currentUserRef.current?.hasVRChatDisplayName) {
                setMessage(currentUserRef.current
                    ? "Spot の登録にはVRChat表示名の登録が必要です。"
                    : "Spot の登録にはログインが必要です。");
                return;
            }

            const lat = roundCoordinate(event.latlng.lat);
            const lng = roundCoordinate(event.latlng.lng);
            clearLinkedSpotId();
            setDraft({
                name: "",
                description: "",
                latitude: lat,
                longitude: lng,
                areaCode: DefaultAreaCode
            });
            setSelectedSpot(null);
            setSelectedDetails(null);
            setMessage("右クリックした位置に Spot を登録できます。");
        });

        mapRef.current = map;
        applyPendingCenter();

        return () => {
            map.remove();
            mapRef.current = null;
        };
    }, []);

    useEffect(() => {
        const map = mapRef.current;
        if (!map) {
            return;
        }

        for (const marker of markersRef.current.values()) {
            marker.remove();
        }
        markersRef.current.clear();

        for (const spot of spots) {
            const marker = L.marker([spot.latitude, spot.longitude])
                .addTo(map)
                .bindPopup(`<strong>${escapeHtml(spot.name)}</strong><br>${escapeHtml(spot.description)}`);
            marker.on("click", () => selectSpot(spot));
            markersRef.current.set(spot.id, marker);
        }
    }, [spots]);

    const panelTitle = useMemo(() => {
        if (screen === "profile") {
            return "プロフィール設定";
        }

        if (draft) {
            return "Spot を登録";
        }

        if (selectedSpot) {
            return selectedSpot.name;
        }

        return "Spot 詳細";
    }, [draft, screen, selectedSpot]);

    async function loadInitialMapState() {
        setMessage("");
        const loaded = await loadSpots();
        setSpots(loaded);

        const linkedSpotId = getLinkedSpotId();
        if (linkedSpotId) {
            try {
                const details = await getSpot(linkedSpotId);
                setSelectedSpot(details.spot);
                setSelectedDetails(details);
                centerMapOnSpot(details.spot);
                return;
            } catch (error) {
                setMessage(`Spot 直リンクの読み込みに失敗しました: ${error.message}`);
            }
        }

        centerMapOnCurrentPosition();
    }

    async function refreshSpots() {
        setMessage("");
        const loaded = await loadSpots(searchQuery);
        setSpots(loaded);
    }

    async function selectSpot(spot, options: SelectSpotOptions = {}) {
        const shouldUpdateUrl = options.updateUrl ?? true;
        const shouldCenter = options.center ?? true;
        const nextScreen = options.screen ?? "map";
        setSelectedSpot(spot);
        setSelectedDetails(null);
        setDraft(null);
        setMessage("");
        setScreen(nextScreen);

        if (shouldUpdateUrl) {
            setLinkedSpotId(spot.id);
        }

        if (shouldCenter) {
            centerMapOnSpot(spot);
        }

        try {
            const details = await getSpot(spot.id);
            setSelectedDetails(details);
        } catch (error) {
            setSelectedDetails(null);
            setMessage(error.message);
        }
    }

    function centerMapOnSpot(spot) {
        centerMap([spot.latitude, spot.longitude], 15);
    }

    function centerMapOnCurrentPosition() {
        if (!navigator.geolocation) {
            return;
        }

        navigator.geolocation.getCurrentPosition(
            (position) => {
                centerMap([position.coords.latitude, position.coords.longitude], 13);
            },
            () => {
                // 位置情報は任意。拒否や取得失敗時はデフォルト中心のままにする。
            },
            {
                enableHighAccuracy: false,
                maximumAge: 300000,
                timeout: 5000
            });
    }

    function centerMap(center, zoom) {
        const map = mapRef.current;
        if (!map) {
            pendingCenterRef.current = { center, zoom };
            return;
        }

        map.setView(center, zoom);
    }

    function applyPendingCenter() {
        const pending = pendingCenterRef.current;
        if (!pending || !mapRef.current) {
            return;
        }

        mapRef.current.setView(pending.center, pending.zoom);
        pendingCenterRef.current = null;
    }

    async function reloadSelectedSpot(spotId = selectedSpot?.id) {
        if (!spotId) {
            return;
        }

        const details = await getSpot(spotId);
        setSelectedDetails(details);
        setSelectedSpot(details.spot);
    }

    async function reloadAfterSpotMutation(spotId) {
        const loaded = await loadSpots(searchQuery);
        setSpots(loaded);

        if (spotId) {
            await reloadSelectedSpot(spotId);
        }
    }

    async function clearDeletedSpot() {
        setSelectedSpot(null);
        setSelectedDetails(null);
        clearLinkedSpotId();
        await refreshSpots();
    }

    async function saveDraft(event) {
        event.preventDefault();
        if (!draft) {
            return;
        }

        if (!currentUser?.hasVRChatDisplayName) {
            setMessage(currentUser
                ? "Spot の登録にはVRChat表示名の登録が必要です。"
                : "Spot の登録にはログインが必要です。");
            return;
        }

        setIsSaving(true);
        setMessage("");

        try {
            const created = await createSpot({
                registeredByUserId: actorUserId,
                name: draft.name,
                latitude: Number(draft.latitude),
                longitude: Number(draft.longitude),
                areaCode: Number(draft.areaCode),
                description: draft.description
            });
            const loaded = await loadSpots(searchQuery);
            setSpots(loaded);
            setDraft(null);
            setSelectedSpot(created);
            await selectSpot(created);
        } catch (error) {
            setMessage(error.message);
        } finally {
            setIsSaving(false);
        }
    }

    async function searchSpots(event) {
        event.preventDefault();
        setMessage("");
        setDraft(null);
        setSelectedSpot(null);
        setSelectedDetails(null);
        clearLinkedSpotId();

        try {
            const loaded = await loadSpots(searchQuery);
            setSpots(loaded);
            if (searchQuery.trim() && loaded.length === 0) {
                setMessage("検索条件に一致する Spot は見つかりませんでした。");
            }
        } catch (error) {
            setMessage(error.message);
        }
    }

    async function clearSearch() {
        setSearchQuery("");
        setMessage("");
        setDraft(null);
        setSelectedSpot(null);
        setSelectedDetails(null);
        clearLinkedSpotId();

        try {
            setSpots(await loadSpots());
        } catch (error) {
            setMessage(error.message);
        }
    }

    async function logout() {
        setMessage("");

        try {
            await postJson("/auth/logout", {});
            setCurrentUser(null);
            setDraft(null);
            setScreen("map");
            setMessage("ログアウトしました。");
        } catch (error) {
            setMessage(error.message);
        }
    }

    function openProfileScreen() {
        if (!currentUser) {
            return;
        }

        setDraft(null);
        setScreen("profile");
        setMessage("");
    }

    async function downloadPortalData() {
        setIsDownloadingPortal(true);
        setMessage("");

        try {
            const body = await postJson("/portal/world-data", { showPrivateWorld: true });
            const portalData = unwrap(body);
            const blob = new Blob([JSON.stringify(portalData, null, 2)], { type: "application/json" });
            const url = URL.createObjectURL(blob);
            const anchor = document.createElement("a");
            anchor.href = url;
            anchor.download = "WorldData.json";
            document.body.append(anchor);
            anchor.click();
            anchor.remove();
            URL.revokeObjectURL(url);
            setMessage("PortalLibrarySystem 用 WorldData.json をダウンロードしました。");
        } catch (error) {
            setMessage(error.message);
        } finally {
            setIsDownloadingPortal(false);
        }
    }

    return React.createElement("div", { className: "app-shell" },
        React.createElement("nav", { className: "top-menu" },
            React.createElement("div", null,
                React.createElement("p", { className: "eyebrow" }, "VRC Web Map"),
                React.createElement("strong", null, "Map Console")
            ),
            React.createElement(SearchPanel, {
                query: searchQuery,
                resultCount: spotCount,
                onChange: setSearchQuery,
                onSubmit: searchSpots,
                onClear: clearSearch,
                compact: true
            }),
            React.createElement("div", { className: "top-menu-controls" },
                currentUser ? React.createElement("span", { className: "user-chip" }, getUserDisplayName(currentUser)) : null,
                React.createElement("details", { className: "hamburger-menu" },
                    React.createElement("summary", { "aria-label": "メニューを開く" },
                        React.createElement("span", { className: "hamburger-icon", "aria-hidden": "true" },
                            React.createElement("span", null),
                            React.createElement("span", null),
                            React.createElement("span", null)
                        ),
                        React.createElement("span", null, "メニュー")
                    ),
                    React.createElement("div", { className: "menu-panel" },
                        React.createElement("a", { className: "menu-button secondary", href: "/guide.html" }, "使い方"),
                        React.createElement("a", { className: "menu-button secondary", href: "/terms.html" }, "利用規約"),
                        React.createElement("a", { className: "menu-button secondary", href: "/privacy.html" }, "プライバシーポリシー"),
                        React.createElement("hr", null),
                        currentUser
                            ? React.createElement(React.Fragment, null,
                                React.createElement("button", {
                                    type: "button",
                                    className: screen === "profile" ? "" : "secondary",
                                    onClick: openProfileScreen
                                }, "プロフィール設定"),
                                currentUser.isAdmin ? React.createElement("a", {
                                    className: "menu-button secondary",
                                    href: "/admin.html"
                                }, "管理用画面") : null,
                                React.createElement("button", {
                                    type: "button",
                                    className: "secondary",
                                    onClick: logout
                                }, "ログアウト")
                            )
                            : React.createElement(React.Fragment, null,
                                React.createElement("a", { className: "menu-button", href: "/auth/discord/login" }, "Discord ログイン"),
                                developmentUsers.map((user) =>
                                    React.createElement("a", {
                                        key: user.userId,
                                        className: user.isAdmin ? "menu-button" : "menu-button secondary",
                                        href: user.loginUrl
                                    }, user.isAdmin ? "開発: 管理者" : "開発: 一般")
                                )
                            ),
                        developmentApp ? React.createElement(React.Fragment, null,
                            React.createElement("hr", null),
                            React.createElement("a", {
                                className: "menu-button secondary",
                                href: developmentApp.swaggerUrl,
                                target: "_blank",
                                rel: "noopener noreferrer"
                            }, "Swagger")
                        ) : null,
                        React.createElement("hr", null),
                        React.createElement("button", {
                            type: "button",
                            className: "secondary",
                            onClick: downloadPortalData,
                            disabled: isDownloadingPortal
                        }, isDownloadingPortal ? "生成中..." : "WorldData.json ダウンロード")
                    )
                )
            )
        ),
        React.createElement("section", { className: "map-frame" },
            React.createElement("div", { className: "map-brand" },
                React.createElement("p", { className: "eyebrow" }, "VRC Web Map"),
                React.createElement("h1", null, "Spot Atlas"),
                React.createElement("p", { className: "hint" }, "地図を右クリックして Spot を登録。marker をクリックすると右ペインに詳細を表示します。")
            ),
            React.createElement("div", { id: "map", ref: mapElementRef, "aria-label": "Spot map" })
        ),
        React.createElement("aside", { className: "side-panel" },
            React.createElement("header", { className: "panel-header" },
                React.createElement("p", { className: "eyebrow" }, `${spotCount} spots`),
                React.createElement("h2", null, panelTitle),
                React.createElement("p", { className: "meta" }, currentUser ? `ログイン中: ${getUserDisplayName(currentUser)}` : "未ログイン: 書き込みにはログインが必要です。")
            ),
            React.createElement("div", { className: "panel-body" },
                message ? React.createElement("p", { className: "notice", role: "status" }, message) : null,
                screen === "profile" ? React.createElement(ProfileSettings, {
                    currentUser,
                    onUpdated: async () => {
                        const user = await loadCurrentUser();
                        setCurrentUser(user);
                        setScreen("map");
                        setMessage("VRChat表示名を保存しました。");
                    },
                    onCancel: () => setScreen("map"),
                    onMessage: setMessage
                }) : null,
                screen !== "profile" && draft ? React.createElement(SpotForm, {
                    draft,
                    areas,
                    isSaving,
                    onChange: setDraft,
                    onSubmit: saveDraft,
                    onCancel: () => {
                        setDraft(null);
                        setMessage("");
                    }
                }) : null,
                screen !== "profile" && !draft && selectedSpot ? React.createElement(SpotDetails, {
                    spot: selectedSpot,
                    details: selectedDetails,
                    areas,
                    currentUser,
                    registeredByUserId: actorUserId,
                    registrantName,
                    onCreated: reloadSelectedSpot,
                    onSpotUpdated: reloadAfterSpotMutation,
                    onSpotDeleted: clearDeletedSpot,
                    onMessage: setMessage
                }) : null,
                screen !== "profile" && !draft && !selectedSpot ? React.createElement(EmptyState, {
                    onReload: refreshSpots,
                    currentUser,
                    onOpenProfile: openProfileScreen
                }) : null,
                screen !== "profile" ? React.createElement(SpotList, { spots, selectedSpotId: selectedSpot?.id, onSelect: selectSpot }) : null
            )
        )
    );
}

function AdminScreen({ spots, selectedSpot, selectedDetails, areas, currentUser, onSelectSpot, onChanged, onDeleted, onBack, onReload, onMessage }) {
    if (!currentUser?.isAdmin) {
        return React.createElement("section", { className: "card" },
            React.createElement("h3", null, "管理用画面"),
            React.createElement("p", { className: "meta" }, "管理用画面を開くには管理者ログインが必要です。")
        );
    }

    const worlds = selectedDetails?.vrChatWorlds ?? [];
    const placeInfos = selectedDetails?.placeInfos ?? [];
    const webLinks = selectedDetails?.webLinks ?? [];
    const comments = selectedDetails?.comments ?? [];

    return React.createElement("section", { className: "admin-screen" },
        React.createElement("div", { className: "actions" },
            React.createElement("button", { type: "button", className: "secondary", onClick: onBack }, "通常画面へ戻る"),
            React.createElement("button", { type: "button", className: "secondary", onClick: onReload }, "Spot を再読み込み")
        ),
        React.createElement(KmlImportPanel, {
            areas,
            currentUser,
            onImported: onReload,
            onMessage
        }),
        React.createElement("div", { className: "admin-card" },
            React.createElement("h4", null, "管理対象 Spot"),
            spots.length === 0
                ? React.createElement("p", { className: "meta" }, "登録済み Spot はありません。")
                : React.createElement("div", { className: "related-list" }, spots.map((spot) =>
                    React.createElement("button", {
                        key: spot.id,
                        type: "button",
                        className: spot.id === selectedSpot?.id ? "" : "secondary",
                        onClick: () => onSelectSpot(spot)
                    }, `${spot.name} / ${formatAreaName(spot.areaCode, areas)}`)
                ))
        ),
        selectedSpot
            ? selectedDetails
                ? React.createElement(AdminPanel, {
                    spot: selectedDetails.spot,
                    worlds,
                    placeInfos,
                    webLinks,
                    comments,
                    areas,
                    currentUser,
                    actorUserId: getCurrentUserId(currentUser),
                    onChanged,
                    onDeleted,
                    onMessage
                })
                : React.createElement("p", { className: "meta" }, "Spot 詳細を読み込み中です。")
            : React.createElement("p", { className: "meta" }, "編集する Spot を選択してください。")
    );
}

function KmlImportPanel({ areas, currentUser, onImported, onMessage }) {
    const [file, setFile] = useState(null);
    const [defaultAreaCode, setDefaultAreaCode] = useState(DefaultAreaCode);
    const [preview, setPreview] = useState(null);
    const [isPreviewing, setIsPreviewing] = useState(false);
    const [isImporting, setIsImporting] = useState(false);

    const candidateCount = preview?.items?.length ?? 0;

    async function buildPayload() {
        if (!file) {
            throw new Error("KML または KMZ ファイルを選択してください。");
        }

        return {
            actorUserId: getCurrentUserId(currentUser),
            actorIsAdmin: true,
            fileName: file.name,
            contentBase64: await readFileAsBase64(file),
            defaultAreaCode: Number(defaultAreaCode)
        };
    }

    async function previewFile(event) {
        event.preventDefault();
        setIsPreviewing(true);
        setPreview(null);
        onMessage("");

        try {
            const nextPreview = await previewKmlImport(await buildPayload());
            setPreview(nextPreview);
            onMessage(`${nextPreview.items?.length ?? 0} 件の Spot 候補を読み込みました。`);
        } catch (error) {
            onMessage(error.message);
        } finally {
            setIsPreviewing(false);
        }
    }

    async function importFile() {
        setIsImporting(true);
        onMessage("");

        try {
            const result = await importKmlSpots(await buildPayload());
            setPreview(null);
            setFile(null);
            await onImported();
            onMessage(`${result.spots?.length ?? 0} 件の Spot を import しました。`);
        } catch (error) {
            onMessage(error.message);
        } finally {
            setIsImporting(false);
        }
    }

    return React.createElement("form", { className: "admin-card kml-import-panel", onSubmit: previewFile },
        React.createElement("h4", null, "KML/KMZ import"),
        React.createElement("p", { className: "meta" }, "Google My Maps などから export した KML/KMZ の Point Placemark を Spot 候補として読み込みます。座標は WGS84 の longitude,latitude として扱います。"),
        React.createElement("label", null,
            "KML/KMZ ファイル",
            React.createElement("input", {
                type: "file",
                accept: ".kml,.kmz,application/vnd.google-earth.kml+xml,application/vnd.google-earth.kmz",
                onChange: (event) => {
                    setFile(event.target.files?.[0] ?? null);
                    setPreview(null);
                }
            })
        ),
        React.createElement("label", null,
            "既定エリア",
            React.createElement("select", {
                value: defaultAreaCode,
                onChange: (event) => {
                    setDefaultAreaCode(Number(event.target.value));
                    setPreview(null);
                }
            } as React.SelectHTMLAttributes<HTMLSelectElement>,
                areas.map((area) => React.createElement("option", { key: area.areaCode, value: area.areaCode }, area.areaName))
            )
        ),
        React.createElement("div", { className: "actions" },
            React.createElement("button", { type: "submit", disabled: isPreviewing || isImporting }, isPreviewing ? "解析中..." : "Preview"),
            React.createElement("button", { type: "button", className: "secondary", onClick: importFile, disabled: isPreviewing || isImporting || candidateCount === 0 }, isImporting ? "Import 中..." : "Import")
        ),
        preview ? React.createElement("div", { className: "kml-preview" },
            React.createElement("p", { className: "meta" }, `候補: ${candidateCount} 件 / 未対応 Placemark: ${preview.unsupportedPlacemarkCount ?? 0} 件`),
            preview.warnings?.length ? React.createElement("ul", { className: "warning-list" },
                preview.warnings.map((warning, index) => React.createElement("li", { key: index }, warning))
            ) : null,
            candidateCount === 0
                ? React.createElement("p", { className: "meta" }, "import 可能な Point Placemark はありません。")
                : React.createElement("div", { className: "related-list" }, preview.items.slice(0, 10).map((item, index) =>
                    React.createElement("div", { key: `${item.name}-${index}`, className: "related-item" },
                        React.createElement("strong", null, item.name),
                        React.createElement("p", { className: "meta" }, `${formatCoordinate(item.latitude)}, ${formatCoordinate(item.longitude)} / ${formatAreaName(item.areaCode, areas)}`),
                        item.warnings?.length ? React.createElement("ul", { className: "warning-list" },
                            item.warnings.map((warning, warningIndex) => React.createElement("li", { key: warningIndex }, warning))
                        ) : null
                    )
                )),
            candidateCount > 10 ? React.createElement("p", { className: "meta" }, `ほか ${candidateCount - 10} 件`) : null
        ) : null
    );
}

function SpotForm({ draft, areas, isSaving, onChange, onSubmit, onCancel, submitLabel = "Spot を登録" }) {
    const update = (key) => (event) => onChange({ ...draft, [key]: event.target.value });
    const selectedAreaCode = Number(draft.areaCode);
    const hasSelectedArea = areas.some((area) => area.areaCode === selectedAreaCode);

    return React.createElement("form", { className: "card form-grid", onSubmit },
        React.createElement("label", null,
            "Spot 名",
            React.createElement("input", {
                value: draft.name,
                onChange: update("name"),
                required: true,
                maxLength: 200,
                placeholder: "例: 秋葉原イベント会場"
            })
        ),
        React.createElement("label", null,
            "説明 Markdown",
            React.createElement("textarea", {
                value: draft.description,
                onChange: update("description"),
                required: true,
                rows: 4,
                placeholder: "例: ## 見出し\n- VRChat ワールド\n- 飲食店情報"
            })
        ),
        React.createElement("div", { className: "two-column" },
            React.createElement("label", null,
                "緯度",
                React.createElement("input", {
                    type: "number",
                    step: "0.000001",
                    min: "-90",
                    max: "90",
                    value: draft.latitude,
                    onChange: update("latitude"),
                    required: true
                })
            ),
            React.createElement("label", null,
                "経度",
                React.createElement("input", {
                    type: "number",
                    step: "0.000001",
                    min: "-180",
                    max: "180",
                    value: draft.longitude,
                    onChange: update("longitude"),
                    required: true
                })
            )
        ),
        React.createElement("label", null,
            "都道府県/地域",
            React.createElement("select", {
                value: draft.areaCode,
                onChange: update("areaCode"),
                required: true
            },
                !hasSelectedArea
                    ? React.createElement("option", { value: draft.areaCode }, formatAreaName(draft.areaCode, areas))
                    : null,
                areas.map((area) => React.createElement("option", { key: area.areaCode, value: area.areaCode }, area.areaName))
            )
        ),
        React.createElement("div", { className: "actions" },
            React.createElement("button", { type: "submit", disabled: isSaving }, isSaving ? "保存中..." : submitLabel),
            React.createElement("button", { type: "button", className: "secondary", onClick: onCancel }, "キャンセル")
        )
    );
}

function SpotDetails({ spot, details, areas, currentUser, registeredByUserId, registrantName, onCreated, onSpotUpdated, onSpotDeleted, onMessage }) {
    const worlds = details?.vrChatWorlds ?? [];
    const placeInfos = details?.placeInfos ?? [];
    const webLinks = details?.webLinks ?? [];
    const comments = details?.comments ?? [];

    return React.createElement("section", { className: "card" },
        React.createElement("h3", null, spot.name),
        renderMarkdown(spot.description),
        React.createElement("p", { className: "meta" }, `座標: ${formatCoordinate(spot.latitude)}, ${formatCoordinate(spot.longitude)}`),
        React.createElement("p", { className: "meta" }, `地域: ${formatAreaName(spot.areaCode, areas)}`),
        currentUser?.hasVRChatDisplayName
            ? React.createElement(AddContentForms, { spot, registeredByUserId, registrantName, onCreated, onMessage })
            : currentUser
                ? React.createElement(ProfileRequiredNotice)
                : React.createElement(LoginRequiredNotice),
        currentUser?.hasVRChatDisplayName && canEditSelectedDetails(currentUser, spot, worlds, placeInfos, webLinks, comments) ? React.createElement(AdminPanel, {
            spot,
            worlds,
            placeInfos,
            webLinks,
            comments,
            areas,
            currentUser,
            actorUserId: getCurrentUserId(currentUser),
            onChanged: onSpotUpdated,
            onDeleted: onSpotDeleted,
            onMessage
        }) : null,
        React.createElement(RelatedSection, { title: "VRChat Worlds", items: worlds, render: renderWorld }),
        React.createElement(RelatedSection, { title: "Place Infos", items: placeInfos, render: renderPlaceInfo }),
        React.createElement(RelatedSection, { title: "Web Links", items: webLinks, render: renderWebLink }),
        React.createElement(RelatedSection, { title: "Comments", items: comments, render: renderComment })
    );
}

function LoginRequiredNotice() {
    return React.createElement("div", { className: "content-form" },
        React.createElement("p", { className: "meta" }, "関連情報の追加にはログインが必要です。"),
        React.createElement("a", { className: "menu-button", href: "/auth/discord/login" }, "Discord ログイン")
    );
}

function ProfileRequiredNotice() {
    return React.createElement("div", { className: "content-form" },
        React.createElement("strong", null, "VRChat表示名を登録してください"),
        React.createElement("p", { className: "meta" }, "投稿・編集を行う前に、メニューの「プロフィール設定」からVRChatのDisplay Nameを登録してください。")
    );
}

function ProfileSettings({ currentUser, onUpdated, onCancel, onMessage }) {
    const [displayName, setDisplayName] = useState(currentUser?.vrChatDisplayName ?? "");
    const [isSaving, setIsSaving] = useState(false);

    async function submit(event) {
        event.preventDefault();
        setIsSaving(true);
        onMessage("");

        try {
            await postJson("/users/profile", { vrChatDisplayName: displayName });
            await onUpdated();
        } catch (error) {
            onMessage(error.message);
        } finally {
            setIsSaving(false);
        }
    }

    return React.createElement("section", { className: "card profile-settings" },
        React.createElement("p", { className: "eyebrow" }, "Public identity"),
        React.createElement("h3", null, "VRChat表示名"),
        React.createElement("p", { className: "meta" }, `Discord: ${currentUser?.username ?? ""}`),
        React.createElement("p", { className: "meta" }, "VRChat内で頭上に表示されるDisplay Nameを入力してください。4〜15文字で、他の利用者と同じ名前は登録できません。"),
        React.createElement("form", { className: "form-grid", onSubmit: submit },
            React.createElement("label", null,
                "VRChat Display Name",
                React.createElement("input", {
                    value: displayName,
                    minLength: 4,
                    maxLength: 15,
                    required: true,
                    autoComplete: "off",
                    onChange: (event) => setDisplayName(event.target.value)
                })
            ),
            React.createElement("div", { className: "actions" },
                React.createElement("button", { type: "submit", disabled: isSaving }, isSaving ? "保存中..." : "表示名を保存"),
                React.createElement("button", { type: "button", className: "secondary", onClick: onCancel }, "戻る")
            )
        )
    );
}

function RelatedSection({ title, items, render }) {
    return React.createElement("div", { className: "related-list" },
        React.createElement("h3", null, title),
        items.length === 0
            ? React.createElement("p", { className: "meta" }, "まだ登録されていません。")
            : items.map((item) => React.createElement("div", { key: item.id, className: "related-item" }, render(item)))
    );
}

function getCurrentUserId(user) {
    return user?.discordUserId ?? user?.userId ?? "";
}

function getUserDisplayName(user) {
    return user?.vrChatDisplayName ?? user?.displayName ?? user?.username ?? "";
}

function canEditItem(item, user) {
    const userId = getCurrentUserId(user);
    return Boolean(user?.isAdmin || (userId && item?.registeredByUserId === userId));
}

function editableItems(items, user) {
    return user?.isAdmin ? items : items.filter((item) => canEditItem(item, user));
}

function canEditSelectedDetails(user, spot, worlds, placeInfos, webLinks, comments) {
    return Boolean(user && (
        canEditItem(spot, user) ||
        worlds.some((item) => canEditItem(item, user)) ||
        placeInfos.some((item) => canEditItem(item, user)) ||
        webLinks.some((item) => canEditItem(item, user)) ||
        comments.some((item) => canEditItem(item, user))
    ));
}

function AddContentForms({ spot, registeredByUserId, registrantName, onCreated, onMessage }) {
    const [kind, setKind] = useState("world");
    const [isSaving, setIsSaving] = useState(false);
    const [world, setWorld] = useState(createEmptyWorld());
    const [placeInfo, setPlaceInfo] = useState(createEmptyPlaceInfo());
    const [webLink, setWebLink] = useState(createEmptyWebLink());
    const [comment, setComment] = useState("");

    async function submit(event) {
        event.preventDefault();
        setIsSaving(true);
        onMessage("");

        try {
            if (kind === "world") {
                await createVRChatWorld({
                    spotId: spot.id,
                    registeredByUserId,
                    vrChatWorldId: normalizeWorldId(world.vrChatWorldId),
                    name: world.name,
                    recommendedCapacity: Number(world.recommendedCapacity),
                    capacity: Number(world.capacity),
                    description: world.description,
                    pc: world.pc,
                    android: world.android,
                    ios: world.ios,
                    isPrivate: world.isPrivate
                });
                setWorld(createEmptyWorld());
            } else if (kind === "placeInfo") {
                await createPlaceInfo({
                    spotId: spot.id,
                    registeredByUserId,
                    name: placeInfo.name,
                    address: placeInfo.address,
                    businessInformation: placeInfo.businessInformation
                });
                setPlaceInfo(createEmptyPlaceInfo());
            } else if (kind === "webLink") {
                await createWebLink({
                    spotId: spot.id,
                    registeredByUserId,
                    siteName: webLink.siteName,
                    url: webLink.url
                });
                setWebLink(createEmptyWebLink());
            } else {
                await createComment({
                    spotId: spot.id,
                    registeredByUserId,
                    comments: comment
                });
                setComment("");
            }

            await onCreated(spot.id);
            onMessage("追加しました。");
        } catch (error) {
            onMessage(error.message);
        } finally {
            setIsSaving(false);
        }
    }

    return React.createElement("form", { className: "content-form", onSubmit: submit },
        React.createElement("div", { className: "segmented" },
            React.createElement("button", { type: "button", className: kind === "world" ? "" : "secondary", onClick: () => setKind("world") }, "VRChat World"),
            React.createElement("button", { type: "button", className: kind === "placeInfo" ? "" : "secondary", onClick: () => setKind("placeInfo") }, "場所情報"),
            React.createElement("button", { type: "button", className: kind === "webLink" ? "" : "secondary", onClick: () => setKind("webLink") }, "Webリンク"),
            React.createElement("button", { type: "button", className: kind === "comment" ? "" : "secondary", onClick: () => setKind("comment") }, "コメント")
        ),
        kind === "world" ? React.createElement(WorldFields, { value: world, onChange: setWorld }) : null,
        kind === "placeInfo" ? React.createElement(PlaceInfoFields, { value: placeInfo, onChange: setPlaceInfo }) : null,
        kind === "webLink" ? React.createElement(WebLinkFields, { value: webLink, onChange: setWebLink }) : null,
        kind === "comment" ? React.createElement(CommentFields, { value: comment, onChange: setComment }) : null,
        React.createElement("p", { className: "meta" }, `追加ユーザー: ${registrantName}`),
        React.createElement("button", { type: "submit", disabled: isSaving }, isSaving ? "追加中..." : "右ペインに追加")
    );
}

function AdminPanel({ spot, worlds, placeInfos, webLinks, comments, areas, currentUser, actorUserId, onChanged, onDeleted, onMessage }) {
    const actor = { actorUserId, actorIsAdmin: currentUser.isAdmin };
    const canDelete = currentUser.isAdmin;
    const editableWorlds = editableItems(worlds, currentUser);
    const editablePlaceInfos = editableItems(placeInfos, currentUser);
    const editableWebLinks = editableItems(webLinks, currentUser);
    const editableComments = editableItems(comments, currentUser);

    return React.createElement("section", { className: "admin-panel" },
        React.createElement("p", { className: "eyebrow" }, currentUser.isAdmin ? "Admin edit" : "Owner edit"),
        React.createElement("h3", null, currentUser.isAdmin ? "管理者編集" : "登録者編集"),
        React.createElement("p", { className: "meta" }, currentUser.isAdmin
            ? "管理者として Spot と関連データを編集・削除できます。"
            : "あなたが登録した Spot と関連データを編集できます。削除は管理者のみ可能です。"),
        canEditItem(spot, currentUser) ? React.createElement(AdminSpotEditor, { spot, areas, actor, canDelete, onChanged, onDeleted, onMessage }) : null,
        React.createElement(AdminWorldSection, { items: editableWorlds, actor, canDelete, onChanged: () => onChanged(spot.id), onMessage }),
        React.createElement(AdminPlaceInfoSection, { items: editablePlaceInfos, actor, canDelete, onChanged: () => onChanged(spot.id), onMessage }),
        React.createElement(AdminWebLinkSection, { items: editableWebLinks, actor, canDelete, onChanged: () => onChanged(spot.id), onMessage }),
        React.createElement(AdminCommentSection, { items: editableComments, actor, canDelete, onChanged: () => onChanged(spot.id), onMessage })
    );
}

function AdminSpotEditor({ spot, areas, actor, canDelete, onChanged, onDeleted, onMessage }) {
    const [draft, setDraft] = useState(createSpotDraft(spot));
    const [isSaving, setIsSaving] = useState(false);

    useEffect(() => {
        setDraft(createSpotDraft(spot));
    }, [spot]);

    async function submit(event) {
        event.preventDefault();
        setIsSaving(true);
        onMessage("");

        try {
            const updated = await updateSpot({
                id: spot.id,
                ...actor,
                name: draft.name,
                latitude: Number(draft.latitude),
                longitude: Number(draft.longitude),
                areaCode: Number(draft.areaCode),
                description: draft.description
            });
            await onChanged(updated.id);
            onMessage("Spot を更新しました。");
        } catch (error) {
            onMessage(error.message);
        } finally {
            setIsSaving(false);
        }
    }

    async function remove() {
        if (!confirm("この Spot を削除します。関連データが残っている場合は削除できません。よろしいですか？")) {
            return;
        }

        setIsSaving(true);
        onMessage("");

        try {
            await deleteSpot({ id: spot.id, ...actor });
            await onDeleted();
            onMessage("Spot を削除しました。");
        } catch (error) {
            onMessage(error.message);
        } finally {
            setIsSaving(false);
        }
    }

    return React.createElement("div", { className: "admin-card" },
        React.createElement("h4", null, "Spot"),
        React.createElement(SpotForm, {
            draft,
            areas,
            isSaving,
            onChange: setDraft,
            onSubmit: submit,
            onCancel: () => setDraft(createSpotDraft(spot)),
            submitLabel: "Spot を更新"
        }),
        canDelete ? React.createElement("button", { type: "button", className: "danger", onClick: remove, disabled: isSaving }, "Spot を削除") : null
    );
}

function AdminWorldSection({ items, actor, canDelete, onChanged, onMessage }) {
    return React.createElement(AdminEditableSection, {
        title: "VRChat Worlds",
        items,
        getLabel: (world) => world.name,
        createDraft: createWorldDraft,
        renderForm: (draft, setDraft) => React.createElement(WorldFields, { value: draft, onChange: setDraft }),
        onUpdate: (world, draft) => updateVRChatWorld({
            id: world.id,
            ...actor,
            vrChatWorldId: normalizeWorldId(draft.vrChatWorldId),
            name: draft.name,
            recommendedCapacity: Number(draft.recommendedCapacity),
            capacity: Number(draft.capacity),
            description: draft.description,
            pc: draft.pc,
            android: draft.android,
            ios: draft.ios,
            isPrivate: draft.isPrivate
        }),
        onDelete: (world) => deleteVRChatWorld({ id: world.id, ...actor }),
        canDelete,
        onChanged,
        onMessage
    });
}

function AdminPlaceInfoSection({ items, actor, canDelete, onChanged, onMessage }) {
    return React.createElement(AdminEditableSection, {
        title: "Place Infos",
        items,
        getLabel: (placeInfo) => placeInfo.name,
        createDraft: createPlaceInfoDraft,
        renderForm: (draft, setDraft) => React.createElement(PlaceInfoFields, { value: draft, onChange: setDraft }),
        onUpdate: (placeInfo, draft) => updatePlaceInfo({
            id: placeInfo.id,
            ...actor,
            name: draft.name,
            address: draft.address,
            businessInformation: draft.businessInformation
        }),
        onDelete: (placeInfo) => deletePlaceInfo({ id: placeInfo.id, ...actor }),
        canDelete,
        onChanged,
        onMessage
    });
}

function AdminWebLinkSection({ items, actor, canDelete, onChanged, onMessage }) {
    return React.createElement(AdminEditableSection, {
        title: "Web Links",
        items,
        getLabel: (webLink) => webLink.siteName,
        createDraft: createWebLinkDraft,
        renderForm: (draft, setDraft) => React.createElement(WebLinkFields, { value: draft, onChange: setDraft }),
        onUpdate: (webLink, draft) => updateWebLink({
            id: webLink.id,
            ...actor,
            siteName: draft.siteName,
            url: draft.url
        }),
        onDelete: (webLink) => deleteWebLink({ id: webLink.id, ...actor }),
        canDelete,
        onChanged,
        onMessage
    });
}

function AdminCommentSection({ items, actor, canDelete, onChanged, onMessage }) {
    return React.createElement(AdminEditableSection, {
        title: "Comments",
        items,
        getLabel: (comment) => comment.comments.slice(0, 40) || comment.id,
        createDraft: (comment) => ({ comments: comment.comments }),
        renderForm: (draft, setDraft) => React.createElement(CommentFields, {
            value: draft.comments,
            onChange: (comments) => setDraft({ comments })
        }),
        onUpdate: (comment, draft) => updateComment({
            id: comment.id,
            ...actor,
            comments: draft.comments
        }),
        onDelete: (comment) => deleteComment({ id: comment.id, ...actor }),
        canDelete,
        onChanged,
        onMessage
    });
}

function AdminEditableSection({ title, items, getLabel, createDraft, renderForm, onUpdate, onDelete, canDelete, onChanged, onMessage }) {
    const [editingId, setEditingId] = useState(null);
    const [draft, setDraft] = useState(null);
    const [isSaving, setIsSaving] = useState(false);

    function beginEdit(item) {
        setEditingId(item.id);
        setDraft(createDraft(item));
    }

    async function submit(event, item) {
        event.preventDefault();
        setIsSaving(true);
        onMessage("");

        try {
            await onUpdate(item, draft);
            setEditingId(null);
            setDraft(null);
            await onChanged();
            onMessage(`${title} を更新しました。`);
        } catch (error) {
            onMessage(error.message);
        } finally {
            setIsSaving(false);
        }
    }

    async function remove(item) {
        if (!confirm(`${getLabel(item)} を削除します。よろしいですか？`)) {
            return;
        }

        setIsSaving(true);
        onMessage("");

        try {
            await onDelete(item);
            await onChanged();
            onMessage(`${title} を削除しました。`);
        } catch (error) {
            onMessage(error.message);
        } finally {
            setIsSaving(false);
        }
    }

    return React.createElement("div", { className: "admin-card" },
        React.createElement("h4", null, title),
        items.length === 0
            ? React.createElement("p", { className: "meta" }, "編集可能な対象データはありません。")
            : items.map((item) => React.createElement("div", { key: item.id, className: "admin-row" },
                editingId === item.id
                    ? React.createElement("form", { className: "content-form", onSubmit: (event) => submit(event, item) } as React.FormHTMLAttributes<HTMLFormElement>,
                        renderForm(draft, setDraft),
                        React.createElement("div", { className: "actions" },
                            React.createElement("button", { type: "submit", disabled: isSaving }, isSaving ? "保存中..." : "更新"),
                            React.createElement("button", { type: "button", className: "secondary", onClick: () => setEditingId(null), disabled: isSaving }, "キャンセル")
                        )
                    )
                    : React.createElement(React.Fragment, null,
                        React.createElement("p", { className: "meta" }, getLabel(item)),
                        React.createElement("div", { className: "actions" },
                            React.createElement("button", { type: "button", className: "secondary", onClick: () => beginEdit(item), disabled: isSaving }, "編集"),
                            canDelete ? React.createElement("button", { type: "button", className: "danger", onClick: () => remove(item), disabled: isSaving }, "削除") : null
                        )
                    )
            ))
    );
}

function WorldFields({ value, onChange }) {
    const update = (key) => (event) => onChange({ ...value, [key]: event.target.type === "checkbox" ? event.target.checked : event.target.value });

    return React.createElement("div", { className: "form-grid" },
        React.createElement("label", null, "VRChat World ID または URL", React.createElement("input", { value: value.vrChatWorldId, onChange: update("vrChatWorldId"), required: true, placeholder: "wrld_... または https://vrchat.com/home/world/wrld_.../info" })),
        React.createElement("label", null, "ワールド名", React.createElement("input", { value: value.name, onChange: update("name"), required: true })),
        React.createElement("div", { className: "two-column" },
            React.createElement("label", null, "推奨人数", React.createElement("input", { type: "number", min: "0", value: value.recommendedCapacity, onChange: update("recommendedCapacity"), required: true })),
            React.createElement("label", null, "最大人数", React.createElement("input", { type: "number", min: "0", value: value.capacity, onChange: update("capacity"), required: true }))
        ),
        React.createElement("label", null, "説明", React.createElement("textarea", { value: value.description, onChange: update("description"), required: true, rows: 3 })),
        React.createElement("div", { className: "check-row" },
            React.createElement("label", null, React.createElement("input", { type: "checkbox", checked: value.pc, onChange: update("pc") }), "PC"),
            React.createElement("label", null, React.createElement("input", { type: "checkbox", checked: value.android, onChange: update("android") }), "Android"),
            React.createElement("label", null, React.createElement("input", { type: "checkbox", checked: value.ios, onChange: update("ios") }), "iOS"),
            React.createElement("label", null, React.createElement("input", { type: "checkbox", checked: value.isPrivate, onChange: update("isPrivate") }), "Private")
        )
    );
}

function PlaceInfoFields({ value, onChange }) {
    const update = (key) => (event) => onChange({ ...value, [key]: event.target.value });

    return React.createElement("div", { className: "form-grid" },
        React.createElement("label", null, "場所名", React.createElement("input", { value: value.name, onChange: update("name"), required: true })),
        React.createElement("label", null, "所在地", React.createElement("input", { value: value.address, onChange: update("address"), required: true })),
        React.createElement("label", null, "営業情報 Markdown", React.createElement("textarea", { value: value.businessInformation, onChange: update("businessInformation"), required: true, rows: 4, placeholder: "- 昼: 11:00-14:00\n- 夜: 17:00-22:00\n- 定休日: 火曜" }))
    );
}

function WebLinkFields({ value, onChange }) {
    const update = (key) => (event) => onChange({ ...value, [key]: event.target.value });

    return React.createElement("div", { className: "form-grid" },
        React.createElement("label", null, "サイト名", React.createElement("input", { value: value.siteName, onChange: update("siteName"), required: true, placeholder: "公式サイト / 食べログ / X など" })),
        React.createElement("label", null, "URL", React.createElement("input", { type: "url", value: value.url, onChange: update("url"), required: true }))
    );
}

function CommentFields({ value, onChange }) {
    return React.createElement("label", null,
        "コメント Markdown",
        React.createElement("textarea", { value, onChange: (event) => onChange(event.target.value), required: true, rows: 4 } as React.TextareaHTMLAttributes<HTMLTextAreaElement>)
    );
}

function SearchPanel({ query, resultCount, onChange, onSubmit, onClear, compact = false }) {
    return React.createElement("form", { className: compact ? "top-search-form" : "card search-card", onSubmit },
        React.createElement("label", { className: compact ? "top-search-label" : undefined },
            "Spot 検索",
            React.createElement("input", {
                type: "search",
                value: query,
                onChange: (event) => onChange(event.target.value),
                placeholder: "スポット名・説明を検索"
            })
        ),
        compact
            ? React.createElement("span", { className: "top-search-count", "aria-live": "polite" }, query.trim() ? `${resultCount} 件` : `${resultCount} spots`)
            : React.createElement("p", { className: "meta" },
                query.trim()
                    ? `検索結果: ${resultCount} 件`
                    : `表示中: ${resultCount} 件`
            ),
        React.createElement("div", { className: compact ? "top-search-actions" : "actions" },
            React.createElement("button", { type: "submit" }, "検索"),
            React.createElement("button", { type: "button", className: "secondary", onClick: onClear, disabled: !query.trim() }, "クリア")
        )
    );
}

function EmptyState({ onReload, currentUser, onOpenProfile }) {
    return React.createElement("section", { className: "card" },
        React.createElement("h3", null, "Spot を選択してください"),
        React.createElement("p", { className: "meta" }, "地図上の marker をクリックすると詳細が表示されます。新しい Spot は地図を右クリックして登録します。"),
        currentUser && !currentUser.hasVRChatDisplayName
            ? React.createElement("button", { type: "button", onClick: onOpenProfile }, "VRChat表示名を登録")
            : null,
        React.createElement("button", { type: "button", className: "secondary", onClick: onReload }, "Spot を再読み込み")
    );
}

function SpotList({ spots, selectedSpotId, onSelect }) {
    return React.createElement("section", { className: "card" },
        React.createElement("h3", null, "Spot 一覧"),
        spots.length === 0
            ? React.createElement("p", { className: "meta" }, "登録済み Spot はありません。")
            : React.createElement("div", { className: "related-list" }, spots.map((spot) =>
                React.createElement("button", {
                    key: spot.id,
                    type: "button",
                    className: spot.id === selectedSpotId ? "" : "secondary",
                    onClick: () => onSelect(spot)
                }, spot.name)
            ))
    );
}

function renderWorld(world) {
    const url = getWorldPageUrl(world);

    return React.createElement(React.Fragment, null,
        React.createElement(OgpPreviewCard, {
            url,
            fallbackTitle: world.name,
            fallbackDescription: world.description
        }),
        React.createElement("p", { className: "meta" }, `追加ユーザー: ${world.registeredByUserId}`),
        React.createElement("p", { className: "meta" }, `人数: ${world.recommendedCapacity} 推奨 / ${world.capacity} 最大`),
        React.createElement("p", { className: "meta" }, `Platform: ${platformLabel(world)}`)
    );
}

function renderPlaceInfo(placeInfo) {
    return React.createElement(React.Fragment, null,
        React.createElement("strong", null, placeInfo.name),
        React.createElement("p", { className: "meta" }, `追加ユーザー: ${placeInfo.registeredByUserId}`),
        React.createElement("p", { className: "meta" }, placeInfo.address),
        renderMarkdown(placeInfo.businessInformation)
    );
}

function renderWebLink(webLink) {
    return React.createElement(React.Fragment, null,
        React.createElement(OgpPreviewCard, {
            url: webLink.url,
            fallbackTitle: webLink.siteName
        }),
        React.createElement("p", { className: "meta" }, `追加ユーザー: ${webLink.registeredByUserId}`)
    );
}

function OgpPreviewCard({ url, fallbackTitle, fallbackDescription = "" }) {
    const [preview, setPreview] = useState(null);

    useEffect(() => {
        let cancelled = false;
        getWebLinkPreview(url)
            .then((loaded) => {
                if (!cancelled) {
                    setPreview(loaded);
                }
            })
            .catch(() => {
                if (!cancelled) {
                    setPreview(null);
                }
            });

        return () => {
            cancelled = true;
        };
    }, [url]);

    const title = preview?.title ?? fallbackTitle;
    const description = preview?.description ?? fallbackDescription;
    const siteName = preview?.siteName;
    const imageUrl = preview?.imageUrl;

    return React.createElement("div", { className: "ogp-card" },
        imageUrl ? React.createElement("img", { src: imageUrl, alt: "", loading: "lazy" }) : null,
        React.createElement("div", null,
            siteName ? React.createElement("p", { className: "eyebrow" }, siteName) : null,
            React.createElement("strong", null, title),
            description ? React.createElement("p", { className: "meta" }, description) : null,
            React.createElement("a", { href: url, target: "_blank", rel: "noopener noreferrer" }, url)
        )
    );
}

function renderComment(comment) {
    return React.createElement(React.Fragment, null,
        renderMarkdown(comment.comments),
        React.createElement("p", { className: "meta" }, `追加ユーザー: ${comment.registeredByUserId}`)
    );
}

async function loadCurrentUser() {
    const response = await fetch("/auth/me");
    if (!response.ok) {
        return null;
    }

    return response.json();
}

async function loadDevelopmentUsers() {
    const response = await fetch("/auth/dev/users");
    if (!response.ok) {
        return [];
    }

    return response.json();
}

async function loadDevelopmentApp() {
    const response = await fetch("/auth/dev/app");
    if (!response.ok) {
        return null;
    }

    return response.json();
}

async function loadAreas() {
    const body = await postJson("/areas/list", {});
    return unwrap(body).areas ?? [];
}

async function loadSpots(query = "") {
    const body = await postJson("/spots/list", { query });
    return unwrap(body).spots ?? [];
}

async function getSpot(id) {
    const body = await postJson("/spots/get", { id });
    return unwrap(body);
}

async function createSpot(payload) {
    const body = await postJson("/spots/create", payload);
    return unwrap(body).spot;
}

async function previewKmlImport(payload) {
    const body = await postJson("/spots/import/kml/preview", payload);
    return unwrap(body);
}

async function importKmlSpots(payload) {
    const body = await postJson("/spots/import/kml", payload);
    return unwrap(body);
}

async function updateSpot(payload) {
    const body = await postJson("/spots/update", payload);
    return unwrap(body).spot;
}

async function deleteSpot(payload) {
    const body = await postJson("/spots/delete", payload);
    return unwrap(body);
}

async function createVRChatWorld(payload) {
    const body = await postJson("/vrchat-worlds/create", payload);
    return unwrap(body).world;
}

async function updateVRChatWorld(payload) {
    const body = await postJson("/vrchat-worlds/update", payload);
    return unwrap(body).world;
}

async function deleteVRChatWorld(payload) {
    const body = await postJson("/vrchat-worlds/delete", payload);
    return unwrap(body);
}

async function createPlaceInfo(payload) {
    const body = await postJson("/place-infos/create", payload);
    return unwrap(body).placeInfo;
}

async function updatePlaceInfo(payload) {
    const body = await postJson("/place-infos/update", payload);
    return unwrap(body).placeInfo;
}

async function deletePlaceInfo(payload) {
    const body = await postJson("/place-infos/delete", payload);
    return unwrap(body);
}

async function createWebLink(payload) {
    const body = await postJson("/web-links/create", payload);
    return unwrap(body).webLink;
}

async function getWebLinkPreview(url) {
    const body = await postJson("/web-links/preview", { url });
    return unwrap(body).preview;
}

async function updateWebLink(payload) {
    const body = await postJson("/web-links/update", payload);
    return unwrap(body).webLink;
}

async function deleteWebLink(payload) {
    const body = await postJson("/web-links/delete", payload);
    return unwrap(body);
}

async function createComment(payload) {
    const body = await postJson("/comments/create", payload);
    return unwrap(body).comment;
}

async function updateComment(payload) {
    const body = await postJson("/comments/update", payload);
    return unwrap(body).comment;
}

async function deleteComment(payload) {
    const body = await postJson("/comments/delete", payload);
    return unwrap(body);
}

async function postJson(url, payload) {
    const response = await fetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload)
    });

    if (!response.ok) {
        const problem = await response.json().catch(() => null);
        throw new Error(problem?.title ?? problem?.detail ?? `${url} failed with ${response.status}`);
    }

    return response.json();
}

function unwrap(body) {
    return body?.value ?? body;
}

function readFileAsBase64(file) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.addEventListener("load", () => {
            const result = String(reader.result ?? "");
            resolve(result.includes(",") ? result.split(",", 2)[1] : result);
        });
        reader.addEventListener("error", () => reject(new Error("ファイルを読み込めませんでした。")));
        reader.readAsDataURL(file);
    });
}

function roundCoordinate(value) {
    return Number(value.toFixed(6));
}

function formatCoordinate(value) {
    return Number(value).toFixed(6);
}

function formatAreaName(areaCode, areas) {
    const numericAreaCode = Number(areaCode);
    const area = areas.find((item) => item.areaCode === numericAreaCode);
    return area ? area.areaName : `未定義エリア (${numericAreaCode})`;
}

function getLinkedSpotId() {
    const params = new URLSearchParams(window.location.search);
    const queryValue = params.get("spotId") ?? params.get("spot");
    if (queryValue) {
        return queryValue;
    }

    const hashValue = window.location.hash.match(/spot=([^&]+)/)?.[1];
    return hashValue ? decodeURIComponent(hashValue) : null;
}

function setLinkedSpotId(spotId) {
    const url = new URL(window.location.href);
    url.searchParams.set("spotId", spotId);
    url.searchParams.delete("spot");
    url.hash = "";
    window.history.replaceState(null, "", url);
}

function clearLinkedSpotId() {
    const url = new URL(window.location.href);
    url.searchParams.delete("spotId");
    url.searchParams.delete("spot");
    url.hash = "";
    window.history.replaceState(null, "", url);
}

function createEmptyWorld() {
    return {
        vrChatWorldId: "",
        name: "",
        recommendedCapacity: 16,
        capacity: 32,
        description: "",
        pc: true,
        android: false,
        ios: false,
        isPrivate: false
    };
}

function createSpotDraft(spot) {
    return {
        name: spot.name,
        description: spot.description,
        latitude: spot.latitude,
        longitude: spot.longitude,
        areaCode: spot.areaCode
    };
}

function createWorldDraft(world) {
    return {
        vrChatWorldId: world.vrChatWorldId,
        name: world.name,
        recommendedCapacity: world.recommendedCapacity,
        capacity: world.capacity,
        description: world.description,
        pc: world.pc,
        android: world.android,
        ios: world.ios,
        isPrivate: world.isPrivate
    };
}

function createEmptyPlaceInfo() {
    return {
        name: "",
        address: "",
        businessInformation: "- 昼: 11:00-14:00\n- 夜: 17:00-22:00\n- 定休日: 不定休"
    };
}

function createPlaceInfoDraft(placeInfo) {
    return {
        name: placeInfo.name,
        address: placeInfo.address,
        businessInformation: placeInfo.businessInformation
    };
}

function createEmptyWebLink() {
    return {
        siteName: "",
        url: ""
    };
}

function createWebLinkDraft(webLink) {
    return {
        siteName: webLink.siteName,
        url: webLink.url
    };
}

function normalizeWorldId(value) {
    const trimmed = value.trim();
    const match = trimmed.match(/wrld_[0-9a-fA-F-]+/);
    return match?.[0] ?? trimmed;
}

function getWorldPageUrl(world) {
    if (world.worldPageUrl) {
        return world.worldPageUrl;
    }

    return `https://vrchat.com/home/world/${normalizeWorldId(world.vrChatWorldId)}/info`;
}

function platformLabel(world) {
    return [
        world.pc ? "PC" : null,
        world.android ? "Android" : null,
        world.ios ? "iOS" : null
    ].filter(Boolean).join(" / ") || "未設定";
}

function emptyToNull(value) {
    const trimmed = value.trim();
    if (trimmed === "") {
        return null;
    }

    return /^https?:\/\//i.test(trimmed) ? trimmed : null;
}

function renderMarkdown(value) {
    const lines = String(value ?? "").split(/\r?\n/);
    const elements = [];
    let index = 0;

    while (index < lines.length) {
        const line = lines[index];

        if (line.trim() === "") {
            index += 1;
            continue;
        }

        const heading = line.match(/^(#{1,3})\s+(.+)$/);
        if (heading) {
            const tag = `h${Math.min(heading[1].length + 2, 5)}`;
            elements.push(React.createElement(tag, { key: `h-${index}` }, renderMarkdownInline(heading[2])));
            index += 1;
            continue;
        }

        if (/^-\s+/.test(line)) {
            const items = [];
            while (index < lines.length && /^-\s+/.test(lines[index])) {
                items.push(React.createElement("li", { key: `li-${index}` }, renderMarkdownInline(lines[index].replace(/^-\s+/, ""))));
                index += 1;
            }
            elements.push(React.createElement("ul", { key: `ul-${index}` }, items));
            continue;
        }

        const paragraphLines = [];
        while (
            index < lines.length &&
            lines[index].trim() !== "" &&
            !/^(#{1,3})\s+/.test(lines[index]) &&
            !/^-\s+/.test(lines[index])
        ) {
            paragraphLines.push(lines[index]);
            index += 1;
        }

        elements.push(React.createElement("p", { key: `p-${index}` }, joinMarkdownLines(paragraphLines)));
    }

    return React.createElement("div", { className: "markdown-body" }, elements);
}

function joinMarkdownLines(lines) {
    return lines.flatMap((line, index) => {
        const nodes = renderMarkdownInline(line);
        return index === 0 ? nodes : [React.createElement("br", { key: `br-${index}` }), ...nodes];
    });
}

function renderMarkdownInline(text) {
    const nodes = [];
    const pattern = /(`[^`]+`|\*\*[^*]+\*\*|\*[^*]+\*|\[[^\]]+\]\([^)]+\))/g;
    let cursor = 0;
    let match;

    while ((match = pattern.exec(text)) !== null) {
        if (match.index > cursor) {
            nodes.push(text.slice(cursor, match.index));
        }

        const token = match[0];
        const key = `${match.index}-${token}`;
        if (token.startsWith("`")) {
            nodes.push(React.createElement("code", { key }, token.slice(1, -1)));
        } else if (token.startsWith("**")) {
            nodes.push(React.createElement("strong", { key }, token.slice(2, -2)));
        } else if (token.startsWith("*")) {
            nodes.push(React.createElement("em", { key }, token.slice(1, -1)));
        } else {
            const link = token.match(/^\[([^\]]+)\]\(([^)]+)\)$/);
            const href = safeMarkdownUrl(link?.[2] ?? "");
            nodes.push(href
                ? React.createElement("a", { key, href, target: "_blank", rel: "noopener noreferrer" }, link?.[1] ?? href)
                : link?.[1] ?? token);
        }

        cursor = match.index + token.length;
    }

    if (cursor < text.length) {
        nodes.push(text.slice(cursor));
    }

    return nodes;
}

function safeMarkdownUrl(value) {
    const trimmed = value.trim();
    return /^https?:\/\//i.test(trimmed) ? trimmed : null;
}

function escapeHtml(value) {
    return String(value ?? "").replace(/[&<>"']/g, (character) => ({
        "&": "&amp;",
        "<": "&lt;",
        ">": "&gt;",
        "\"": "&quot;",
        "'": "&#039;"
    }[character]));
}

createRoot(document.querySelector("#root")).render(React.createElement(App));
