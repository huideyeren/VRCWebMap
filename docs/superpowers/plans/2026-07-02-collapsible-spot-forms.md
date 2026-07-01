# Spot詳細フォーム折りたたみ Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Spot詳細の追加フォームと編集パネルを、入力状態を保持する排他的accordionへ変更する。

**Architecture:** `SpotDetails` が `activeFormPanel: null | "add" | "edit"` を所有し、表示専用 `CollapsiblePanel` がbutton/hidden/ARIAを担当する。子フォームは閉じてもunmountせず、保存成功時だけ親へ通知して閉じる。

**Tech Stack:** React 19、TypeScript、Vite

## Global Constraints

- 初期状態は両方閉じる。
- addとeditは同時に開かない。
- 同じSpotではdraftを保持し、Spot切替で初期化する。
- 成功時だけ閉じ、失敗時は値と開閉状態を保持する。
- 関連データ削除成功ではeditを開いたままにする。
- backend contract/APIは変更しない。

---

### Task 1: 共通accordionと状態遷移を追加する

**Files:**
- Modify: `src/main.tsx`
- Modify: `src/styles.css`

- [ ] **Step 1: CollapsiblePanelを追加する**

```typescript
function CollapsiblePanel({ title, open, onToggle, contentId, children }) {
    return React.createElement("section", { className: "collapsible-form-panel" },
        React.createElement("button", {
            type: "button",
            className: "collapsible-form-heading",
            "aria-expanded": open,
            "aria-controls": contentId,
            onClick: onToggle
        },
            React.createElement("span", null, title),
            React.createElement("span", { "aria-hidden": true }, open ? "⌄" : "›")
        ),
        React.createElement("div", { id: contentId, hidden: !open }, children)
    );
}
```

- [ ] **Step 2: SpotDetailsへ排他stateを追加する**

```typescript
const [activeFormPanel, setActiveFormPanel] =
    useState<null | "add" | "edit">(null);

useEffect(() => {
    setActiveFormPanel(null);
}, [spot.id]);

const togglePanel = (panel) =>
    setActiveFormPanel((current) => current === panel ? null : panel);
```

Give Spot-specific form subtree `key={spot.id}` so drafts reset only when the
selected Spot changes.

- [ ] **Step 3: add/edit sectionsをwrapする**

Use exact titles:

```text
情報を追加
管理者編集
登録者編集
```

Choose the edit title from `currentUser.isAdmin`. Do not render the add heading
without a registered VRChat Display Name. Do not render edit heading when no
resource has `CanEdit`.

- [ ] **Step 4: success callbackを配線する**

Add `onSaved` callbacks:

```typescript
onSaved={() => setActiveFormPanel(null)}
```

Call after successful create/update and after refreshed Spot details. Never call
from catch paths. Item edit cancel and related-data delete do not call it. Spot
delete continues closing the whole details view.

- [ ] **Step 5: stylesを追加する**

Add scoped styles for heading width, chevron, focus-visible, hidden content and
mobile wrapping. Do not add nested card backgrounds.

- [ ] **Step 6: validationとmanual state matrix**

```bash
pnpm typecheck
pnpm build
dotnet test VrcWebMap.Backend.Tests/VrcWebMap.Backend.Tests.csproj \
  --no-restore -p:RestorePackagesPath=/private/tmp/nuget-packages
```

Manually verify:

```text
initial: both closed
open add: edit closes
open edit: add closes
close/reopen same Spot: draft remains
switch Spot: state/draft resets
create/update success: panel closes
failure: panel and draft remain
item cancel/delete: edit panel remains
keyboard: Enter/Space and ARIA state work
```

- [ ] **Step 7: commit**

```bash
git add src/main.tsx src/styles.css
git commit -m "feat: collapse spot detail forms"
git status --short
```
