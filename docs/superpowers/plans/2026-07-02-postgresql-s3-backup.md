# PostgreSQL S3バックアップ・リストア Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** PostgreSQL 18のcustom dumpをAWS S3/Wasabiへ週次保存し、検証済み世代の一覧・latest解決・安全なリストアを提供する。

**Architecture:** Docker Composeの`backup` profileにone-shot `db-backup` serviceを追加し、PostgreSQL 18 clientとAWS CLI v2を専用imageへ封入する。DB処理はcontainer script、backend停止・復旧はPOSIX/PowerShell host wrapperが担当する。

**Tech Stack:** PostgreSQL 18、Docker Compose、AWS CLI v2、POSIX shell、PowerShell 7、MinIO

## Global Constraints

- backup/list/restoreは通常の`docker compose up`で起動しない。
- `.dump`、`.sha256`、`.json` manifestの3objectで1世代とする。
- manifestを最後にuploadし、manifestなしを不完全世代として除外する。
- retention既定値は4。
- restore前にchecksum、size、DB名、`pg_restore --list`を検証する。
- restore失敗時はbackendを停止したままにする。
- `.env.backup` とsecretはcommit/log出力しない。
- PostgreSQL client majorは18。

## Exit Codes

```text
0  success
64 usage/configuration error
65 lock unavailable
66 PostgreSQL connection/dump error
67 S3 list/upload/download error
68 archive/checksum/manifest verification error
69 database restore error
70 retention cleanup error after successful backup
71 backend restart error after successful restore
```

---

### Task 1: backup image、Compose service、設定境界を追加する

**Files:**
- Create: `ops/db-backup/Dockerfile`
- Create: `ops/db-backup/db-backup.sh`
- Create: `.env.backup.example`
- Modify: `.gitignore`
- Modify: `docker-compose.yml`

- [ ] **Step 1: image contract testを先に書く**

Create `ops/db-backup/tests/image-smoke.sh`:

```sh
#!/bin/sh
set -eu
image="${1:?image is required}"
docker run --rm "$image" pg_dump --version | grep -E ' 18([. ]|$)'
docker run --rm "$image" aws --version | grep 'aws-cli/2'
docker run --rm "$image" sha256sum --help >/dev/null
docker run --rm "$image" jq --version >/dev/null
```

- [ ] **Step 2: Dockerfileを追加する**

Use `postgres:18-bookworm`, install `curl unzip jq util-linux ca-certificates`,
and AWS CLI v2. Map `$TARGETARCH`:

```dockerfile
ARG TARGETARCH
RUN case "$TARGETARCH" in \
      amd64) aws_arch=x86_64 ;; \
      arm64) aws_arch=aarch64 ;; \
      *) echo "unsupported architecture: $TARGETARCH" >&2; exit 64 ;; \
    esac \
    && curl -fsSLo /tmp/awscliv2.zip \
      "https://awscli.amazonaws.com/awscli-exe-linux-${aws_arch}.zip" \
    && unzip -q /tmp/awscliv2.zip -d /tmp \
    && /tmp/aws/install \
    && rm -rf /tmp/aws /tmp/awscliv2.zip
```

Copy `db-backup.sh` to `/usr/local/bin/db-backup`, set executable, and use it as
ENTRYPOINT.

- [ ] **Step 3: Compose serviceとenv exampleを追加する**

Add:

```yaml
  db-backup:
    build:
      context: .
      dockerfile: ops/db-backup/Dockerfile
    profiles: [backup]
    environment:
      PGHOST: postgres
      PGPORT: 5432
      PGDATABASE: ${BACKUP_PGDATABASE:-vrcwebmap}
      PGUSER: ${BACKUP_PGUSER:-vrcwebmap}
      PGPASSWORD: ${BACKUP_PGPASSWORD}
      S3_BUCKET: ${S3_BUCKET}
      S3_PREFIX: ${S3_PREFIX:-backups}
      S3_ENDPOINT_URL: ${S3_ENDPOINT_URL:-}
      S3_ADDRESSING_STYLE: ${S3_ADDRESSING_STYLE:-auto}
      AWS_REGION: ${AWS_REGION}
      AWS_ACCESS_KEY_ID: ${AWS_ACCESS_KEY_ID}
      AWS_SECRET_ACCESS_KEY: ${AWS_SECRET_ACCESS_KEY}
      AWS_SESSION_TOKEN: ${AWS_SESSION_TOKEN:-}
      BACKUP_RETENTION_COUNT: ${BACKUP_RETENTION_COUNT:-4}
    depends_on:
      postgres:
        condition: service_healthy
    volumes:
      - backup-work:/backup-work
```

Add `backup-work:` under volumes. `.env.backup.example` lists every variable
without real credentials. Add `.env.backup` to `.gitignore`.

- [ ] **Step 4: image testを実行してcommitする**

```bash
docker build -f ops/db-backup/Dockerfile -t vrcwebmap-db-backup:test .
sh ops/db-backup/tests/image-smoke.sh vrcwebmap-db-backup:test
git add ops/db-backup docker-compose.yml .env.backup.example .gitignore
git commit -m "feat: add PostgreSQL backup container"
```

Expected: PostgreSQL 18、AWS CLI v2、jq、sha256sum確認成功。

---

### Task 2: backup、list、resolve、retentionを実装する

**Files:**
- Modify: `ops/db-backup/db-backup.sh`
- Create: `ops/db-backup/tests/minio-compose.yml`
- Create: `ops/db-backup/tests/integration.sh`

- [ ] **Step 1: validationと命名のshell testを書く**

Integration test starts isolated PostgreSQL and MinIO, then asserts:

```sh
run backup
run list | grep 'backups/vrcwebmap-'
object_count '.dump' = 1
object_count '.sha256' = 1
object_count '.json' = 1
run resolve latest | grep 'backups/vrcwebmap-.*\\.dump'
```

It also runs 5 backups with unique UTC timestamps and asserts only 4 complete
manifests remain.

- [ ] **Step 2: common script functionsを実装する**

At script start:

```sh
#!/bin/sh
set -eu
umask 077

EX_USAGE=64
EX_LOCK=65
EX_POSTGRES=66
EX_S3=67
EX_VERIFY=68
EX_RESTORE=69
EX_RETENTION=70
```

Validate required vars, `BACKUP_RETENTION_COUNT` as integer >=1, addressing
style in `auto|path|virtual`, and HTTPS endpoint unless
`ALLOW_INSECURE_S3_ENDPOINT=true`.

Create a temporary `AWS_CONFIG_FILE` containing:

```ini
[default]
region = configured-region
s3 =
    addressing_style = configured-style
```

Build endpoint args without `eval`:

```sh
aws_s3() {
  if [ -n "${S3_ENDPOINT_URL:-}" ]; then
    aws --endpoint-url "$S3_ENDPOINT_URL" s3 "$@"
  else
    aws s3 "$@"
  fi
}
```

Use `flock -n 9` on `/backup-work/operation.lock`; exit 65 if unavailable.

- [ ] **Step 3: backup commandを実装する**

Exact order:

```text
pg_isready
aws_s3 ls bucket/prefix
pg_dump --format=custom --no-owner --no-acl --file dump
pg_restore --list dump
sha256sum dump
generate manifest with jq
upload dump
upload sha256
upload manifest last
retention cleanup
```

Manifest fields:

```json
{
  "database": "vrcwebmap",
  "createdAtUtc": "2026-07-02T03:00:00Z",
  "dumpKey": "backups/vrcwebmap-20260702T030000Z.dump",
  "sizeBytes": 123,
  "sha256": "hex",
  "format": "pg_dump-custom"
}
```

Trap removes local temp files and config. Dump/upload failure skips retention.
Retention failure exits 70 after reporting that the new backup succeeded.

- [ ] **Step 4: listとresolveを実装する**

List only `.json` manifests, download each, validate required fields, and print
created time、size、hash、dump key newest-first. Ignore objects without manifest.

`resolve latest` prints exactly one dump key from the newest valid manifest.
`resolve <key>` validates the matching manifest before echoing the key.
`describe <key>` prints the validated database、created time、size、hash and
exact dump key without downloading the dump.

- [ ] **Step 5: MinIO integration testを実行する**

```bash
sh ops/db-backup/tests/integration.sh
```

Expected: 3-object generation、manifest-last semantics、list/latest、
4-generation retention、不完全世代除外 all pass。

- [ ] **Step 6: commit**

```bash
git add ops/db-backup
git commit -m "feat: back up PostgreSQL to S3"
```

---

### Task 3: verified restoreとhost wrappersを実装する

**Files:**
- Modify: `ops/db-backup/db-backup.sh`
- Create: `ops/db-backup/restore.sh`
- Create: `ops/db-backup/restore.ps1`
- Modify: `ops/db-backup/tests/integration.sh`

- [ ] **Step 1: restore integration scenariosを書く**

Test:

```text
seed row A
backup
replace with row B
restore explicit key
assert row A restored
repeat with latest
tamper checksum and assert exit 68
force pg_restore failure and assert exit 69
```

- [ ] **Step 2: container restoreを実装する**

`restore <resolved-dump-key>`:

1. derive `.sha256` and `.json` keys;
2. download all three;
3. validate manifest database/key/size/hash;
4. run `pg_restore --list`;
5. terminate other DB sessions;
6. execute:

```sh
pg_restore \
  --clean \
  --if-exists \
  --exit-on-error \
  --single-transaction \
  --no-owner \
  --no-acl \
  --dbname "$PGDATABASE" \
  "$dump_file"
```

7. run `psql --set ON_ERROR_STOP=1 --command ANALYZE`.

Do not print credentials or source dump content.

- [ ] **Step 3: POSIX wrapperを実装する**

`restore.sh <latest|object-key>`:

```text
resolve exact key before stopping backend
call describe and display manifest metadata
require exact confirmation input: vrcwebmap
record whether backend is running
stop backend only when running
run internal restore with exact key
restart only on restore success and only when previously running
leave stopped on restore failure
exit 71 when restart alone fails
```

Use only `docker compose --env-file .env.backup --profile backup ...`.

- [ ] **Step 4: PowerShell wrapperを実装する**

Mirror the same steps and exit codes using `$LASTEXITCODE`, `try/finally`, and
`Read-Host`. Do not use a broader automatic restart in `finally`.

- [ ] **Step 5: testsとstatic syntax checksを実行する**

```bash
sh -n ops/db-backup/db-backup.sh
sh -n ops/db-backup/restore.sh
pwsh -NoProfile -Command \
  '[void][System.Management.Automation.Language.Parser]::ParseFile(
    "ops/db-backup/restore.ps1",[ref]$null,[ref]$null)'
sh ops/db-backup/tests/integration.sh
```

Expected: syntax and all restore scenarios pass。

- [ ] **Step 6: commit**

```bash
git add ops/db-backup
git commit -m "feat: restore PostgreSQL backups safely"
```

---

### Task 4: 運用docs、scheduler例、Wasabi smoke testを完成する

**Files:**
- Modify: `README.md`
- Modify: `AGENTS.md`
- Create: `ops/db-backup/cron.example`
- Create: `ops/db-backup/windows-task.example.ps1`

- [ ] **Step 1: Linux/Windows scheduler例を追加する**

Cron uses an absolute repository path and:

```cron
0 3 * * 0 cd /opt/vrcwebmap && docker compose --env-file .env.backup --profile backup run --rm db-backup backup
```

PowerShell example changes directory, executes the same Compose command, and
exits with `$LASTEXITCODE` for Task Scheduler history.

- [ ] **Step 2: READMEへexact commandsとfailure semanticsを書く**

Document:

```sh
docker compose --env-file .env.backup --profile backup run --rm db-backup backup
docker compose --env-file .env.backup --profile backup run --rm db-backup list
./ops/db-backup/restore.sh latest
./ops/db-backup/restore.sh "backups/vrcwebmap-20260702T030000Z.dump"
```

and PowerShell equivalents. Include Wasabi endpoint/addressing settings、
least-privilege actions、4-generation default、failed restore leaves backend
stopped、manual backend restart command。

- [ ] **Step 3: no-secret and config regressionを実行する**

```bash
git grep -nE 'AWS_SECRET_ACCESS_KEY=.+|BACKUP_PGPASSWORD=.+' -- \
  ':!.env.backup.example'
docker compose --env-file .env.backup.example --profile backup config >/tmp/vrcwebmap-backup-compose.yml
```

Expected: secret grep has no matches; Compose config succeeds after dummy
example values are supplied。

- [ ] **Step 4: Wasabi test bucket smoke testを行う**

With temporary least-privilege credentials:

```bash
docker compose --env-file .env.backup --profile backup run --rm db-backup backup
docker compose --env-file .env.backup --profile backup run --rm db-backup list
```

Verify dump/checksum/manifest and delete the test objects after validation.
Do not run destructive restore against production; restore into an isolated test DB.

- [ ] **Step 5: full verification and commit**

```bash
dotnet test VrcWebMap.Backend.Tests/VrcWebMap.Backend.Tests.csproj \
  --no-restore -p:RestorePackagesPath=/private/tmp/nuget-packages
sh ops/db-backup/tests/integration.sh
git add README.md AGENTS.md ops/db-backup
git commit -m "docs: document PostgreSQL backup operations"
git status --short
```

Expected: tests pass and worktree clean。
