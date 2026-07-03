# VrcWebMap 承認済み機能 Delivery Roadmap

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 承認済みの表示改善、WPPLS、DBバックアップ、PortalCategoryを、重複実装とDB事故を避ける順序で完成させる。

**Architecture:** 外部DTOと地域定義を先に安定させ、同じReact画面の変更を続けて完了してからWPPLS contractへ進む。PostgreSQL schemaを変更するPortalCategoryより前にS3バックアップ・リストアを実装し、復元経路を検証する。

**Tech Stack:** .NET 10、Kawa.Web、EF Core/Npgsql、PostgreSQL 18、React 19、TypeScript、Vite、Docker Compose、AWS CLI v2、S3/Wasabi

## Global Constraints

- 各Phaseは対応する計画の全test・commit・clean worktree確認まで完了してから次へ進む。
- Docker Hubへ公開する場合は `beta` を先に更新し、受け入れ確認前に `latest` を変更しない。
- PostgreSQL schema変更前にbackup/restoreのローカル結合試験を成功させる。
- PortalCategory実装はWPPLS基礎contractの完了後に開始する。

---

### Phase 1: 登録者表示名と公開DTO

- [ ] Execute `docs/superpowers/plans/2026-07-02-registrant-display-name.md`

Produces: 生のDiscord IDを外部へ出さないresource DTOと `CanEdit`。

### Phase 2: 地域別Spot一覧

- [ ] Execute `docs/superpowers/plans/2026-07-02-regional-spot-list.md`

Produces: `AreaCategoryDisplayNames` と `/areas/list` category metadata。

### Phase 3: Spot詳細フォーム折りたたみ

- [ ] Execute `docs/superpowers/plans/2026-07-02-collapsible-spot-forms.md`

Produces: 安定した `main.tsx` のフォーム状態遷移。

### Phase 4: WPPLS正式JSON基礎

- [ ] Execute `docs/superpowers/plans/2026-07-02-wppls-world-data.md`

Produces: `ShowPrivateWorld: true`、raw world ID、Roleなしの基礎contract。

### Phase 5: PostgreSQL S3バックアップ

- [ ] Execute `docs/superpowers/plans/2026-07-02-postgresql-s3-backup.md`

Produces: backup/list/restore、Wasabi互換設定、MinIO結合試験、運用手順。

### Phase 6: PortalCategory、Roles、JSONマージ

- [ ] Execute `docs/superpowers/plans/2026-07-02-portal-category-json-merge.md` Task 1以降

Produces: Personal/Public PortalCategory、role-aware WorldData、匿名JSON merge、Portal UI。

### Phase 7: 全体受け入れとbeta公開

- [ ] Run backend/frontend/OpenAPI/PostgreSQL/backup end-to-end verification from both Phase 5 and Phase 6 plans.
- [ ] Confirm `git status --short` is empty.
- [ ] Build and push multi-arch `linux/amd64,linux/arm64` image with the `beta` tag.
- [ ] Verify both platforms with `docker buildx imagetools inspect`.
- [ ] Leave `latest` unchanged until the user confirms beta acceptance.
