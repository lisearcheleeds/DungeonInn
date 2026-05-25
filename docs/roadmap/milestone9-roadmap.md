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
- 合算処理は View / Presenter に近い浅い層の仮実装として扱う
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

`GuildCombinedInventoryViewService` は UI 表示用の合算 inventory を作る浅い層の仮実装とする。Domain の inventory を統合してはならない。

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
- UI 上の合算 inventory 表示は View / Presenter に近い浅い層の仮実装として扱う

## 実装タスク案

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
- docs とコードコメントに仮実装であることを記録する

完了条件:

- [ ] UI 上は合算資産として表示される
- [ ] 内部 inventory は施設別に保持される
- [ ] 合算処理が Domain Entity に入っていない
- [ ] コードコメントに浅い層の仮実装であることが明記されている

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
