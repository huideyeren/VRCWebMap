# 地図ナビゲーションとSpotピン表示 設計

## 目的

地図の案内カードを必要に応じて閉じられるようにし、現在地へ簡単に戻れる操作を追加する。また、Spotに紐づく情報をピン色で判別できるようにする。

## スコープ

- 「Spot Atlas」案内カードの一時的な非表示
- 「現在地に戻る」ボタン
- VRChatWorldとPlaceInfoの有無によるピン色分け
- ピン色の凡例
- Spot一覧Contractへの関連情報フラグ追加

閉じた状態の永続化、現在地のサーバー保存、ユーザー独自のピン色設定は対象外とする。

## Spot Atlas案内カード

案内カード右上に、用途が読み上げ可能な閉じるボタンを追加する。

閉じた状態はReact componentのstateだけで保持する。`localStorage`、cookie、URLには保存しない。ページを再読み込みすると案内カードを再表示する。

カードを閉じても地図の中心、zoom、選択中Spot、検索条件には影響させない。

## 現在地に戻る

地図上にLeaflet controlとして「現在地に戻る」ボタンを追加する。zoom controlと重ならない位置に配置し、マウス、キーボード、screen readerから操作できるようにする。

押下時にだけ `navigator.geolocation.getCurrentPosition` を呼び出す。既存の初期表示と同じ設定を使う。

- `enableHighAccuracy: false`
- `maximumAge: 300000`
- `timeout: 5000`

取得成功時は現在地を中心としてzoom 13へ移動する。位置情報はブラウザー内の地図移動にのみ使い、API requestや永続化データへ含めない。

取得中はボタンを無効化し、重複要求を防ぐ。Geolocation APIがない場合、権限拒否、timeout、その他の取得失敗では地図位置を変更せず、画面のnotice領域へ日本語メッセージを表示する。

## Spot一覧Contract

`ListSpots.Response` は `Spot[]` の代わりに、地図表示に必要なSpot情報と関連データ有無を持つitem配列を返す。

各itemは既存のSpotプロパティに加えて次を持つ。

- `HasVRChatWorld`
- `HasPlaceInfo`

中心ContractへLeafletの色名やCSS classは含めない。Contractが表現するのは関連データの有無までとし、見た目はfrontendで決める。

`ListSpotsUseCase` はSpot一覧、VRChatWorld一覧、PlaceInfo一覧をそれぞれ一度取得し、関連するSpot IDを `HashSet<Guid>` にまとめてitemを作る。Spotごとにrepositoryを呼び出すN+1処理は行わない。

検索時は、従来どおりSpot名と説明で絞り込んだ後、返却対象へ関連データフラグを付ける。

## ピン色と優先順位

frontendは一覧itemからピン種別を次の優先順位で決める。

1. `HasVRChatWorld = true`: 紫
2. `HasPlaceInfo = true`: オレンジ
3. どちらもfalse: 青

両方がtrueの場合は紫にする。

Leafletの既定画像を色加工するのではなく、アプリケーション管理の `L.divIcon` とCSSでピンを描画する。これにより外部画像依存を増やさず、高DPI画面でも同じ色と形を維持する。

色は既存UIとのコントラストを確認し、紫、オレンジ、青の3種類をCSS custom propertyとして定義する。markerのpopup、Spot選択、直リンクの挙動は変更しない。

## 凡例

地図上に小さな凡例を置き、次を表示する。

- 青: 通常Spot
- オレンジ: 施設情報あり
- 紫: VRChatワールドあり

凡例は地図操作を妨げない位置へ置き、狭い画面でも地図を覆いすぎない大きさにする。

## 更新時の同期

VRChatWorldまたはPlaceInfoの追加・更新・削除後はSpot詳細だけでなくSpot一覧も再取得する。一覧再取得後、Reactがmarkerを再構築し、ピン色を最新状態へ更新する。

Spot検索中に関連情報を変更した場合も、現在の検索語を維持して一覧を再取得する。

## エラー処理

- Geolocation API非対応: noticeへ非対応メッセージを表示
- 位置情報権限拒否: noticeへ許可が必要であることを表示
- 位置情報timeout: noticeへ再試行可能なメッセージを表示
- Spot一覧取得失敗: 既存の一覧エラー処理を維持

位置情報取得失敗をアプリケーション全体のfatal errorにはしない。

## テスト

### UseCase

- 関連データなしのSpotで両フラグがfalse
- VRChatWorldがあるSpotで `HasVRChatWorld` がtrue
- PlaceInfoがあるSpotで `HasPlaceInfo` がtrue
- 両方あるSpotで両フラグがtrue
- 検索結果にも正しいフラグが付く
- 複数Spot間で関連情報が混ざらない

### Frontend

- ピン種別決定関数が紫を最優先にする
- Spot Atlasを閉じると現在のページ内で非表示になる
- ページ再読み込み後はSpot Atlasが再表示される
- 現在地取得成功時に地図が移動する
- 位置情報拒否・timeout時に地図を移動せずメッセージを表示する
- 関連情報の変更後に一覧を再取得する
- PNPMによるTypeScript型チェックとVite build

### Docker Compose受け入れ

- 通常Spotが青で表示される
- PlaceInfoを追加するとオレンジへ変わる
- VRChatWorldを追加すると紫へ変わる
- PlaceInfoとVRChatWorldの両方がある場合も紫になる
- 「現在地に戻る」がCloudflare Tunnel配下のHTTPS環境で動作する

## 完了条件

- Spot Atlas案内カードを閉じられ、再読み込みで再表示される。
- 現在地ボタンで現在地へ戻れ、位置情報はサーバーへ送信されない。
- 通常Spot、施設情報あり、VRChatワールドありをピン色で判別できる。
- VRChatワールドの紫が施設情報のオレンジより優先される。
- 関連情報の追加・削除後にピン色が最新状態へ更新される。
