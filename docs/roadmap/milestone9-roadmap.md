# Milestone 9 Roadmap - ギルド経営 UI / Market

## ゴール

Milestone 9 では、プレイヤーが宿屋ギルドの状態を確認し、施設アップグレードと Market 取引を行えるようにする。

対象画面は以下の 3 つとする。

- ダンジョン情報
- 冒険者ギルド管理
- 市場

3 画面はいずれも ScreenStack を利用した個別のウィンドウ型 UI として扱う。HUD 内のタブではなく、それぞれ独立した ScreenStack window として開閉できる構成にする。

## 対象外

以下は Milestone 9 では実装しない。

- スタッフ管理
- GuildStaff の雇用、配置、解雇
- GuildStaffBehavior / GuildStaffAssignment を利用する新規ゲームフロー
- スタッフ能力値による施設ポイント反映
- 宿泊料、食事代、販売価格倍率などの価格設定 UI
- 生産機能
- 冒険者が雑貨屋 / 装備屋の商品を購入する AI
- 酒屋の攻撃力バフ効果

生産、購入 AI、酒屋バフ、新米冒険者の 0 ゴールド装備購入は `docs/roadmap/milestone9.5-roadmap.md` に送る。

## 既存在庫構造の前提

現状、冒険者から買い取ったアイテムは施設の inventory に入る。

- `Material` は `GeneralStore`
- `Equipment` は `EquipmentShop`

`SellItemsUseCase` は冒険者の所持品を該当施設へ売却し、施設のゴールドを冒険者へ支払う。したがって、買い取ったアイテムを施設アップグレードや Market 納品に使う前提は既存構造と一致している。

ただし、Milestone 9 の UI では施設ごとのゴールドや inventory は表示しない。冒険者ギルドの情報としては、全施設 inventory とギルド inventory を合算した値を表示する。

## アイテム分類方針

`ItemCategory` は廃止し、`ItemTag` へ完全移行する。

理由:

- 1 つのアイテムが複数の性質を持つため、単一カテゴリでは表現しづらい
- 例: ポーションは回復アイテムであり、売買対象でもあり、将来は店舗ラインナップや MarketOffer の対象にもなる
- 武器、防具、アクセサリー、素材、貴重品、売却用などをカテゴリ enum で排他的に扱うと、今後の Market / 生産 / 店舗販売で混乱する

方針:

- `ItemTag` は flags enum とする
- 1 つの `ItemMaster` は複数の `ItemTag` を持てる
- 回復判定、素材判定、装備判定、売却先判定は `ItemTag` を見る
- `ItemCategory` は Runtime から削除し、互換用 alias としても残さない

重要:

- 内部データは施設ごとに分けて保持する
- UI 上だけ合算されているように見せる
- 合算処理は Application Service として扱い、Domain の inventory 統合ではなく、UI / 判定用の読み取り snapshot を作る責務に限定する
- 実装時は docs とコードコメントの両方に「UI 上の合算表示であり、内部 inventory は施設別に保持する」ことを明記する
- 将来、施設ごとの在庫表示や施設別経済ルールを追加できるように、Domain の inventory 統合は行わない

## 共通 UI 方針

### ScreenStack

3 画面は ScreenStack window として実装する。

- `DungeonInfoWindow`
- `GuildManagementWindow`
- `MarketWindow`

命名は実装時に既存 ScreenStack / Lighthouse の生成規約に合わせて調整する。

### Scene / LifetimeScope 境界

- UI は GameHUD / ScreenStack 系 ModuleScene の所有物として扱う
- World MainScene の scene-owned component を直接参照しない
- World / Application 情報は Application Query / DataProvider / 抽象 interface 経由で読む
- ScreenStack window から直接 Domain / WorldState を走査しない

## ダンジョン情報

### 目的

地上を含むダンジョン階層ごとの状態を閲覧できるようにする。

この画面は情報表示のみで、操作による Domain 状態変更は行わない。

### 表示内容

階層リスト:

- 階層名
- 階層番号
- 現在存在する冒険者数
- 現在存在するモンスター数
- 簡易状態

階層リスト項目の mouse over popup:

- その階層に Spawn するモンスター一覧
- その階層で Drop するアイテム一覧
- 必要であればレアリティ、Drop 元、カテゴリ

### 実装方針

階層ごとの情報は Application Query で取得する。

想定する新規入口:

- `GetDungeonLayerInfoUseCase`
- `DungeonLayerInfoSummary`
- `DungeonLayerMonsterSpawnSummary`
- `DungeonLayerItemDropSummary`

想定する ViewData:

- `DungeonInfoWindowViewData`
- `DungeonLayerListItemViewData`
- `DungeonLayerPopupViewData`

### 注意点

Monster spawn / Item drop の情報は、実際の生成ロジックや Master と矛盾しないことを優先する。表示専用の別定義を作って二重管理しない。

## 冒険者ギルド管理

### 目的

冒険者ギルドの経済状態を確認し、施設アップグレードを実行できるようにする。

### 表示内容

ギルド情報:

- 現在資金
- 収支 KPI
- 施設別売上
- 取引履歴
- 合算 inventory の主要アイテム

施設一覧:

- 施設名
- レベル
- 品質
- キャパシティ
- 現在効果
- アップグレード後の効果
- アップグレードに必要なゴールド
- アップグレードに必要なアイテム
- アップグレード可否
- 実行不可理由

### 施設アップグレード

施設アップグレードにはゴールドとアイテムを消費する。

消費元:

- ゴールドとアイテムは、ギルド inventory と全施設 inventory を横断して取得可能な場所から消費する
- 宿屋のアップグレード素材が雑貨屋 inventory にある場合でも消費できる
- UI 上は合算資産として表示する
- 内部では施設ごとの inventory を維持する

消費処理:

- 判定時は合算 inventory として足りるか確認する
- 実行時は取得可能な inventory から必要数を取り崩す
- どの施設 inventory から消費されたかは transaction / debug 確認できるようにする
- Milestone 9 ではパフォーマンス最適化より、内部 inventory を分けたまま運用できる構造を優先する

### 施設効果

宿屋:

- アップグレードで回復速度を増加させる

酒屋:

- 将来、5 分間の攻撃力バフを付与する
- バフ機能は Milestone 9.5 に送る
- Milestone 9 では preview / 表示上の将来効果として扱うか、効果未実装として明記する

雑貨屋:

- アップグレードで固定ラインナップに並ぶアイテムを増やす
- Milestone 9 では在庫が減らない固定販売ラインナップとして扱う
- 生産機能は Milestone 9.5 に送る

装備屋:

- アップグレードで固定ラインナップに並ぶ装備を増やす
- Milestone 9 では在庫が減らない固定販売ラインナップとして扱う
- 生産機能と冒険者購入 AI は Milestone 9.5 に送る

### Master 方針

施設アップグレード:

- 施設種別とアップグレードレベルをキーにして Master 化する
- 必要ゴールド
- 必要アイテム
- アップグレード後の効果値

雑貨屋 / 装備屋ラインナップ:

- `FacilityType + UpgradeLevel` を複合キーとして Master 化する
- ラインナップに出す `ItemId` を定義する
- Milestone 9 では固定ラインナップで、販売しても在庫は減らない
- 生産素材や生産時間は Milestone 9.5 で扱う

想定する Master:

- `FacilityUpgradeMaster`
- `FacilityUpgradeCostMaster`
- `FacilityLineupMaster`
- `FacilityLineupItemMaster`

### 新米冒険者の初期装備

新米冒険者に付与する装備は、Spawn 時に既に持っているものではなく、将来的に装備屋が 0 ゴールドで販売する装備として扱う。

Milestone 9.5 が完了するまで、冒険者は一時的に無装備で Spawn してよい。

Milestone 9 では以下を行う。

- Spawn 時の初期装備付与を外す
- 装備屋 0 ゴールド販売と冒険者購入 AI は Milestone 9.5 に送る

### 実装方針

想定する新規入口:

- `GetGuildManagementStatusUseCase`
- `GetFacilityUpgradePreviewUseCase`
- `UpgradeFacilityUseCase`
- `GetFacilityLineupUseCase`

想定する Application Service:

- `GuildCombinedInventoryViewService`
- `GuildInventoryWithdrawalService`

`GuildCombinedInventoryViewService` は UI 表示 / 判定用の合算 inventory snapshot を作る Application Service とする。Domain の inventory を統合してはならない。

`GuildInventoryWithdrawalService` は、施設アップグレードや Market 納品で必要になったアイテム / ゴールドを、取得可能な inventory から消費する。Milestone 9 では単純な走査でよい。

想定する ViewData:

- `GuildManagementWindowViewData`
- `GuildEconomyKpiViewData`
- `FacilityUpgradeRowViewData`
- `FacilityUpgradePreviewViewData`
- `GuildTransactionHistoryRowViewData`
- `GuildCombinedInventoryItemViewData`

## 市場

### 目的

Market が提示するアイテム販売要望に応じて、冒険者ギルドが買い取ったアイテムを納品し、ゴールドを得られるようにする。

Market は施設ではなく外部取引先として扱う。

### Market の扱い

Market は無限のゴールドを持ち、必要に応じてアイテムを生成・取引できる外部取引先とする。

ただし、プレイヤーは Market の無限資産を直接利用できない。MarketOffer を選択し、取引が成立した場合にのみ、指定されたゴールドを獲得できる。

Market には固定 Guid を定義し、取引履歴のフォーマットは既存の `ExchangeTransaction` と合わせる。

想定:

- `MarketParticipant`
- `MarketIds.DefaultMarketId`
- `MarketOffer`
- `MarketOfferMaster`

### 表示内容

常に 3 つのアイテム販売要望を表示する。

例:

- ゴブリンの耳 10 個を 1000 ゴールドで納品
- 薬草 20 個を 800 ゴールドで納品
- 鉄鉱石 5 個を 1200 ゴールドで納品

各行に表示する内容:

- 要求アイテム
- 要求数
- 報酬ゴールド
- 納品可否
- 不足数
- 実行ボタン

### Offer 数

Milestone 9 では固定 3 件でよい。

将来的には冒険者ギルドのトータルレベルに応じて選択肢数を増やす。Milestone 9 ではトータルレベルは施設レベル合計として扱う前提で docs に記録する。実装時に別定義が必要になった場合は、実装前に確認する。

### Master 方針

MarketOffer は Master で管理する。

想定する Master:

- `MarketOfferMaster`
- `MarketOfferRequirementMaster`
- `MarketOfferRewardMaster`

主な項目:

- Offer ID
- 解放に必要なギルドトータルレベル
- 要求アイテム ID
- 要求数
- 報酬ゴールド
- 表示優先度

Milestone 9 では、解放済み候補から 3 件を表示する。ランダム抽選にするか固定順にするかは、まず固定順でよい。

### 取引処理

Market 納品では、全施設 inventory とギルド inventory を横断して要求アイテムを消費する。

- 合算 inventory 上で要求数を満たすか確認する
- 実行時は取得可能な inventory から要求数を消費する
- Market から報酬ゴールドを受け取る
- 取引履歴に Market 取引として記録する

想定する新規入口:

- `GetMarketOffersUseCase`
- `FulfillMarketOfferUseCase`

想定する ViewData:

- `MarketWindowViewData`
- `MarketOfferViewData`
- `MarketOfferRequirementViewData`

## Application / View 境界

Application 側:

- UseCase はステートレスにする
- 長期状態が必要な場合は Service / StateService に分ける
- View 専用の整形済み文字列は Application Event / Domain Event には入れない
- 資金変更、施設アップグレード、Market 納品は 1 UseCase 内で状態変更とイベント発行を完結させる

View 側:

- `ViewData` suffix を使う
- ScreenStack window は Application Query / UseCase の結果を表示する
- World MainScene の scene-owned component を直接参照しない
- UI 上の合算 inventory 表示は Application Service が作る snapshot を ViewData へ変換して扱う。View / Presenter は Domain inventory を直接集計しない

## 実装前の理想設計方針

Milestone 9 は「今のコードに最小差分で足す」タスクではなく、ギルド経営 / Market / ScreenStack UI を今後拡張できる形で導入するタスクである。
互換性維持や既存実装の延命より、破壊的変更であっても責務・依存方向・寿命を正しく分けることを優先する。

### 全体原則

- 既存の曖昧な DTO / Service / namespace を延命しない。Milestone 9 の正しい概念に合わない場合はリネーム・移動・削除する。
- Runtime に互換 alias、旧 API、旧 enum、暫定 constructor を残さない。テストや移行都合だけの surface は追加しない。
- Domain はゲーム上の状態と不変条件だけを持つ。UI 表示都合、ScreenStack 都合、Unity 座標、整形済み文字列を持たない。
- Application は UseCase / Query / Service で Domain を組み合わせる。UseCase はステートレス、状態を持つものは Service / StateService に分ける。
- View は Application の Query / ViewData を表示し、Domain / WorldState / MainScene 具象を直接走査しない。
- Master は固定データの正典とする。表示用に同じ Spawn / Drop / Offer / Upgrade 定義を二重管理しない。
- GameSession scope はゲームセッション共有の Application / Domain / proxy を所有する。World MainScene scope は 3D 表現、ScreenStack / GameHUD ModuleScene scope は UI 表現だけを所有する。
- 新規概念を追加する場合は、既存類似概念、意味差分、代替不可理由、統合・削除条件を作業ログに記録する。

### 望ましい大枠

```text
Domain
  Item
    ItemTag / ItemStack / Inventory / SpecialItemIds
  Facility
    Facility / FacilityType / FacilityUpgradeState
  Commerce
    ExchangeTransaction / MarketOffer

Master
  ItemMaster / EquipmentMaster / WeaponMaster
  DungeonLayerSpawnMaster / DungeonLayerDropMaster
  FacilityUpgradeMaster / FacilityUpgradeCostMaster
  FacilityLineupMaster / FacilityLineupItemMaster
  MarketOfferMaster / MarketOfferRequirementMaster / MarketOfferRewardMaster

Application
  DungeonInfo
    GetDungeonLayerInfoUseCase
  GuildManagement
    GetGuildManagementStatusUseCase
    GetFacilityUpgradePreviewUseCase
    UpgradeFacilityUseCase
    GuildCombinedInventoryViewService
    GuildInventoryWithdrawalService
  Market
    GetMarketOffersUseCase
    FulfillMarketOfferUseCase

View
  ScreenStack
    DungeonInfoWindow
    GuildManagementWindow
    MarketWindow
  GameHUD
    Window open buttons only
```

`GuildCombinedInventoryViewService` と `GuildInventoryWithdrawalService` は Domain の inventory 統合ではない。
前者は「UI / 判定用の合算 snapshot を作る Application Service」、後者は「複数 inventory から必要量を取り崩す Application Service」として分ける。
合算 snapshot を Domain Entity に保持してはならない。

### Task 1: ScreenStack window 入口の設計

3 画面は GameHUD のタブや World MainScene の子 UI ではなく、ScreenStack が所有する独立 window とする。
HUD の責務は「開くボタンを提供する」だけであり、window の生成、初期化、閉じる処理、refresh は ScreenStack window / Presenter 側が持つ。

理想構成:

- `DungeonInfoWindow`
- `GuildManagementWindow`
- `MarketWindow`
- 各 window 用 Presenter / ViewData mapper
- HUD 側には `OpenDungeonInfoWindowUseCase` のような Application UseCase は置かず、ScreenStack open command だけを置く

依存方向:

- GameHUD Presenter -> `IScreenStackManager`
- ScreenStack Window Presenter -> Application Query / UseCase
- ScreenStack Window Presenter -X-> World MainScene 具象
- Application -X-> View / ScreenStack

Scope 前提:

- GameHUD は ScreenStack 具象 window 型や prefab を知らず、ScreenStack manager の抽象だけを使って window を開く。
- ScreenStack module は GameSession 配下の Application Query / UseCase を解決できる親 scope に置く。
- GameHUD module と ScreenStack module が兄弟 scope になる場合でも、兄弟の具象 View / Presenter を直接 inject しない。
- Window の所有者は ScreenStack module であり、GameHUD は開閉要求の発行者に留める。

実装時の注意:

- Window prefab / View は ScreenStack / UI ModuleScene 側の Addressable Factory が所有する。
- Window から `WorldScene` / `WorldLifetimeScope` / `WorldMapView` / `ActorSelectionService` などを直接参照しない。
- window close 後に GameSession 状態が変わらないよう、情報表示 window とコマンド window の責務を分ける。

### Task 2: ダンジョン情報 Query / View の設計

ダンジョン情報は「現在の状態」と「マスタ上の出現・ドロップ情報」を組み合わせた読み取り専用 Query とする。
表示専用の Spawn / Drop 定義を作らず、実際の Dungeon / Spawn / Drop / Master の正典から読む。

Application 側:

- `GetDungeonLayerInfoUseCase`
  - `IGameWorldStateReader` から現在の階層、冒険者数、モンスター数を読む
  - `IMasterRepository` から階層ごとの Spawn monster / Drop item 情報を読む
  - `DungeonLayerInfoSummary` の list を返す
- `DungeonLayerInfoSummary`
  - 階層 ID / 階層名 / 階層番号 / 現在数 / 簡易状態
  - popup 用に `DungeonLayerMonsterSpawnSummary` / `DungeonLayerItemDropSummary` を持つ

View 側:

- `DungeonInfoWindowViewData`
- `DungeonLayerListItemViewData`
- `DungeonLayerPopupViewData`

設計上の禁止:

- View が `Dungeon` / `DungeonFloor` / `GameWorldState` を直接読むこと。
- hover popup 用に Spawn / Drop の別マスタを作ること。
- 階層リストを毎フレーム再集計すること。

### Task 3: 合算 inventory 表示サービスの設計

合算 inventory は Domain の新しい在庫モデルではなく、Milestone 9 UI と判定のための読み取り snapshot である。
内部 inventory は今後もギルド inventory / 施設 inventory に分かれている必要がある。

Application 側:

- `GuildCombinedInventoryViewService`
  - `AdventurerGuild.Inventory`
  - 各 `Facility.Inventory`
  - 必要に応じて gold も `SpecialItemIds.Money` として合算
  - `List<GuildCombinedInventoryItemSummary>` へ `CopyTo` / `Fill` する
- `GuildInventorySourceSummary`
  - debug / transaction 用に「どの inventory から取れるか」を表す補助 summary

責務:

- 表示用の合算数を作る
- Upgrade / Market の事前判定に使う
- 実際の消費はしない

禁止:

- `AdventurerGuild` や `Facility` に合算 inventory field を追加すること。
- 合算 inventory をキャッシュとして Domain に保持すること。
- 施設 inventory を削除して単一 inventory に統合すること。

### Task 4: 冒険者ギルド管理 Query / View の設計

ギルド管理 window は複数の読み取りを 1 つの ViewData にまとめるが、Application 内では責務を分ける。
Presenter が複数の広い state を読んで表示を組み立てるのではなく、Query が表示に必要な summary を作る。

Application 側:

- `GetGuildManagementStatusUseCase`
  - 現在資金
  - 収支 KPI
  - 施設別売上
  - 取引履歴
  - 合算 inventory summary
- `GetFacilityUpgradePreviewUseCase`
  - 施設ごとの現在効果 / 次レベル効果 / 必要コスト / 可否 / 不可理由
- `GetFacilityLineupUseCase`
  - 雑貨屋 / 装備屋の現在固定ラインナップ

View 側:

- `GuildManagementWindowViewData`
- `GuildEconomyKpiViewData`
- `FacilityUpgradeRowViewData`
- `FacilityUpgradePreviewViewData`
- `GuildTransactionHistoryRowViewData`
- `GuildCombinedInventoryItemViewData`

方針:

- Application の `Summary` / `Status` を Presenter が View 用 `ViewData` に変換する。
- 表示文言、色、ボタン状態、tooltip 文言は ViewData / View 側の責務にする。
- Query は Domain / Master の値を返すが、整形済み UI 文字列は返さない。

### Task 5: 施設アップグレードの設計

施設アップグレードは「判定」と「実行」を分ける。
判定は preview Query、実行は `UpgradeFacilityUseCase` が 1 トランザクションで完結させる。

Domain / Master:

- `FacilityUpgradeMaster`
  - `FacilityType`
  - `FromLevel`
  - `ToLevel`
  - 効果値
- `FacilityUpgradeCostMaster`
  - 必要 gold
  - 必要 `ItemStack`
- `FacilityLineupMaster` / `FacilityLineupItemMaster`
  - `FacilityType + Level` から固定販売ラインナップを解決する

Application:

- `GetFacilityUpgradePreviewUseCase`
  - 現在施設状態と Master を照合する
  - `GuildCombinedInventoryViewService` で必要素材の充足を判定する
  - 不可理由を `FacilityUpgradeUnavailableReason` のような enum / summary で返す
- `UpgradeFacilityUseCase`
  - 対象施設と次レベル master を再解決する
  - 実行直前に必要素材の充足を再判定する
  - `GuildInventoryWithdrawalService` で gold / item を消費する
  - `Facility` のレベル / 効果状態を更新する
  - `ExchangeTransaction` または施設アップグレード履歴を記録する
  - 必要な GameEvent を状態変更後に発行する

施設効果:

- 宿屋の回復速度は実際の回復計算で参照される設定 / facility state に接続する。
- 酒屋バフは Milestone 9.5 へ延期するため、Milestone 9 の実行効果としては追加しない。
- 雑貨屋 / 装備屋は固定ラインナップ master の解放として扱い、販売在庫の減算はしない。

禁止:

- Upgrade 実行 UseCase から preview UseCase を呼ばない。
- View 側で不足素材を判定しない。
- コスト消費後に一部だけ失敗する中途半端な状態を許さない。

### Task 6: Market Master / Query / View の設計

Market は施設ではなく外部取引先である。
Market の gold / inventory を Domain Entity として細かく保持せず、Offer 成立時に報酬を生成する外部 participant として扱う。

Domain / Master:

- `MarketIds.DefaultMarketId`
  - fixed `Guid`
  - `ExchangeTransaction` の counterparty として使う外部取引先 ID
- `MarketParticipant` は Milestone 9 では原則作らない
  - 状態を持たない固定外部取引先は Domain Entity にしない
  - 表示名が必要な場合は View / Master 側の label 解決に留める
  - 将来 Market ごとの在庫、評判、更新周期など状態が必要になった場合に初めて Entity / StateService 化を検討する
- `MarketOfferMaster`
  - offer ID
  - 解放に必要なギルドトータルレベル
  - 表示優先度
- `MarketOfferRequirementMaster`
  - 要求 `ItemStack`
- `MarketOfferRewardMaster`
  - 報酬 `ItemStack`。Milestone 9 では gold のみでも `ItemStack(SpecialItemIds.Money, amount)` として扱う

Application:

- `GetMarketOffersUseCase`
  - ギルドトータルレベルを施設レベル合計として計算する
  - master から解放済み offer を表示優先度順に 3 件選ぶ
  - `GuildCombinedInventoryViewService` で納品可否と不足数を判定する
  - `MarketOfferSummary` を返す

View:

- `MarketWindowViewData`
- `MarketOfferViewData`
- `MarketOfferRequirementViewData`

方針:

- Milestone 9 では固定順でよい。ランダム抽選や日替わり offer は導入しない。
- Offer 選択数や更新条件が必要になった場合は、Milestone 9.5 以降で MarketOfferStateService として追加する。

### Task 7: Market 納品の設計

Market 納品は 1 UseCase 内で、要求判定、inventory 消費、報酬付与、履歴記録、イベント発行まで完結させる。

Application:

- `FulfillMarketOfferUseCase`
  - offer ID を受け取る
  - master から要求 / 報酬を再解決する
  - 現在のギルドトータルレベルで offer が解放済みか確認する
  - `GuildInventoryWithdrawalService` で要求 item を取り崩す
  - 報酬 gold をギルド inventory に追加する
  - `ExchangeTransaction` に Market 取引として記録する
  - 状態変更後に event を発行する

`GuildInventoryWithdrawalService` の方針:

- 消費順序は明確に固定する。例: Guild inventory -> Inn -> Tavern -> GeneralStore -> EquipmentShop。
- 消費結果として `GuildInventoryWithdrawalResult` を返し、どの inventory から何個消費したかを履歴・debug で追えるようにする。
- 不足時は状態を変えず、失敗 result または例外で止める。

禁止:

- Query で計算した可否を信用して実行時再判定を省略すること。
- Market の無限資産をプレイヤーが直接引き出せる形にすること。
- Market を Facility として扱うこと。

### Task 8: 新米冒険者の初期装備付与削除の設計

Spawn 時の初期装備直接付与は、将来の装備屋 0 ゴールド購入 AI と責務が衝突するため削除する。
ActorFactory は Actor の基礎状態を作るだけに戻し、装備支給・購入・取引履歴は UseCase / AI の責務にする。

方針:

- `ActorArchetypeMaster` に初期装備 item を直接持たせない。
- `ActorFactory` / `ActorFactoryCore` は equipment inventory 付与や取引履歴記録を行わない。
- `SpawnAdventurerUseCase` / spawn orchestrator からも新米装備の直接付与を外す。
- Milestone 9.5 の Rookie Equipment Purchase で、装備屋ラインナップから 0 gold item を購入し、自動装備する流れを追加する。

Milestone 9 で許容する一時状態:

- 新米冒険者は無装備で spawn する。
- 無装備時の戦闘は自然武器 / 素手の既存計算で成立させる。
- 無装備 Actor が combat power 計算、attack calculator 解決、AI 更新、戦闘進行で例外にならないことを EditMode test または既存テスト拡張で確認する。
- ただし「直接付与を外しただけで後続導線がない」ことを M9.5 roadmap と作業ログに明記する。

禁止:

- 直接付与を別名メソッドへ移して温存すること。
- 互換用の `GiveInitialEquipment` API を残すこと。
- Factory が装備購入 AI の代わりをすること。

### 実装開始前ゲート

各 Task の実装前に、Codex は以下を確認し、問題があれば実装せず `review/{task_id}_question.md` で Claude Code に返す。

- 新規 public / internal API が production 契約として必要か。
- 既存類似 DTO / Service / Master と責務が重複していないか。
- Domain / Application / View / Master のどこが正しい所有者か。
- UseCase が他 UseCase を呼ぶ構造になっていないか。
- View が `IGameWorldStateReader` や Domain 集約を直接読む構造になっていないか。
- LifetimeScope が Prefab / View 実体 / Content catalog を持つ構造になっていないか。
- 破壊的変更を避けるためだけに旧 API / alias / optional constructor を残していないか。

## 実装タスク案

以下の Task 1〜8 は実装順序と完了条件の一覧である。
設計判断は上記「実装前の理想設計方針」を正とする。
この一覧と理想設計方針が矛盾する場合は、理想設計方針を優先し、必要に応じてこの一覧側を更新してから実装する。

### Task 1: ScreenStack window 入口

- ダンジョン情報、冒険者ギルド管理、市場を個別 ScreenStack window として開けるようにする
- HUD から各 window を開く導線を作る

完了条件:

- [ ] 3 つの window を個別に開閉できる
- [ ] World 操作へ戻れる
- [ ] Window 間で scene-owned component の直接参照がない

### Task 2: ダンジョン情報 Query / View

- 階層ごとの冒険者数、モンスター数、Spawn monster、Drop item を取得する
- 階層リストと hover popup を表示する

完了条件:

- [ ] 地上を含む階層リストが表示される
- [ ] 階層ごとの冒険者数が表示される
- [ ] Mouse over popup で Spawn monster / Drop item が表示される

### Task 3: 合算 inventory 表示サービス

- ギルド inventory と全施設 inventory を UI 表示用に合算する
- 施設ごとの inventory 表示は行わない
- docs とコードコメントに、Domain の inventory 統合ではなく Application Service による snapshot であることを記録する

完了条件:

- [ ] UI 上は合算資産として表示される
- [ ] 内部 inventory は施設別に保持される
- [ ] 合算処理が Domain Entity に入っていない
- [ ] View / Presenter が Domain inventory を直接集計していない
- [ ] コードコメントに Application Service による snapshot であり、内部 inventory は施設別に保持することが明記されている

### Task 4: 冒険者ギルド管理 Query / View

- 現在資金、収支 KPI、施設別売上、取引履歴を表示する
- 施設ごとのアップグレード preview を表示する

完了条件:

- [ ] 冒険者ギルドの合算資金が表示される
- [ ] 収支 KPI と施設別売上が表示される
- [ ] 取引履歴が表示される
- [ ] 施設アップグレードに必要なゴールド / アイテムが表示される

### Task 5: 施設アップグレード

- `FacilityUpgradeMaster` 系を追加する
- `UpgradeFacilityUseCase` を追加する
- ゴールドとアイテムを取得可能な inventory から消費する
- 施設効果を反映する

完了条件:

- [ ] 合算 inventory 上で素材が足りる場合にアップグレードできる
- [ ] 実行時に内部 inventory から素材とゴールドが消費される
- [ ] 宿屋の回復速度増加が反映される
- [ ] 雑貨屋 / 装備屋の固定ラインナップがレベルに応じて増える
- [ ] 酒屋バフ効果は M9.5 に延期されている

### Task 6: Market Master / Query / View

- MarketOffer Master を追加する
- 固定 3 件の MarketOffer を表示する
- 納品可否と不足数を表示する

完了条件:

- [ ] MarketOffer が Master から取得される
- [ ] 3 件の販売要望が表示される
- [ ] 合算 inventory 上の所持数で納品可否が表示される

### Task 7: Market 納品

- `FulfillMarketOfferUseCase` を追加する
- 全施設 inventory / ギルド inventory から要求アイテムを消費する
- Market から報酬ゴールドを受け取る
- 取引履歴に記録する

完了条件:

- [ ] 要求アイテムが足りる場合だけ納品できる
- [ ] 納品後に要求アイテムが消費される
- [ ] 報酬ゴールドが増える
- [ ] 取引履歴に Market 取引として表示される

### Task 8: 新米冒険者の初期装備付与を外す

- Spawn 時に初期装備を直接付与しない
- 装備屋 0 ゴールド販売と購入 AI は M9.5 に送る

完了条件:

- [ ] 新米冒険者が Spawn 時に装備を直接受け取らない
- [ ] 無装備 Actor が combat power 計算、attack calculator 解決、AI 更新、戦闘進行で例外にならない
- [ ] M9.5 に装備購入 AI のタスクがある

## 表示更新方針

Milestone 9 の UI は常時毎フレーム再集計しない。

- Window を開いた時に初回取得する
- 施設アップグレード、Market 納品、取引発生後に明示 refresh する
- 将来必要になれば経済状態 revision / dirty flag を追加する

## 完了条件

- [ ] ダンジョン情報、冒険者ギルド管理、市場が個別 ScreenStack window として実装されている
- [ ] ダンジョン情報で階層リスト、冒険者数、Spawn monster、Drop item が確認できる
- [ ] 冒険者ギルド管理で現在資金、収支 KPI、施設別売上、取引履歴が確認できる
- [ ] 施設アップグレードにゴールドとアイテムを消費できる
- [ ] UI 上は合算 inventory として表示されるが、内部 inventory は施設別に保持されている
- [ ] MarketOffer が Master 管理され、固定 3 件を表示できる
- [ ] Market 納品でアイテムを消費し、ゴールドを獲得できる
- [ ] 生産、購入 AI、酒屋バフ、新米装備購入が M9.5 に記録されている
- [ ] `uloop.cmd compile --project-path Client` が成功している
- [ ] `uloop.cmd run-tests --project-path Client --test-mode EditMode` が成功している
- [ ] Play 30 秒確認で error が出ていない

## Codex implementation log - 2026-05-26

Scope:
- Implemented Task 1 through Task 8 for Milestone 9.
- Kept the implementation on the breaking-change design path: no compatibility wrapper, no old rookie-equipment API, no domain inventory merge for UI snapshots.

Task review log:
- Task 1: ScreenStack window entry from HUD implemented, compile checked, separate Agent review passed after fixes.
- Task 2: Dungeon layer info query/view implemented, compile checked, separate Agent review passed after popup visibility fix.
- Task 3: Combined guild/facility inventory snapshot service implemented, compile checked, separate Agent review passed after duplicate requirement aggregation fix.
- Task 4: Guild management query/view implemented, compile checked, separate Agent review passed after removing UseCase-to-UseCase dependency and replacing placeholder upgrade preview.
- Task 5: Facility upgrade preview/execution implemented, compile checked, separate Agent review passed after source-by-source transaction result fix.
- Task 6: Market master/query/view implemented, compile checked, separate Agent review passed.
- Task 7: Market fulfillment implemented, compile checked, separate Agent review passed after reward duplication in source transactions was removed. UI delivery buttons were then added and re-reviewed with no Blocker/High findings.
- Task 8: Rookie initial equipment grant removed, compile checked, separate Agent review passed after removing stale SpawnAdventurerUseCase arguments and ActorArchetypeMaster.InitialEquipmentItemIds.

Guideline check:
- Rechecked the guideline set required by AGENTS.md during implementation/review: lighthouse-patterns, coding-rules, domain-design-guidelines, application-boundary-guidelines, implementation-quality-guidelines, debugging-policy, self-review-guidelines.
- No hard-gate violation remains in the implemented scope.

Final verification:
- `uloop.cmd compile --project-path Client`: Success true, ErrorCount 0, WarningCount 0.
- `uloop.cmd run-tests --project-path Client --test-mode EditMode`: Passed, 308/308.
- Play verification: opened Launcher, entered Play mode, invoked StartGameButton through the normal Title -> World route, ran for 30 seconds, then stopped.
- Required log confirmed: `[World] GameWorldState initialized. Facilities=3 DungeonFloors=1 Actors=0`.
- Error log check after final normal-route Play verification: 0 errors.

## Codex fix log - 2026-05-26

Fixes:
- Moved GameHUD-owned ScreenStack contents from `View/Scene/ModuleScene/ScreenStack/Milestone9` to `View/Scene/ModuleScene/GameHUD/ScreenStack`.
- Moved ScreenStack window prefabs from `Runtime/Prefab/ScreenStack` to `Runtime/Prefab/GameHUD/ScreenStack`.
- Regenerated `ScreenStackEntityFactory.g.cs` through the Lighthouse ScreenStack generator after namespace relocation.
- Added `SetupGameHudScreenStackPrefabsOneShot` and regenerated the three window prefabs through `PrefabUtility.SaveAsPrefabAsset`.
- Removed direct GameSession UseCase injection from ScreenStack window instances. GameHUD now builds `ScreenStackData` with ViewData/callbacks before opening the window, and windows only render data.
- Added feature execution logs for GameHUD ScreenStack open requests, displayed windows, and market fulfillment.

Verification:
- Prefab selection check: selected DungeonInfoWindow / GuildManagementWindow / MarketWindow prefabs in Unity; Error logs 0.
- DungeonInfoButton: clicked through uLoop UI simulation, confirmed `[GameHUD.ScreenStack] DungeonInfoWindow displayed. Layers=3`, kept displayed for 30 seconds, Error logs 0.
- GuildManagementButton: clicked through uLoop UI simulation, confirmed `[GameHUD.ScreenStack] GuildManagementWindow displayed. Facilities=3 Inventory=5`, kept displayed for 30 seconds, Error logs 0.
- MarketButton: clicked through uLoop UI simulation, confirmed `[GameHUD.ScreenStack] MarketWindow displayed. Offers=3`, kept displayed for 30 seconds, Error logs 0.
- `uloop.cmd compile --project-path Client`: Success true, ErrorCount 0, WarningCount 0.
- `uloop.cmd run-tests --project-path Client --test-mode EditMode`: Passed, 308/308.
