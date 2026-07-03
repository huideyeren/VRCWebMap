import React, { useEffect, useState } from "react";
import { createRoot } from "react-dom/client";
import { loadCurrentUser, postJson, unwrap } from "./main";

const Personal = 0;
const Public = 1;

const loadPortalCategories = () =>
    postJson("/portal-categories/list", {}).then(unwrap).then((body) => body.categories ?? []);

const downloadJson = (content, fileName) => {
    const blob = new Blob([content], { type: "application/json" });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = fileName;
    document.body.append(anchor);
    anchor.click();
    anchor.remove();
    URL.revokeObjectURL(url);
};

const readTextFile = (file) => file.text();

function PortalApp() {
    const [currentUser, setCurrentUser] = useState(null);
    const [isLoaded, setIsLoaded] = useState(false);
    const [categories, setCategories] = useState([]);
    const [users, setUsers] = useState([]);
    const [selectedFile, setSelectedFile] = useState(null);
    const [fileInputKey, setFileInputKey] = useState(0);
    const [message, setMessage] = useState("");
    const [isBusy, setIsBusy] = useState(false);

    useEffect(() => {
        Promise.all([loadCurrentUser(), loadPortalCategories()])
            .then(async ([user, loadedCategories]) => {
                setCurrentUser(user);
                setCategories(loadedCategories);
                if (user?.isAdmin) {
                    const body = await postJson("/users/list", {});
                    setUsers(unwrap(body).users ?? []);
                }
            })
            .catch((error) => setMessage(error.message))
            .finally(() => setIsLoaded(true));
    }, []);

    async function refreshCategories() {
        setCategories(await loadPortalCategories());
    }

    async function run(action, successMessage, refresh = true) {
        setIsBusy(true);
        setMessage("");
        try {
            await action();
            if (refresh) {
                await refreshCategories();
            }
            setMessage(successMessage);
            return true;
        } catch (error) {
            setMessage(error.message);
            return false;
        } finally {
            setIsBusy(false);
        }
    }

    async function downloadSystemData() {
        await run(async () => {
            const body = await postJson("/portal/world-data", {});
            downloadJson(JSON.stringify(unwrap(body), null, 2), "WorldData.json");
        }, "現在の公開範囲でWorldData.jsonを作成しました。", false);
    }

    async function mergeAndDownload() {
        if (!selectedFile) {
            setMessage("マージ元の.jsonファイルを選択してください。");
            return;
        }

        const succeeded = await run(async () => {
            const existingJson = await readTextFile(selectedFile);
            const body = await postJson("/portal/world-data/merge", { existingJson });
            downloadJson(unwrap(body).mergedJson, "WorldData.merged.json");
        }, "カテゴリとロールを末尾へ追加したJSONを作成しました。", false);

        if (succeeded) {
            setSelectedFile(null);
            setFileInputKey((value) => value + 1);
        }
    }

    return (
        <div className="portal-console">
            <header className="portal-header">
                <div>
                    <p className="eyebrow">VRC Web Map / Portal routing desk</p>
                    <h1>Portal JSON</h1>
                    <p className="meta">
                        WPPLS用データの出力と、地図に属さないワールドのカテゴリ管理。
                    </p>
                </div>
                <div className="actions">
                    <a className="menu-button secondary" href="/">地図へ戻る</a>
                    {!currentUser
                        ? <a className="menu-button" href="/auth/discord/login">Discordでログイン</a>
                        : <span className="user-chip">{currentUser.vrChatDisplayName ?? currentUser.displayName ?? currentUser.username}</span>}
                </div>
            </header>

            {message ? <p className="notice portal-notice" role="status">{message}</p> : null}

            <main className="portal-grid">
                <section className="portal-export-panel">
                    <div className="portal-section-heading">
                        <p className="eyebrow">JSON output</p>
                        <h2>ファイルを組み立てる</h2>
                    </div>
                    <div className="portal-action-block">
                        <h3>このシステムのWorldData</h3>
                        <p>未ログイン時は地域と全体公開、ログイン時はさらに自分のPersonalカテゴリを含みます。</p>
                        <button type="button" onClick={downloadSystemData} disabled={isBusy}>
                            WorldData.jsonをダウンロード
                        </button>
                    </div>
                    <div className="portal-action-block portal-merge-block">
                        <h3>既存JSONへ追加</h3>
                        <p>既存のカテゴリと未知の拡張値を保ったまま、システム側カテゴリを末尾へ追加します。上限は5 MiBです。</p>
                        <label>
                            マージ元の.jsonファイル
                            <input
                                key={fileInputKey}
                                type="file"
                                accept=".json,application/json"
                                onChange={(event) => setSelectedFile(event.target.files?.[0] ?? null)}
                            />
                        </label>
                        <div className="portal-file-readout" aria-live="polite">
                            {selectedFile
                                ? <><strong>{selectedFile.name}</strong><span>{formatBytes(selectedFile.size)}</span></>
                                : <span>ファイルは選択されていません</span>}
                        </div>
                        <button type="button" onClick={mergeAndDownload} disabled={isBusy || !selectedFile}>
                            マージしてダウンロード
                        </button>
                    </div>
                </section>

                <section className="portal-ledger">
                    <div className="portal-section-heading">
                        <p className="eyebrow">Category ledger</p>
                        <h2>地図外ワールド</h2>
                        <p className="meta">
                            {currentUser
                                ? "編集権限は各カテゴリの所有範囲に従います。公開範囲と所有者は作成後に変更できません。"
                                : "全体公開カテゴリを閲覧中です。編集するにはログインしてください。"}
                        </p>
                    </div>

                    {currentUser
                        ? <CreateCategoryForm
                            currentUser={currentUser}
                            users={users}
                            disabled={isBusy || !currentUser.hasVRChatDisplayName}
                            onCreate={(payload) => run(
                                () => postJson("/portal-categories/create", payload),
                                "カテゴリを作成しました。")}
                        />
                        : null}

                    {!isLoaded
                        ? <p className="portal-empty">カテゴリを読み込んでいます。</p>
                        : categories.length === 0
                            ? <p className="portal-empty">表示できるカテゴリはまだありません。</p>
                            : <div className="portal-category-list">
                                {categories.map((category) =>
                                    <CategoryCard
                                        key={category.id}
                                        category={category}
                                        categories={categories}
                                        disabled={isBusy}
                                        onRun={run}
                                    />)}
                            </div>}
                </section>
            </main>
        </div>
    );
}

function CreateCategoryForm({ currentUser, users, disabled, onCreate }) {
    const [name, setName] = useState("");
    const [visibility, setVisibility] = useState(Personal);
    const [ownerUserId, setOwnerUserId] = useState(currentUser.discordUserId);

    async function submit(event) {
        event.preventDefault();
        const succeeded = await onCreate({
            name,
            visibility,
            ownerUserId: visibility === Personal && currentUser.isAdmin
                ? ownerUserId
                : null
        });
        if (succeeded) {
            setName("");
        }
    }

    return (
        <form className="portal-create-form" onSubmit={submit}>
            <div>
                <p className="eyebrow">New route</p>
                <h3>カテゴリを作成</h3>
            </div>
            <label>
                カテゴリ名
                <input value={name} onChange={(event) => setName(event.target.value)} required />
            </label>
            {currentUser.isAdmin
                ? <label>
                    公開範囲
                    <select value={visibility} onChange={(event) => setVisibility(Number(event.target.value))}>
                        <option value={Personal}>Personal</option>
                        <option value={Public}>全体公開</option>
                    </select>
                </label>
                : <p className="portal-fixed-value"><span>公開範囲</span><strong>Personal</strong></p>}
            {currentUser.isAdmin && visibility === Personal
                ? <label>
                    所有者
                    <select value={ownerUserId} onChange={(event) => setOwnerUserId(event.target.value)}>
                        {users.filter((user) => user.vrChatDisplayName).map((user) =>
                            <option key={user.discordUserId} value={user.discordUserId}>
                                {user.vrChatDisplayName}
                            </option>)}
                    </select>
                </label>
                : null}
            <button type="submit" disabled={disabled || !name.trim()}>作成する</button>
        </form>
    );
}

function CategoryCard({ category, categories, disabled, onRun }) {
    const [name, setName] = useState(category.name);
    const [isAddingWorld, setIsAddingWorld] = useState(false);
    const editableDestinations = categories.filter((candidate) =>
        candidate.canEdit && candidate.id !== category.id);

    async function rename(event) {
        event.preventDefault();
        await onRun(
            () => postJson("/portal-categories/update", { id: category.id, name }),
            "カテゴリ名を更新しました。");
    }

    async function removeCategory() {
        if (!confirm(`「${category.name}」を削除しますか？`)) {
            return;
        }
        await onRun(
            () => postJson("/portal-categories/delete", { id: category.id }),
            "カテゴリを削除しました。");
    }

    return (
        <article className={`portal-category-card ${category.canEdit ? "editable" : "readonly"}`}>
            <div className="portal-route-line" aria-hidden="true" />
            <header className="portal-category-header">
                <div>
                    <div className="portal-badges">
                        <span className={`status-chip ${category.visibility === Public ? "public" : "personal"}`}>
                            {category.visibility === Public ? "PUBLIC" : "PERSONAL"}
                        </span>
                        {!category.canEdit ? <span className="status-chip">READ ONLY</span> : null}
                    </div>
                    <h3>{category.name}</h3>
                    <p className="meta">
                        所有者: {category.ownerDisplayName ?? "全体公開"} / 登録者: {category.registeredByDisplayName}
                    </p>
                </div>
                <span className="portal-world-count">{category.worlds.length} worlds</span>
            </header>

            {category.canEdit
                ? <form className="portal-inline-editor" onSubmit={rename}>
                    <label>
                        カテゴリ名
                        <input value={name} onChange={(event) => setName(event.target.value)} required />
                    </label>
                    <button type="submit" className="secondary" disabled={disabled || name.trim() === category.name}>
                        名前を保存
                    </button>
                    <button type="button" className="danger" onClick={removeCategory} disabled={disabled || category.worlds.length > 0}>
                        カテゴリを削除
                    </button>
                </form>
                : null}

            <div className="portal-world-list">
                {category.worlds.length === 0
                    ? <p className="portal-empty compact">このカテゴリにワールドはありません。</p>
                    : category.worlds.map((world) =>
                        <WorldCard
                            key={world.id}
                            world={world}
                            category={category}
                            destinations={editableDestinations}
                            disabled={disabled}
                            onRun={onRun}
                        />)}
            </div>

            {category.canEdit
                ? <div className="portal-add-world">
                    <button type="button" className="secondary" onClick={() => setIsAddingWorld((value) => !value)}>
                        {isAddingWorld ? "追加を閉じる" : "ワールドを追加"}
                    </button>
                    {isAddingWorld
                        ? <WorldForm
                            submitLabel="登録する"
                            disabled={disabled}
                            onSubmit={async (value) => {
                                const succeeded = await onRun(
                                    () => postJson("/portal-worlds/create", {
                                        portalCategoryId: category.id,
                                        ...toWorldPayload(value)
                                    }),
                                    "ワールドを登録しました。");
                                if (succeeded) {
                                    setIsAddingWorld(false);
                                }
                                return succeeded;
                            }}
                        />
                        : null}
                </div>
                : null}
        </article>
    );
}

function WorldCard({ world, category, destinations, disabled, onRun }) {
    const [isEditing, setIsEditing] = useState(false);
    const [destinationId, setDestinationId] = useState(destinations[0]?.id ?? "");

    async function remove() {
        if (!confirm(`「${world.name}」を削除しますか？`)) {
            return;
        }
        await onRun(
            () => postJson("/portal-worlds/delete", { id: world.id }),
            "ワールドを削除しました。");
    }

    return (
        <div className="portal-world-card">
            <div className="portal-world-summary">
                <div>
                    <strong>{world.name}</strong>
                    <p>{world.description}</p>
                    <span className="utility-text">
                        {world.vrChatWorldId} · {world.recommendedCapacity}/{world.capacity}人 · {world.isPrivate ? "private" : "public"}
                    </span>
                </div>
                {category.canEdit
                    ? <div className="actions">
                        <button type="button" className="secondary compact" onClick={() => setIsEditing((value) => !value)}>
                            {isEditing ? "閉じる" : "編集"}
                        </button>
                        <button type="button" className="danger compact" onClick={remove} disabled={disabled}>削除</button>
                    </div>
                    : null}
            </div>
            {isEditing
                ? <>
                    <WorldForm
                        initialValue={world}
                        submitLabel="変更を保存"
                        disabled={disabled}
                        onSubmit={async (value) => {
                            const succeeded = await onRun(
                                () => postJson("/portal-worlds/update", {
                                    id: world.id,
                                    ...toWorldPayload(value)
                                }),
                                "ワールド情報を更新しました。");
                            if (succeeded) {
                                setIsEditing(false);
                            }
                            return succeeded;
                        }}
                    />
                    {destinations.length > 0
                        ? <div className="portal-move-row">
                            <label>
                                移動先
                                <select value={destinationId} onChange={(event) => setDestinationId(event.target.value)}>
                                    {destinations.map((destination) =>
                                        <option key={destination.id} value={destination.id}>{destination.name}</option>)}
                                </select>
                            </label>
                            <button
                                type="button"
                                className="secondary"
                                disabled={disabled || !destinationId}
                                onClick={() => onRun(
                                    () => postJson("/portal-worlds/move", {
                                        id: world.id,
                                        destinationPortalCategoryId: destinationId
                                    }),
                                    "ワールドを移動しました。")}>
                                移動する
                            </button>
                        </div>
                        : null}
                </>
                : null}
        </div>
    );
}

function WorldForm({ initialValue = null, submitLabel, disabled, onSubmit }) {
    const [value, setValue] = useState(() => initialValue ?? emptyWorld());

    function update(field) {
        return (event) => {
            const nextValue = event.target.type === "checkbox"
                ? event.target.checked
                : event.target.value;
            setValue((current) => ({ ...current, [field]: nextValue }));
        };
    }

    return (
        <form className="portal-world-form" onSubmit={(event) => {
            event.preventDefault();
            onSubmit(value);
        }}>
            <label>VRChat world ID<input value={value.vrChatWorldId} onChange={update("vrChatWorldId")} required /></label>
            <label>ワールド名<input value={value.name} onChange={update("name")} required /></label>
            <label className="portal-form-wide">説明<textarea rows={3} value={value.description} onChange={update("description")} required /></label>
            <label>推奨人数<input type="number" min="0" value={value.recommendedCapacity} onChange={update("recommendedCapacity")} required /></label>
            <label>最大人数<input type="number" min="0" value={value.capacity} onChange={update("capacity")} required /></label>
            <fieldset className="portal-platforms">
                <legend>対応プラットフォーム</legend>
                <label><input type="checkbox" checked={value.pc} onChange={update("pc")} />PC</label>
                <label><input type="checkbox" checked={value.android} onChange={update("android")} />Android</label>
                <label><input type="checkbox" checked={value.ios} onChange={update("ios")} />iOS</label>
                <label><input type="checkbox" checked={value.isPrivate} onChange={update("isPrivate")} />VRChat上でprivate</label>
            </fieldset>
            <button type="submit" disabled={disabled}>{submitLabel}</button>
        </form>
    );
}

function emptyWorld() {
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

function toWorldPayload(value) {
    return {
        ...value,
        recommendedCapacity: Number(value.recommendedCapacity),
        capacity: Number(value.capacity)
    };
}

function formatBytes(bytes) {
    if (bytes < 1024) {
        return `${bytes} B`;
    }
    return `${(bytes / 1024).toFixed(bytes < 1024 * 100 ? 1 : 0)} KiB`;
}

createRoot(document.getElementById("root")!).render(<PortalApp />);
