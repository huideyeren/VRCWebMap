const form = document.querySelector("#map-form");
const message = document.querySelector("#message");
const mapsContainer = document.querySelector("#maps");
const reloadButton = document.querySelector("#reload-button");
const resetButton = document.querySelector("#reset-button");
const localUserId = "local-user";

const fields = {
    id: document.querySelector("#map-id"),
    name: document.querySelector("#name"),
    description: document.querySelector("#description"),
    latitude: document.querySelector("#latitude"),
    longitude: document.querySelector("#longitude"),
    areaCode: document.querySelector("#area-code")
};

resetForm();
await loadMaps();

form.addEventListener("submit", async (event) => {
    event.preventDefault();
    await saveMap();
});

reloadButton.addEventListener("click", loadMaps);
resetButton.addEventListener("click", resetForm);

async function loadMaps() {
    message.textContent = "";
    const response = await fetch("/spots/list", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({})
    });

    if (!response.ok) {
        message.textContent = "地図一覧の取得に失敗しました。";
        return;
    }

    const result = await response.json();
    renderMaps(result.spots);
}

async function saveMap() {
    const id = fields.id.value;
    const payload = {
        name: fields.name.value,
        latitude: Number(fields.latitude.value),
        longitude: Number(fields.longitude.value),
        areaCode: Number(fields.areaCode.value),
        description: fields.description.value
    };
    const body = id
        ? { id, actorUserId: localUserId, actorIsAdmin: false, ...payload }
        : { registeredByUserId: localUserId, ...payload };

    const response = await fetch(id ? "/spots/update" : "/spots/create", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body)
    });

    if (!response.ok) {
        const problem = await response.json().catch(() => null);
        message.textContent = problem?.title ?? "保存に失敗しました。入力値を確認してください。";
        return;
    }

    resetForm();
    await loadMaps();
}

async function deleteMap(id) {
    const response = await fetch("/spots/delete", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ id, actorUserId: localUserId, actorIsAdmin: false })
    });

    if (!response.ok) {
        message.textContent = "削除に失敗しました。";
        return;
    }

    await loadMaps();
}

function renderMaps(maps) {
    if (maps.length === 0) {
        mapsContainer.innerHTML = "<p class=\"meta\">登録済みの地図はありません。</p>";
        return;
    }

    mapsContainer.innerHTML = "";

    for (const map of maps) {
        const card = document.createElement("article");
        card.className = "card";
        card.innerHTML = `
            <div>
                <h3>${escapeHtml(map.name)}</h3>
                <p class="meta">ID: ${escapeHtml(map.id)}</p>
            </div>
            <p class="meta">座標: ${map.latitude.toFixed(6)}, ${map.longitude.toFixed(6)}</p>
            <p class="meta">地域コード: ${map.areaCode}</p>
            <p class="meta">${escapeHtml(map.description)}</p>
            <div class="actions">
                <button type="button" class="ghost" data-action="edit">編集</button>
                <button type="button" class="ghost" data-action="delete">削除</button>
            </div>
        `;

        card.querySelector("[data-action='edit']").addEventListener("click", () => editMap(map));
        card.querySelector("[data-action='delete']").addEventListener("click", () => deleteMap(map.id));
        mapsContainer.append(card);
    }
}

function editMap(map) {
    fields.id.value = map.id;
    fields.name.value = map.name;
    fields.description.value = map.description;
    fields.latitude.value = map.latitude;
    fields.longitude.value = map.longitude;
    fields.areaCode.value = map.areaCode;
    message.textContent = "既存の地図を編集中です。";
}

function resetForm() {
    fields.id.value = "";
    fields.name.value = "";
    fields.description.value = "イベント会場と常設ワールドを管理するサンプルスポット";
    fields.latitude.value = "35.681236";
    fields.longitude.value = "139.767125";
    fields.areaCode.value = "13";
    message.textContent = "";
}

function escapeHtml(value) {
    return value.replace(/[&<>"']/g, (character) => ({
        "&": "&amp;",
        "<": "&lt;",
        ">": "&gt;",
        "\"": "&quot;",
        "'": "&#039;"
    }[character]));
}
