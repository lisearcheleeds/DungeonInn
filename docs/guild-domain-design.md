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

## 現在の実装状態

2026-05-04 時点では、`Client/Assets/DungeonInn/Runtime/Scripts/Domain` と `Application/UseCase` に初期実装がある。

実装は設計の概念を一部統合している。

- `CharacterId` / `FacilityId` / `TransactionId` は個別型ではなく `Guid` で扱う。
- 所持金は `int` ではなく、`Inventory` 内の `ItemStack` として扱う。通貨アイテムは `SpecialItemIds.Money = 1`。
- `AdventurerProfile` と `StaffProfile` は独立クラスではなく、現状は `Character` と `CharacterStats` に統合されている。
- `InnFacility` / `TavernFacility` / `GeneralStoreFacility` / `EquipmentShopFacility` は独立クラスではなく、`Facility` + `FacilityType` で表現している。
- `GuildTransaction` / `TransactionType` は未実装で、現状は `ExchangeTransaction` によって「こちらが渡すもの」「相手が渡すもの」を記録する。
- Application/AI はまだ未実装。現状の Application は UseCase のみ。
- `EntityIdentity` / `EntityIdentityRegistry` が追加されており、表示名、種別、有効/削除状態を Domain 側で管理できる。
- 冒険者ライフサイクル、宿屋居住権、探索目的、交換項目は初期 Domain/UseCase として実装済み。

## 中心となる Domain

### Character

`Character` は冒険者にもギルドスタッフにもなり得る人物を表す。

キャラクターは以下を持つ。

- `Guid Id`
- 名前
- 現在の役割
- 基礎能力値 `CharacterStats`
- レベル / 経験値
- HP / MP / 疲労 / ストレス / 負傷度
- 嗜好・行動傾向に使う `PreferenceSeed`
- スカウト費用 `ScoutCost`
- 給与 `IReadOnlyList<ItemStack>`
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

現状の実装では独立した `AdventurerProfile` は存在しない。冒険者として必要な値は `Character` と `CharacterStats` に統合されている。

`Character` は以下の計算メソッドを持つ。

- 最大 HP
- 最大 MP
- 移動速度
- 負傷耐性
- ストレス耐性
- 剣攻撃力
- 弓攻撃力
- 探索能力
- 装備適性

### StaffProfile

ギルドスタッフとしての能力を表す。

- 宿屋運営適性
- 酒場食堂運営適性
- アイテム雑貨屋運営適性
- 装備屋運営適性
- 採用コスト
- 給与

スタッフ能力は施設ポイントに変換され、施設の品質やキャパシティ、品揃えに影響する。

現状の実装では独立した `StaffProfile` は存在しない。スタッフ能力は `CharacterStats` から `Character.CalculateFacilityPoint(FacilityType)` で算出する。

施設種別ごとの施設ポイント計算は以下の能力値を使う。

- 宿屋: 体力、知恵、魅力
- 酒場食堂: 魅力、知恵、器用さ
- アイテム雑貨屋: 知力、魅力、知恵
- 装備屋: 筋力、器用さ、知力

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

現状の実装では、資金は `Inventory` 内の通貨アイテムとして扱う。ギルドは以下を保持する。

- `Guid Id`
- ギルド在庫 `Inventory`
- 施設一覧 `IReadOnlyList<Facility>`
- スタッフ配置一覧 `IReadOnlyList<GuildStaffAssignment>`
- 取引履歴 `IReadOnlyList<ExchangeTransaction>`

スタッフ配置は `GuildStaffAssignment` として、スタッフキャラクター ID と施設 ID の対応を持つ。

## 施設 Domain

施設はギルドが運営する収益源であり、スタッフによる施設ポイントで自動的に強化される。

現状の実装では、施設種別ごとのクラスは作らず、共通の `Facility` クラスで扱う。

`Facility` は以下を持つ。

- `Guid Id`
- `FacilityType`
- 名前
- 基本価格
- スタッフポイント
- レベル
- 品質
- キャパシティ

スタッフポイントは `Facility.ApplyStaffPoint(int)` によって適用される。現在の式は `100` ポイントごとに 1 レベル上昇し、品質とキャパシティもレベルに連動する。

施設利用は `FacilityUsageRequest` と `FacilityUsageType` で表現する。

- `Rest`
- `Meal`
- `BuyItem`
- `BuyEquipment`

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

現状の実装では、個別装備インスタンス ID は未実装。`Inventory` は `Dictionary<int, int>` によるアイテム ID と個数の管理のみを行う。

`ItemStack` は `ItemId` と `Count` を持つ値型として実装されている。

`SpecialItemIds.Money = 1` を通貨として扱う。

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

現状の実装では `GuildTransaction` と `TransactionType` はまだ存在しない。代わりに `ExchangeTransaction` を使う。

`ExchangeTransaction` は以下を持つ。

- `Guid Id`
- `OurId`
- `TheirId`
- `OurGives`
- `TheirGives`
- `OccurredAtTick`

取引種別は列挙せず、交換したアイテムの向きで記録する。取引種別が必要になった時点で `TransactionType` を追加する。

価格計算は `PricePolicy` が担当する。

- 販売価格: アイテム基本価格合計に施設レベルを掛ける。
- 買取価格: アイテム基本価格合計の半額。ただし最低 1 通貨。

## Recruitment Domain

ギルドスタッフは外部から自由に雇用できない。やってきた冒険者をスカウトする。

主なルール:

- スカウト候補は来訪中、または登録済みの冒険者のみ。
- スカウトには費用がかかる。
- スカウトされたキャラクターはギルドスタッフになる。
- スタッフ化したキャラクターの扱いは、冒険者から完全に外れるか兼任可能にするかを別途決める。
- スカウト可否には本人の能力、評判、所持金、関係性などを後から追加できる。

現状の実装では `Character.CanBeScouted` が `Adventurer` または `RecruitCandidate` の場合に true となる。

スカウト費用は `ScoutCost` として `ItemStack` の一覧を持つ。`RecruitStaffUseCase` はギルド在庫からスカウト費用を支払い、キャラクターを `GuildStaff` に変更し、`ExchangeTransaction` を記録する。

スタッフ化したキャラクターは現状 `GuildStaff` 単一ロールになり、冒険者との兼任は未実装。

## EntityIdentity Domain

実装では、ゲーム内エンティティの表示・有効状態を管理するために `EntityIdentity` が追加されている。

`EntityIdentity` は以下を持つ。

- `Guid Id`
- `EntityKind`
- 表示名
- 有効状態
- 削除 tick

`EntityKind` は以下を持つ。

- `Guild`
- `Adventurer`
- `Staff`
- `Facility`
- `Merchant`

`EntityIdentityRegistry` は ID から `EntityIdentity` を登録・解決し、削除状態を記録する。

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

## ゲームループ方針

ゲームが開始すると、一定間隔で冒険者が来訪する。

冒険者は来訪時にプロフィールが決定され、冒険者ギルドに立ち寄る。冒険者はダンジョン情報、宿屋の空き情報、自身の状態、施設の交換項目を見て、探索準備、探索、回復、売買、旅立ちを判断する。

プレイヤーは冒険者の行動を直接決めない。冒険者の次行動は Application/AI が Domain 状態を見て決定する。

### 冒険者スポーン

- Lv5 以上の冒険者は、来訪時にランダムな装備やアイテムを持つ。
- Lv2 から Lv4 の冒険者は自然スポーンしない。
- Lv1 の冒険者は新米冒険者としてスポーンする。
- Lv1 新米冒険者には、冒険者ギルドから新米用装備を無料支給する。
- Lv1 新米冒険者は、Lv5 になるまで居着く想定とする。

### 宿屋居住権

宿屋は予約制であり、宿屋の居住権は `InnReservation` で表現する。

宿屋の居住権は長期滞在枠に近い。居住権を持つ冒険者は、Lv5 以上になっても通常は旅立ちによってデスポーンしない。

現時点で、一度居着いた Lv5 以上の冒険者がデスポーンする条件は、ダンジョン内で死亡した時のみ。

`InnReservation` は以下を持つ想定とする。

- 冒険者 ID
- 宿屋施設 ID
- 予約開始 tick
- 有効状態

宿屋に空きがない場合でも、冒険者は以下を行える。

- ダンジョン探索
- 雑貨店利用
- 装備店利用
- 酒場利用
- 不要物や素材の売却・交換

宿屋に空きがなく、HP が減っている場合でも、現状では HP が減ったまま再度ダンジョンに潜る可能性がある。

### 旅立ちとデスポーン

- 旅立ち可能条件は Lv5 以上。
- Lv5 以上の来訪冒険者は、来訪直後から旅立ち可能。
- ただし、Lv5 以上でも必ず旅立つわけではなく、再度ダンジョンに潜る可能性がある。
- 宿屋居住権を持たない冒険者は、旅立つ準備が整った時点でデスポーンする可能性がある。
- 宿屋居住権を持つ冒険者は、旅立ちではデスポーンしない。
- 宿屋居住権を持つ冒険者のデスポーン条件は、現時点ではダンジョン内死亡のみ。

### 冒険者状態

冒険者のライフサイクル状態は、現時点では以下を仮置きする。

- `Arrived`
- `Resident`
- `Preparing`
- `Exploring`
- `Recovering`
- `ReadyToLeave`
- `Dead`

各状態の意味は以下。

- `Arrived`: 来訪直後。
- `Resident`: 宿屋居住権を持ち、ギルド圏に居着いている。
- `Preparing`: 探索前準備中。売買、補給、装備更新、酒場バフなどを行う。
- `Exploring`: ダンジョン探索中。
- `Recovering`: 宿屋で回復中。全回復まで宿屋から出ない。
- `ReadyToLeave`: 非居住の Lv5 以上冒険者が旅立ち可能な状態。
- `Dead`: ダンジョン内死亡。デスポーン対象。

### 時間と回復

- ゲーム内 1 日は現実時間 20 分。
- 宿屋で休憩すると、現実時間 1 分ごとに最大 HP の 10% を回復する。
- 冒険者は全回復するまで宿屋から出ない。
- 酒場バフは現実時間 10 分持続する。これはゲーム内半日分に相当する。

### 探索前準備

冒険者はダンジョン探索前に、雑貨店や装備店で消耗品購入、装備更新、不要物や素材の売却・交換を行う。

購入する消耗品や装備は、冒険者の武器、`Intelligence`、`Wisdom`、レベルで分けられたテーブルからランダムなプリセットを決定し、その状態に近づくように売買する。

所持金や在庫が不足している場合は、可能な範囲で売買する。

ユーザーが「知能」「知識」と表現した場合は、それぞれ `Intelligence` と `Wisdom` を指すものとして扱う。

### 酒場バフ

冒険者は探索前に、酒場でステータス上昇バフを受けることがある。

酒場バフを利用する確率は、`Intelligence` が高いほど、またレベルが高いほど上がる。

### 探索目的

冒険者がダンジョン探索に出る時、探索目的を設定する。

探索目的はダンジョン内 AI の行動に一貫性を持たせるための Domain/Application 用データである。

探索目的の種類は以下。

- レベル上げ
- 特定アイテム収集
- 特定モンスター討伐
- 特定フロア到達

現時点ではモンスターとフロアの Domain は未定義のため、対象モンスター ID や対象フロア ID は仮の `int` として扱う。

探索目的は、冒険者のレベルと、冒険者ギルドの雑貨店・装備店で交換対象となっているアイテムから決定する。

雑貨店や装備店に交換項目がある場合、その納品対象アイテムが特定アイテム収集の探索目的候補に入る。

例:

- 薬草 10 個を納品する。
- 報酬として 100 ゴールドを得る。
- この交換項目が存在する場合、冒険者の探索目的候補に「薬草 10 個を集める」が入る。

探索目的の抽選対象は以下。

| 条件 | 抽選対象 |
|---|---|
| Lv10 未満、交換項目なし | レベル上げ |
| Lv10 未満、交換項目あり | レベル上げ、特定アイテム収集 |
| Lv10 以上、交換項目なし | レベル上げ、特定フロア到達 |
| Lv10 以上、交換項目あり | レベル上げ、特定アイテム収集、特定フロア到達 |

特定モンスター討伐は、モンスターや討伐依頼の Domain が追加された後に抽選対象へ入れる。

### 交換項目

雑貨店・装備店は、冒険者向けの交換項目を持てる。

交換項目は、冒険者が納品するアイテムと、冒険者が受け取る報酬アイテムで表現する。

交換は `ExchangeTransaction` を使って記録する。

## 推奨フォルダ構成

```text
Domain/
├── Character/
│   ├── Character
│   ├── CharacterStats
│   ├── ScoutCost
│   ├── AdventurerLifecycleState
│   └── CharacterRole
├── Guild/
│   ├── AdventurerGuild
│   └── GuildStaffAssignment
├── Facility/
│   ├── Facility
│   ├── FacilityType
│   ├── FacilityUsageRequest
│   ├── FacilityUsageType
│   └── InnReservation
├── Item/
│   ├── ItemDefinition
│   ├── ItemCategory
│   ├── EquipmentSpec
│   ├── EquipmentSlot
│   ├── Inventory
│   ├── ItemStack
│   └── SpecialItemIds
├── Commerce/
│   ├── ExchangeTransaction
│   ├── ExchangeOffer
│   └── PricePolicy
├── Dungeon/
│   ├── DungeonExplorationGoal
│   └── DungeonExplorationGoalType
├── Common/
│   └── DomainMath
└── EntityIdentity/
    ├── EntityIdentity
    ├── EntityIdentityRegistry
    └── EntityKind

Application/
└── UseCase/
    ├── RecruitStaffUseCase
    ├── AssignStaffUseCase
    ├── ProcessFacilityUsageUseCase
    ├── ProcessAdventurerSaleUseCase
    ├── ProcessExchangeOfferUseCase
    ├── PayStaffSalaryUseCase
    ├── SpawnAdventurerUseCase
    ├── ReserveInnUseCase
    ├── ReleaseInnReservationUseCase
    ├── SelectDungeonExplorationGoalUseCase
    └── AdvanceAdventurerLifecycleUseCase
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
