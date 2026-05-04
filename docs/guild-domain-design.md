# Adventurer Guild Domain Design

## 目的

このドキュメントは、プレイヤーが冒険者を直接操作せず、AI が動かす冒険者たちの行動を眺めながら冒険者ギルドを運営するゲームの Domain 設計をまとめる。

本設計では、ダンジョン探索の詳細なルールはまだ確定しない。先に、冒険者ギルド、キャラクター、施設、アイテム、取引、雇用の Domain を定義する。

## ゲームの基本方針

- プレイヤーは冒険者を直接操作しない。
- 冒険者は AI によって自律的に行動する。
- プレイヤーが操作できる対象は以下に限定する。
  - カメラ操作
  - 各エンティティの詳細表示
  - 冒険者ギルドの運営
- 冒険者がどの施設を利用するか、何を購入するか、何を売却するかは AI が判断する。
- ギルドはダンジョン探索結果を直接受け取らない。
- ギルドの収支は、冒険者との施設利用・売買・雇用によって発生する。

## レイヤー方針

Domain 層はゲームルールと状態遷移を表現する。UnityEngine、MonoBehaviour、Lighthouse、VContainer には依存しない。

Application 層は Domain を組み合わせてユースケースを実行する。AI 判断ロジックも Application 層に置く。ただし、Application/AI も Pure C# とし、UnityEngine.Vector3 など Unity 型には依存しない。

View 層は表示、カメラ、ポップアップ、アニメーション、Unity 座標変換を担当する。

## 中心となる Domain

### Character

`Character` は冒険者にもギルドスタッフにもなり得る人物を表す。

キャラクターは以下を持つ。

- `CharacterId`
- 名前
- 現在の役割
- 冒険者としてのプロフィール
- ギルドスタッフとしてのプロフィール
- 所持金
- 所持品
- 状態

役割は例として以下を想定する。

- `Adventurer`
- `GuildStaff`
- `RecruitCandidate`
- `Inactive`

### AdventurerProfile

冒険者としての能力や状態を表す。

- レベル
- 経験値
- HP / 疲労 / 負傷
- 戦闘能力
- 探索能力
- 装備適性
- 性格・嗜好
- AI 判断に使う行動傾向

冒険者の行動はプレイヤーが直接決めない。施設利用、購入、売却、休息、食事などは Application/AI が判断する。

### StaffProfile

ギルドスタッフとしての能力を表す。

- 宿屋運営適性
- 酒場食堂運営適性
- アイテム雑貨屋運営適性
- 装備屋運営適性
- 採用コスト
- 給与

スタッフ能力は施設ポイントに変換され、施設の品質やキャパシティ、品揃えに影響する。

### AdventurerGuild

冒険者ギルドは施設、資金、スタッフ、取引履歴を管理する。

ギルドはダンジョン探索結果を直接受け取らない。探索後に変化した冒険者の所持金、所持品、疲労、負傷などに基づき、冒険者 AI が施設利用や売買を行う。その取引結果だけがギルドの収支に反映される。

主な責務:

- ギルド資金の管理
- 施設一覧の管理
- スタッフ雇用状態の管理
- 施設ポイントの集計
- 施設アップグレード状態の管理
- 取引履歴の記録
- 支出の記録

## 施設 Domain

施設はギルドが運営する収益源であり、スタッフによる施設ポイントで自動的に強化される。

### Inn

宿屋。冒険者の休息と回復に関わる。

- 回復効率
- キャパシティ
- 宿泊料金
- 利用可能な部屋ランク

収入:

- 宿泊代

### Tavern

酒場食堂。食事による一時的なバフを提供する。

- メニュー品質
- バフ種類
- バフ効果量
- 食事料金

収入:

- 食事代

### GeneralStore

アイテム雑貨屋。消耗品や素材関連アイテムを扱う。

- 取扱商品の種類
- 商品品質
- 在庫または販売可能ラインナップ

収入:

- アイテム販売代金

支出:

- 冒険者が拾ってきたアイテム・素材の買取

### EquipmentShop

装備屋。装備もアイテムの一種として扱う。

- 取扱装備の種類
- 装備品質
- 新米冒険者への支給装備品質

収入:

- 装備販売代金

支出:

- 冒険者からの装備買取
- 新米冒険者への支給装備

## Item Domain

装備は Item の一種として扱う。

### ItemDefinition

アイテム種別の定義。

- `ItemId`
- 名前
- カテゴリ
- 基本価格
- 品質
- 売買可能か

カテゴリ例:

- `Material`
- `Consumable`
- `Equipment`

### EquipmentSpec

カテゴリが `Equipment` のアイテムに紐づく追加仕様。

- 装備スロット
- 攻撃力
- 防御力
- 補正値
- 推奨レベル

### Inventory

キャラクターやギルドが持つアイテムを管理する。

- アイテム ID
- 個数
- 個別装備インスタンスが必要な場合はインスタンス ID

## Commerce Domain

ギルド経済は探索結果ではなく取引で動く。

### 収入

- 宿屋の代金
- 酒場食堂の売上
- アイテム販売
- 装備販売

### 支出

- 各施設の人件費
- スカウト料
- 新米冒険者への支給品
- ダンジョン内で冒険者が拾ってきたアイテム・素材の買取
- 冒険者からの装備買取

### GuildTransaction

ギルドと冒険者、またはギルド運営によって発生した収支を表す。

- `TransactionId`
- 種別
- 対象キャラクター ID
- 金額
- アイテム一覧
- 発生施設
- 発生日時またはゲーム内 tick

取引種別例:

- `InnPayment`
- `TavernPayment`
- `ItemSaleToAdventurer`
- `ItemPurchaseFromAdventurer`
- `EquipmentSaleToAdventurer`
- `EquipmentPurchaseFromAdventurer`
- `StaffSalary`
- `ScoutFee`
- `RookieSupplyCost`

## Recruitment Domain

ギルドスタッフは外部から自由に雇用できない。やってきた冒険者をスカウトする。

主なルール:

- スカウト候補は来訪中、または登録済みの冒険者のみ。
- スカウトには費用がかかる。
- スカウトされたキャラクターはギルドスタッフになる。
- スタッフ化したキャラクターの扱いは、冒険者から完全に外れるか兼任可能にするかを別途決める。
- スカウト可否には本人の能力、評判、所持金、関係性などを後から追加できる。

## Application/AI の役割

AI は Domain ルールの中で、冒険者が次に何をするかを判断する。

例:

- 宿屋で休むか
- 酒場で食事するか
- アイテムを買うか
- 装備を買い替えるか
- 素材や装備をギルドに売るか
- ダンジョンに向かうか

AI は Unity のオブジェクトや座標を直接扱わない。目的地が必要な場合も、Domain/Application 用の値型を返す。

例:

- `FacilityId`
- `CharacterId`
- `ActionDecision`
- `GridPosition`
- `WorldPosition`

`UnityEngine.Vector3` への変換は View または Infrastructure 側で行う。

## Dungeon との接続方針

ダンジョン探索の詳細設計は後から追加する。

ただし、ギルドは探索結果を直接受け取らない。ダンジョン探索は冒険者自身の状態を変化させる。

探索後に変化するもの:

- 冒険者の所持金
- 冒険者の所持品
- 冒険者の疲労
- 冒険者の負傷
- 冒険者の装備耐久や装備状態
- 冒険者の経験値

その後、冒険者 AI が施設利用や売買を判断し、ギルドとの取引が発生する。

## 推奨フォルダ構成

```text
Domain/
├── Characters/
│   ├── Character
│   ├── AdventurerProfile
│   ├── StaffProfile
│   └── CharacterRole
├── Guild/
│   ├── AdventurerGuild
│   ├── GuildFinance
│   ├── GuildStaffAssignment
│   └── Recruitment
├── Facilities/
│   ├── Facility
│   ├── InnFacility
│   ├── TavernFacility
│   ├── GeneralStoreFacility
│   └── EquipmentShopFacility
├── Items/
│   ├── ItemDefinition
│   ├── ItemCategory
│   ├── EquipmentSpec
│   └── Inventory
└── Commerce/
    ├── GuildTransaction
    ├── TransactionType
    └── PricePolicy

Application/
├── UseCase/
│   ├── RecruitStaffUseCase
│   ├── AssignStaffUseCase
│   ├── ProcessFacilityUsageUseCase
│   ├── ProcessGuildTransactionUseCase
│   └── UpgradeFacilitiesUseCase
└── AI/
    ├── IAdventurerAI
    ├── AdventurerActionDecision
    └── DefaultAdventurerAI
```

## 先に実装できる UseCase 候補

### RecruitStaffUseCase

冒険者をギルドスタッフとしてスカウトする。

入力:

- ギルド ID
- 対象キャラクター ID

処理:

- 対象がスカウト可能な冒険者か確認する。
- スカウト費用を確認する。
- ギルド資金から費用を支払う。
- キャラクターの役割をギルドスタッフに変更する。
- 取引履歴に `ScoutFee` を記録する。

### AssignStaffUseCase

雇用済みスタッフを施設に配置する。

入力:

- ギルド ID
- スタッフキャラクター ID
- 施設 ID

処理:

- 対象がギルドスタッフか確認する。
- 配置先施設が存在するか確認する。
- 施設ポイントを再計算する。
- 必要に応じて施設アップグレードを更新する。

### ProcessFacilityUsageUseCase

冒険者が施設を利用した結果を処理する。

入力:

- 冒険者 ID
- 施設 ID
- 利用内容

処理:

- 利用可能か確認する。
- 冒険者から料金を支払う。
- ギルド収入を増やす。
- 冒険者に回復、バフ、アイテム取得などの効果を与える。
- 取引履歴を記録する。

### ProcessAdventurerSaleUseCase

冒険者がアイテムや装備をギルドに売却する。

入力:

- 冒険者 ID
- 売却アイテム一覧
- 買取施設 ID

処理:

- 買取可能なアイテムか確認する。
- 買取価格を計算する。
- ギルド資金から支払う。
- 冒険者の所持品からアイテムを取り除く。
- ギルド在庫に追加する。
- 取引履歴を記録する。

### PayStaffSalaryUseCase

スタッフ人件費を支払う。

入力:

- ギルド ID
- 対象期間

処理:

- 雇用中スタッフの給与を集計する。
- ギルド資金から支払う。
- 取引履歴に `StaffSalary` を記録する。

## 未決定事項

- スタッフ化したキャラクターが冒険者活動を完全に停止するか、兼任できるか。
- 冒険者がギルドにアイテムを売る価格の決定式。
- ギルド施設のアップグレードが段階制か、連続値による性能変化か。
- 施設在庫を厳密に管理するか、施設レベルに応じた仮想ラインナップとして扱うか。
- 冒険者 AI の意思決定に性格、所持金、危険度、評判をどこまで反映するか。
- 新米冒険者への支給品が固定費か、入門時イベントごとの支出か。
