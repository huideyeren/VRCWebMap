import React, { useEffect, useMemo, useState } from "react";
import { createRoot } from "react-dom/client";
import {
    AdminPanel,
    KmlImportPanel,
    formatAreaName,
    getSpot,
    loadAreas,
    loadCurrentUser,
    loadSpots,
    postJson,
    unwrap
} from "./main";

function AdminApp() {
    const [currentUser, setCurrentUser] = useState(null);
    const [isAuthLoaded, setIsAuthLoaded] = useState(false);
    const [areas, setAreas] = useState([]);
    const [spots, setSpots] = useState([]);
    const [users, setUsers] = useState([]);
    const [selectedDetails, setSelectedDetails] = useState(null);
    const [activeTab, setActiveTab] = useState("spots");
    const [spotQuery, setSpotQuery] = useState("");
    const [areaCode, setAreaCode] = useState("");
    const [message, setMessage] = useState("");

    useEffect(() => {
        loadCurrentUser()
            .then((user) => {
                setCurrentUser(user);
                setIsAuthLoaded(true);
                if (user?.isAdmin) {
                    return Promise.all([loadAreas(), loadSpots(), loadUsers()]);
                }

                return null;
            })
            .then((loaded) => {
                if (!loaded) {
                    return;
                }

                setAreas(loaded[0]);
                setSpots(loaded[1]);
                setUsers(loaded[2]);
            })
            .catch((error) => {
                setMessage(error.message);
                setIsAuthLoaded(true);
            });
    }, []);

    const filteredSpots = useMemo(() => {
        const term = spotQuery.trim().toLocaleLowerCase();
        return spots.filter((spot) =>
            (!term || spot.name.toLocaleLowerCase().includes(term)) &&
            (!areaCode || String(spot.areaCode) === areaCode));
    }, [spots, spotQuery, areaCode]);

    async function refreshSpots() {
        setSpots(await loadSpots());
    }

    async function selectSpot(spot) {
        setMessage("");
        try {
            setSelectedDetails(await getSpot(spot.id));
        } catch (error) {
            setMessage(error.message);
        }
    }

    async function refreshSelectedSpot(spotId = selectedDetails?.spot?.id) {
        await refreshSpots();
        if (spotId) {
            setSelectedDetails(await getSpot(spotId));
        }
    }

    async function updateAdministrator(user) {
        const nextStatus = !user.isAdmin;
        const action = nextStatus ? "管理者に設定" : "管理者権限を解除";
        if (!confirm(`${user.vrChatDisplayName ?? user.username} を${action}しますか？`)) {
            return;
        }

        setMessage("");
        try {
            await postJson("/users/admin-status", {
                discordUserId: user.discordUserId,
                isAdmin: nextStatus
            });
            setUsers(await loadUsers());
            setMessage(`${user.vrChatDisplayName ?? user.username} の権限を更新しました。`);
        } catch (error) {
            setMessage(error.message);
        }
    }

    async function logout() {
        await postJson("/auth/logout", {});
        window.location.href = "/";
    }

    if (!isAuthLoaded) {
        return React.createElement(StatusPage, { title: "管理用画面を読み込み中", message: "認証状態を確認しています。" });
    }

    if (!currentUser) {
        return React.createElement(StatusPage, {
            title: "Discordログインが必要です",
            message: "管理用画面を開くにはログインしてください。",
            action: React.createElement("a", { className: "menu-button", href: "/auth/discord/login" }, "Discordでログイン")
        });
    }

    if (!currentUser.isAdmin) {
        return React.createElement(StatusPage, {
            title: "管理者権限が必要です",
            message: "この画面はVRC Web Mapの管理者だけが利用できます。"
        });
    }

    return React.createElement("div", { className: "admin-console" },
        React.createElement("header", { className: "admin-console-header" },
            React.createElement("div", null,
                React.createElement("p", { className: "eyebrow" }, "VRC Web Map / Operations"),
                React.createElement("h1", null, "Map Ledger"),
                React.createElement("p", { className: "meta" }, `${currentUser.vrChatDisplayName ?? currentUser.displayName ?? currentUser.username} として管理中`)
            ),
            React.createElement("div", { className: "actions" },
                React.createElement("a", { className: "menu-button secondary", href: "/" }, "地図へ戻る"),
                React.createElement("button", { type: "button", className: "secondary", onClick: logout }, "ログアウト")
            )
        ),
        React.createElement("nav", { className: "admin-tabs", "aria-label": "管理機能" },
            React.createElement(TabButton, { id: "spots", activeTab, onChange: setActiveTab }, `Spot管理 ${spots.length}`),
            React.createElement(TabButton, { id: "users", activeTab, onChange: setActiveTab }, `ユーザー管理 ${users.length}`),
            React.createElement(TabButton, { id: "kml", activeTab, onChange: setActiveTab }, "KMLインポート")
        ),
        message ? React.createElement("p", { className: "notice admin-notice", role: "status" }, message) : null,
        !currentUser.hasVRChatDisplayName
            ? React.createElement("p", { className: "notice admin-notice" }, "書き込み操作を行う前に、地図画面のプロフィール設定でVRChat表示名を登録してください。")
            : null,
        activeTab === "spots" ? React.createElement("main", { className: "admin-workspace" },
            React.createElement("section", { className: "admin-table-section" },
                React.createElement("div", { className: "admin-filter-bar" },
                    React.createElement("label", null, "Spot名",
                        React.createElement("input", {
                            type: "search",
                            value: spotQuery,
                            placeholder: "Spot名で絞り込み",
                            onChange: (event) => setSpotQuery(event.target.value)
                        })
                    ),
                    React.createElement("label", null, "地域",
                        React.createElement("select", {
                            value: areaCode,
                            onChange: (event) => setAreaCode(event.target.value)
                        } as React.SelectHTMLAttributes<HTMLSelectElement>,
                            React.createElement("option", { value: "" }, "すべての地域"),
                            areas.map((area) => React.createElement("option", { key: area.areaCode, value: area.areaCode }, area.areaName))
                        )
                    ),
                    React.createElement("span", { className: "table-count" }, `${filteredSpots.length} / ${spots.length}`)
                ),
                React.createElement("div", { className: "table-scroll" },
                    React.createElement("table", { className: "admin-table" },
                        React.createElement("thead", null,
                            React.createElement("tr", null,
                                React.createElement("th", null, "Spot"),
                                React.createElement("th", null, "地域"),
                                React.createElement("th", null, "緯度"),
                                React.createElement("th", null, "経度"),
                                React.createElement("th", null, "登録者"),
                                React.createElement("th", null, "操作")
                            )
                        ),
                        React.createElement("tbody", null,
                            filteredSpots.map((spot) => React.createElement("tr", { key: spot.id },
                                React.createElement("td", { className: "table-primary" }, spot.name),
                                React.createElement("td", null, formatAreaName(spot.areaCode, areas)),
                                React.createElement("td", { className: "numeric" }, Number(spot.latitude).toFixed(6)),
                                React.createElement("td", { className: "numeric" }, Number(spot.longitude).toFixed(6)),
                                React.createElement("td", { className: "utility-text" }, spot.registeredByDisplayName),
                                React.createElement("td", null,
                                    React.createElement("button", { type: "button", className: "secondary compact", onClick: () => selectSpot(spot) }, "編集")
                                )
                            ))
                        )
                    )
                )
            ),
            selectedDetails ? React.createElement("section", { className: "admin-editor-stage" },
                React.createElement("div", { className: "admin-editor-heading" },
                    React.createElement("div", null,
                        React.createElement("p", { className: "eyebrow" }, "Selected record"),
                        React.createElement("h2", null, selectedDetails.spot.name)
                    ),
                    React.createElement("button", { type: "button", className: "secondary compact", onClick: () => setSelectedDetails(null) }, "閉じる")
                ),
                React.createElement(AdminPanel, {
                    spot: selectedDetails.spot,
                    worlds: selectedDetails.vrChatWorlds ?? [],
                    placeInfos: selectedDetails.placeInfos ?? [],
                    webLinks: selectedDetails.webLinks ?? [],
                    comments: selectedDetails.comments ?? [],
                    areas,
                    currentUser,
                    onChanged: refreshSelectedSpot,
                    onDeleted: async () => {
                        setSelectedDetails(null);
                        await refreshSpots();
                    },
                    onMessage: setMessage
                })
            ) : null
        ) : null,
        activeTab === "users" ? React.createElement(UserTable, {
            users,
            currentUser,
            onToggle: updateAdministrator
        }) : null,
        activeTab === "kml" ? React.createElement("main", { className: "admin-workspace narrow" },
            React.createElement(KmlImportPanel, {
                areas,
                currentUser,
                onImported: refreshSpots,
                onMessage: setMessage
            })
        ) : null
    );
}

function TabButton({ id, activeTab, onChange, children = null }) {
    return React.createElement("button", {
        type: "button",
        className: activeTab === id ? "active" : "",
        "aria-current": activeTab === id ? "page" : undefined,
        onClick: () => onChange(id)
    }, children);
}

function UserTable({ users, currentUser, onToggle }) {
    return React.createElement("main", { className: "admin-workspace" },
        React.createElement("section", { className: "admin-table-section" },
            React.createElement("div", { className: "table-scroll" },
                React.createElement("table", { className: "admin-table" },
                    React.createElement("thead", null,
                        React.createElement("tr", null,
                            React.createElement("th", null, "VRChat表示名"),
                            React.createElement("th", null, "Discord"),
                            React.createElement("th", null, "Discord ID"),
                            React.createElement("th", null, "権限"),
                            React.createElement("th", null, "操作")
                        )
                    ),
                    React.createElement("tbody", null,
                        users.map((user) => {
                            const protectedAdmin = user.isInitialAdmin || user.discordUserId === currentUser.discordUserId;
                            return React.createElement("tr", { key: user.discordUserId },
                                React.createElement("td", { className: "table-primary" }, user.vrChatDisplayName ?? "未登録"),
                                React.createElement("td", null, user.username),
                                React.createElement("td", { className: "utility-text" }, user.discordUserId),
                                React.createElement("td", null,
                                    React.createElement("span", { className: user.isAdmin ? "status-chip admin" : "status-chip" },
                                        user.isInitialAdmin ? "初期管理者" : user.isAdmin ? "管理者" : "一般")
                                ),
                                React.createElement("td", null,
                                    React.createElement("button", {
                                        type: "button",
                                        className: user.isAdmin ? "danger compact" : "secondary compact",
                                        disabled: user.isAdmin && protectedAdmin,
                                        onClick: () => onToggle(user)
                                    }, user.isAdmin ? "権限を解除" : "管理者に設定")
                                )
                            );
                        })
                    )
                )
            )
        )
    );
}

function StatusPage({ title, message, action = null }) {
    return React.createElement("main", { className: "admin-status-page" },
        React.createElement("section", { className: "document-card" },
            React.createElement("p", { className: "eyebrow" }, "VRC Web Map / Operations"),
            React.createElement("h1", null, title),
            React.createElement("p", { className: "meta" }, message),
            React.createElement("div", { className: "actions" },
                action,
                React.createElement("a", { className: "menu-button secondary", href: "/" }, "地図へ戻る")
            )
        )
    );
}

async function loadUsers() {
    const body = await postJson("/users/list", {});
    return unwrap(body).users ?? [];
}

const root = document.querySelector("#root");
if (root) {
    createRoot(root).render(React.createElement(AdminApp));
}
