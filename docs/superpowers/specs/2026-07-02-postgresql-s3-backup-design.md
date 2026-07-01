# PostgreSQL S3バックアップ 設計

## 目的

Docker Composeで運用するPostgreSQLのアプリケーションDBを、AWS S3またはS3互換オブジェクトストレージへ定期バックアップできるようにする。

バックアップはホストOSのcronまたはタスクスケジューラーから実行する。リストアは誤操作を防ぎながら、必要に応じてバックエンドを一時停止し、成功時だけ元の稼働状態へ戻せるようにする。

## 対象

- PostgreSQLのアプリケーションDB `vrcwebmap`
- PostgreSQL 18の論理バックアップとリストア
- AWS S3およびS3互換ストレージへの保存
- バックアップ世代の保持と削除
- 明示したオブジェクトキーまたは最新世代からのリストア
- Linuxなどで使うPOSIX shellのリストア用ラッパー
- Windowsで使うPowerShellのリストア用ラッパー
- cronおよびタスクスケジューラーからの実行例

## 対象外

- PostgreSQLクラスタ全体のロール、tablespace、グローバルオブジェクト
- PostgreSQLの物理バックアップ、PITR、WALアーカイブ
- Redisのバックアップ
- Docker socketをバックアップコンテナへ渡す構成
- バックアップ失敗時の通知サービス
- オブジェクトストレージ側のLifecycle設定
- Spotに紐付かないVRChatワールドの登録機能

バックアップ失敗時の通知は、cron、タスクスケジューラー、監視サービスなど、呼び出し側が終了コードとログを使って行う。

## 全体構成

常駐するサイドカーではなく、Docker Composeのone-shot serviceとして `db-backup` を追加する。

バックアップはホストOSから次の形で起動する。

```sh
docker compose --env-file .env.backup run --rm db-backup backup
```

`db-backup` は任意の `backup` profileに所属し、通常の `docker compose up` では起動しない。接続先として `postgres` serviceだけに依存し、`backend` serviceやDocker socketには依存しない。

専用イメージは `ops/db-backup/` に置き、次のツールを含める。

- PostgreSQL 18 client
- AWS CLI v2
- バックアップ、一覧、リストアを実行するスクリプト

PostgreSQL clientのmajor versionは、PostgreSQL serverと同じ18に固定する。

## コマンド

コンテナ内のエントリーポイントは、少なくとも次のサブコマンドを提供する。

- `backup`: バックアップを作成してアップロードし、保持世代を整理する。
- `list`: 完了済みのバックアップ世代を一覧表示する。
- `restore <object-key|latest>`: 指定世代を検証してDBへリストアする。

`restore` はコンテナ内のDB操作だけを担当する。バックエンドの停止と再起動は、ホスト側のPOSIX shellまたはPowerShellラッパーが担当する。

## 設定

主な環境変数は次のとおりとする。

### PostgreSQL

- `PGHOST`
- `PGPORT`
- `PGDATABASE`
- `PGUSER`
- `PGPASSWORD`

`PGDATABASE` の既定値は `vrcwebmap` とする。

### オブジェクトストレージ

- `S3_BUCKET`
- `S3_PREFIX`
- `S3_ENDPOINT_URL`
- `S3_ADDRESSING_STYLE`
- `AWS_REGION`
- `AWS_ACCESS_KEY_ID`
- `AWS_SECRET_ACCESS_KEY`
- `AWS_SESSION_TOKEN`

`S3_ENDPOINT_URL` が空の場合はAWS S3を使い、設定されている場合はWasabiなどのS3互換endpointを使う。

`S3_ADDRESSING_STYLE` は `auto`、`path`、`virtual` を受け付け、既定値は `auto` とする。

`AWS_SESSION_TOKEN` は一時認証情報を使う場合だけ設定する。サーバー側暗号化の指定は任意とし、対象ストレージが対応している場合だけ有効化できるようにする。

### 保持世代

- `BACKUP_RETENTION_COUNT`

既定値は `4` とし、1以上の整数だけを許可する。保持数の判定対象は、完了マーカーが存在する正常な世代だけとする。

## 認証情報

設定例として `.env.backup.example` をリポジトリへ追加する。実際の認証情報を含む `.env.backup` はGit管理から除外する。

ログにはsecret、password、session tokenを出力しない。オブジェクトストレージの認証情報には、対象bucketとprefixに限定したList、Get、Put、Delete権限だけを付与する。

本番のS3 endpointにはHTTPSを使う。HTTPはローカルの結合テストなど、閉じた検証環境に限定する。

## バックアップ形式

`pg_dump` のcustom archive formatを使う。

```sh
pg_dump --format=custom --no-owner --no-acl
```

出力ファイル名にはUTC時刻を使う。

```text
vrcwebmap-20260702T030000Z.dump
vrcwebmap-20260702T030000Z.dump.sha256
vrcwebmap-20260702T030000Z.dump.json
```

1世代は次の三つのオブジェクトで構成する。

- `.dump`: PostgreSQL custom archive
- `.sha256`: dumpのSHA-256
- `.json`: DB名、作成時刻、dumpサイズ、SHA-256、形式などを記録するmanifest

`.json` manifestを最後にアップロードし、世代の完了マーカーとして扱う。manifestがない世代は、一覧、`latest` の解決、保持世代の計数、リストアの対象に含めない。

## バックアップ処理

バックアップは次の順序で実行する。

1. 共有作業volume上のlockを取得し、同時実行を防ぐ。
2. 必須環境変数と値の形式を検証する。
3. PostgreSQLへの接続を確認する。
4. オブジェクトストレージへの接続と必要な操作権限を確認する。
5. 稼働中のDBからcustom archiveを一時領域へ作成する。
6. `pg_restore --list` でarchiveを検証する。
7. SHA-256とmanifestを生成する。
8. `.dump` をアップロードする。
9. `.sha256` をアップロードする。
10. 完了マーカーとして `.json` manifestをアップロードする。
11. 完了済み世代を新しい順に並べ、保持数を超えた世代の三つのオブジェクトを削除する。
12. lockとローカル一時ファイルを必ず削除する。

バックアップ作成またはアップロードに失敗した場合は、世代削除を実行せず、0以外の終了コードで終了する。

新規世代のアップロードは成功したが、古い世代の削除だけに失敗した場合も0以外で終了する。ログと終了コードから、バックアップ失敗と世代削除失敗を区別できるようにする。

途中までアップロードされた不完全なオブジェクトは正常世代として扱わない。後続の正常なバックアップを妨げず、運用者がログとobject keyを確認して削除できる状態にする。

## `latest` の解決

`latest` は固定名のdumpを指すのではなく、完了済みmanifestの作成時刻から最新世代の正確なobject keyを解決する指定とする。

リストア用ラッパーは、バックエンドを停止する前に次を行う。

1. `latest` または明示指定を正確なobject keyへ解決する。
2. manifestを取得して、DB名、作成時刻、サイズ、SHA-256を表示する。
3. リストア対象が存在し、完了済み世代であることを確認する。

解決後は、そのリストア処理の途中で新しいバックアップが追加されても対象が変わらないよう、正確なobject keyだけを使う。

## リストア用ラッパー

POSIX shell版とPowerShell版で同じ状態遷移と終了コードを実装する。

ラッパーは次の順序で動作する。

1. 対象世代を正確なobject keyへ解決し、metadataを表示する。
2. 利用者へ確認を求め、対象DB名 `vrcwebmap` の入力が一致した場合だけ続行する。
3. `backend` serviceが稼働中か記録する。
4. 稼働中だった場合だけ `backend` serviceを停止する。
5. `db-backup restore <resolved-object-key>` を実行する。
6. リストアに成功し、元々 `backend` が稼働中だった場合だけ再起動する。
7. 元々停止していた場合は、リストア成功後も停止したままにする。

object keyの解決、metadata取得、確認入力など、バックエンド停止前の処理に失敗した場合は、バックエンドの状態を変更しない。

バックエンド停止後にリストアが失敗した場合は、安全のためバックエンドを停止したままにし、0以外の終了コードで終了する。自動で再起動しない。

リストア成功後のバックエンド再起動に失敗した場合も0以外で終了し、リストア自体が成功済みであることと再起動失敗をログで区別する。

## リストア処理

コンテナ内の `restore` は次の順序で実行する。

1. `.dump`、`.sha256`、`.json` を一時領域へダウンロードする。
2. manifestのDB名が対象DBと一致することを確認する。
3. dumpのファイルサイズとSHA-256がmanifestおよび `.sha256` と一致することを確認する。
4. `pg_restore --list` でarchiveを検証する。
5. 対象DBに残っている接続を切断する。
6. 既存DBへ単一transactionでリストアする。
7. 成功後に `ANALYZE` を実行する。
8. lockとローカル一時ファイルを必ず削除する。

リストアは次の主要optionを使う。

```sh
pg_restore \
  --clean \
  --if-exists \
  --exit-on-error \
  --single-transaction \
  --no-owner \
  --no-acl
```

既存DBを削除して再作成せず、対象DB内の既存オブジェクトを削除して復元する。`--single-transaction` により、復元途中の失敗で部分的なschemaやデータを残さない。

## 排他制御

バックアップ、一覧、リストアは同じ共有作業volumeを使う。DBを変更するリストアとバックアップが同時に走らないよう、少なくとも `backup` と `restore` は共通lockで排他する。

lockを取得できない場合は待ち続けず、処理中であることが分かる0以外の終了コードで終了する。ホスト側schedulerによる重複起動も、このlockで防ぐ。

## ログと終了コード

各コマンドは標準出力と標準エラーへ、時刻、処理段階、対象DB、object key、結果を出力する。認証情報は出力しない。

終了コードは少なくとも次を区別できるようにする。

- 入力または設定不備
- 排他lock取得失敗
- PostgreSQL接続またはdump失敗
- S3接続、upload、download失敗
- archiveまたはchecksum検証失敗
- リストア失敗
- 世代削除だけの失敗
- リストア成功後のバックエンド再起動失敗

具体的な数値は実装計画で固定し、POSIX shell版とPowerShell版で揃える。

## 運用手順

READMEへ次の内容を追加する。

- `.env.backup` の作成方法
- AWS S3、WasabiなどのS3互換ストレージの設定例
- cronから週1回実行する例
- Windowsタスクスケジューラーから週1回実行する例
- バックアップ一覧の確認方法
- object keyを明示したリストア方法
- `latest` を使うリストア方法
- リストア失敗時にバックエンドが停止したままになること
- 手動での復旧確認とバックエンド再起動方法

## テストと確認

### イメージと設定

- 専用イメージにPostgreSQL 18 clientとAWS CLI v2が含まれる。
- `S3_ENDPOINT_URL` が空の場合はAWS S3設定を使う。
- `S3_ENDPOINT_URL` がある場合はcustom endpointを使う。
- `S3_ADDRESSING_STYLE` の各値を反映できる。
- secretをログへ出力しない。
- 0以下または整数でない `BACKUP_RETENTION_COUNT` を拒否する。

### バックアップ

- ローカルのMinIOをS3互換ストレージとして結合テストする。
- バックアップ1回につき `.dump`、`.sha256`、`.json` の三つを作成する。
- manifestが最後にアップロードされる。
- archive一覧とchecksum検証に成功する。
- 5世代を作成した場合、既定設定では最新4世代だけを残す。
- manifestがない不完全世代を一覧、`latest`、保持数から除外する。
- upload失敗時に既存世代を削除しない。
- 世代削除だけが失敗した場合に異なる終了結果を返す。
- 同時実行をlockで拒否する。

### リストア

- 明示したobject keyからリストアできる。
- `latest` を正確な最新object keyへ解決してリストアできる。
- 解決後に新世代が追加されても対象object keyが変わらない。
- DB名が異なるmanifestを拒否する。
- 改ざんまたは破損したdumpをDB変更前に拒否する。
- リストア成功後に `ANALYZE` を実行する。
- リストア途中の失敗で部分的な変更を残さない。

### バックエンド制御

- 停止前の失敗ではバックエンドの状態を変更しない。
- 稼働中だったバックエンドは、リストア成功時だけ再起動する。
- 元々停止していたバックエンドは、リストア成功後も停止したままにする。
- バックエンド停止後のリストア失敗では停止したままにする。
- POSIX shell版とPowerShell版の状態遷移と終了コードが一致する。

### 実環境

- Wasabiを使う手動smoke testでupload、list、download、deleteを確認する。
- バックアップから検証用DBへリストアし、主要テーブルと件数を確認する。
- cronとWindowsタスクスケジューラーからone-shot serviceを起動できる。

## 受け入れ条件

- ホストOSのschedulerからone-shot serviceとして週次バックアップを実行できる。
- AWS S3とS3互換ストレージを環境変数で切り替えられる。
- PostgreSQL 18のcustom archive、checksum、manifestを1世代として保存できる。
- 既定で完了済みの最新4世代を保持する。
- 不完全な世代を正常なバックアップとして扱わない。
- 明示したobject keyと `latest` の両方からリストアできる。
- dumpを検証してからDBを変更する。
- 稼働中だったバックエンドをリストア前に停止し、成功時だけ再起動する。
- リストア失敗時はバックエンドを停止したままにする。
- POSIX shellとPowerShellのリストア用ラッパーを提供する。
- 認証情報をGit管理またはログへ露出しない。
- バックアップ、世代削除、リストア、再起動の失敗を終了コードとログで判別できる。

## 参考資料

- [Docker Compose run](https://docs.docker.com/reference/cli/docker/compose/run/)
- [PostgreSQL 18 pg_dump](https://www.postgresql.org/docs/18/app-pgdump.html)
- [PostgreSQL 18 pg_restore](https://www.postgresql.org/docs/18/app-pgrestore.html)
- [AWS CLI endpoint configuration](https://docs.aws.amazon.com/cli/latest/userguide/cli-configure-endpoints.html)
- [AWS CLI S3 cp](https://docs.aws.amazon.com/cli/latest/reference/s3/cp.html)
- [MinIO container deployment](https://min.io/docs/minio/container/index.html)
