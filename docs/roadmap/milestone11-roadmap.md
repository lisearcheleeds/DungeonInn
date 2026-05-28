# Milestone 11 Roadmap — コンテンツ充実

## ゴール

プレイヤーが長く遊べるように、ダンジョン、Actor、装備、アイテム、ドロップ、回復行動の幅を広げる。
Milestone 11 は「新しいシステム概念を増やす」ことではなく、既存の `Master/`、Dungeon、Spawn、AI、ActorEffect の契約を使ってゲーム内コンテンツ量と運用パターンを増やす milestone とする。

画像アセットはまだ追加しない。
新しいモンスター、冒険者、装備、アイテムは、既存の visual id / placeholder / 同一アセット参照で表示する。

## 対象範囲

- ダンジョン深度帯とフロア内容の拡充
- モンスター種族、モンスター archetype、エリート、ボスの追加
- 冒険者 archetype と来訪 Spawn パターンの追加
- 武器、防具、アクセサリー、消耗品、素材アイテムの追加
- Species drop table の整備
- 体力回復ポーションの使用と AI 判断への組み込み

## 対象外

- Faction と勢力関係
- Pet システム
- 新規画像アセット、専用アニメーション、オーディオ、パーティクル
- 勝利条件、敗北条件、エンディング
- 座標、モンスター出現状態、ダンジョン内個体状態の Save 対応

## Phase 一覧

### Phase 1: 既存 Master / Domain 契約の棚卸し

M11 で増やすコンテンツが、既存の `Master/` と Domain 契約で表現できるか確認する。
不足がある場合は、互換維持用の旧 API を残さず、最適な契約へ破壊的に直す。

確認対象:

- `SpeciesMaster`
- `ActorArchetypeMaster`
- `AdventurerSpawnMaster`
- `SpawnTableMaster`
- `DungeonDepthBandConfig`
- `ItemMaster`
- `EquipmentMaster`
- `WeaponMaster`
- `ActorEffectMaster`
- `StatusEffectSpec`
- `DungeonFloorExplorationMaster`

完了条件:

- [ ] M11 で追加する item / species / archetype / spawn table / actor effect / dungeon band の一覧が roadmap または task log に記録されている
- [ ] 各追加要素の所属 master / config と参照先 ID が roadmap または task log で確認できる
- [ ] `SpeciesMaster` と `ActorArchetypeMaster` の責務が混ざっていない
- [ ] Runtime Instance に master 由来の不変値を複製保持しない方針が維持されている
- [ ] 旧 master / 旧 enum / 旧 API を互換目的だけで残していない

### Phase 2: ダンジョン深度帯と特殊部屋

`DungeonDepthBandConfig` を拡充し、地下階層ごとに生成設定、出現テーブル、報酬傾向が変わるようにする。
地下は無限に続く前提なので、固定階層だけでなく深度帯の繰り返し・スケールにも対応する。

初期深度帯:

| Band | Floor | 目的 |
|---|---:|---|
| Shallow | 1-3 | 序盤。弱いモンスターと低価値ドロップを中心にする |
| Middle | 4-7 | 中盤。複数武器種と素材ドロップを混ぜる |
| Deep | 8-11 | 高難度。エリート出現率と報酬を上げる |
| Boss | 5 の倍数 | ボスまたは準ボス部屋を配置する |
| Endless | 12 以降 | Deep を基準に、階層に応じてレベルと報酬を緩やかに上げる |

特殊部屋:

- BossRoom
  - ボスまたは準ボスのスポーン候補になる部屋。
  - 原則として主経路の出口側、または `RouteDepth` が高い部屋から選ぶ。
- TreasureRoom
  - 素材、消耗品、換金アイテムのドロップ候補を増やす部屋。
  - M11 では宝箱 object を必須にせず、部屋の報酬補正として扱ってよい。
- RestRoom
  - 敵出現率を下げる部屋。
  - 回復施設は置かない。施設システムとは混ぜない。

責務境界:

- Domain はフロア、部屋、深度帯、生成設定を持つ。
- Application は深度帯からスポーンテーブル、報酬補正、特殊部屋候補を選ぶ。
- View は同一アセットまたは placeholder を使う。M11 では特殊部屋の専用見た目は必須にせず、内部効果として扱う。
- Dungeon 生成は UnityEngine / Addressables に依存しない。

禁止:

- `floorIndex` の if 文を各 UseCase に散らすこと。
- 特殊部屋を View 専用状態として持ち、Application から参照できない状態にすること。
- BossRoom 用の表示 asset がないことを理由に、Domain / Application の特殊部屋概念を View に寄せること。

完了条件:

- [ ] 深度帯ごとに `DungeonDepthBandConfig` が定義されている
- [ ] フロア生成またはフロア選択時に深度帯が参照される
- [ ] BossRoom / TreasureRoom / RestRoom のいずれかが Application から判定できる
- [ ] 地下 12 階以降でも生成設定が破綻しない

### Phase 3: モンスター種族・エリート・ボス

モンスターの追加は `SpeciesMaster` と `ActorArchetypeMaster` を分けて行う。
種族固有ドロップは `SpeciesMaster.SpeciesDrops`、生成時の能力・Behavior・初期装備・自然武器は `ActorArchetypeMaster` が持つ。

追加する種族:

| Species | 役割 | 主なドロップ |
|---|---|---|
| Slime | 序盤の低脅威 | 粘液素材、低価値素材 |
| Goblin | 序盤から中盤の標準敵 | 小銭、粗悪な武器素材 |
| Wolf | 高速近接敵 | 牙、毛皮 |
| Skeleton | 中盤の耐久寄り敵 | 骨素材、錆びた装備 |
| Bat | 低耐久・高頻度敵 | 翼膜素材 |
| Orc | 中盤以降の高火力敵 | 鉄素材、武器素材 |
| Golem | 深層の高耐久敵 | 鉱石、希少素材 |
| Dragonkin | 深層・ボス候補 | 鱗、希少素材 |

ActorArchetype:

- 通常個体
  - 深度帯に応じた標準パラメータを持つ。
- Elite 個体
  - 通常個体より HP / Attack / Reward を高くする。
  - Runtime の `IsElite` bool ではなく、別 `ActorArchetypeMaster` として定義する。
- Boss 個体
  - BossRoom または Boss floor 用の `ActorArchetypeMaster` として定義する。
  - Boss はゲーム終了条件ではない。倒してもゲームは継続する。

責務境界:

- 種族名、種族ドロップは `SpeciesMaster`。
- 能力値、初期レベル、Behavior、自然武器、初期装備、visual id は `ActorArchetypeMaster`。
- 出現階層と出現重みは `SpawnTableMaster`。
- 報酬量の最終調整は drop / reward 用の Application policy が担う。

禁止:

- `Actor` に `IsBoss` / `IsElite` のような分類 bool を追加すること。
- Species に能力値や Behavior を持たせること。
- ActorArchetype に種族固有ドロップを持たせること。
- Monster 専用の旧 master を復活させること。

完了条件:

- [ ] 追加モンスターが `SpeciesMaster` と `ActorArchetypeMaster` に分離されている
- [ ] Elite / Boss が別 `ActorArchetypeMaster` として定義されている
- [ ] 深度帯ごとの `SpawnTableMaster` から出現候補が解決できる
- [ ] Boss を倒してもゲームが継続する

### Phase 4: 冒険者職業と Spawn パターン

冒険者の追加は、職業を固定クラス化せず `ActorArchetypeMaster` と `AdventurerSpawnMaster` で表現する。
同じ archetype を複数の `AdventurerSpawnMaster` から参照できるようにする。

追加する職業 archetype:

| Archetype | 役割 | AI 方針 |
|---|---|---|
| Warrior | 高 HP / 近接 | 近接戦闘を優先する |
| Mage | 低 HP / 高火力 | 危険時は撤退しやすい |
| Archer | 中 HP / 遠距離 | 射程を活かす |
| Healer | 低火力 / 回復支援 | 自身の回復アイテム使用を優先する |
| Scout | 低戦闘力 / 探索寄り | 浅い階層の探索と素材収集を優先する |

Spawn パターン:

- 序盤は Warrior / Scout を中心にする。
- 中盤以降は Mage / Archer / Healer の比率を上げる。
- `SpawnOnce` は固有名付き冒険者だけに使う。
- 汎用冒険者を増やす場合は、同じ archetype を複数 spawn entry から参照する。

責務境界:

- `ActorArchetypeMaster` は能力と初期装備のテンプレート。
- `AdventurerSpawnMaster` は来訪文脈、表示名、SpawnOnce を持つ。
- `SpawnTableMaster` はどの冒険者 spawn 定義をどの重みで選ぶかだけを持つ。
- AI policy は職業名そのものではなく、Actor の能力、装備、Goal / Plan / Action を見て判断する。

禁止:

- `WarriorBehavior` / `MageBehavior` のように職業ごとの Behavior 型を増やすこと。
- `ActorArchetypeMaster.Name` を生成後の個体名として扱うこと。
- SpawnTable に表示名や能力値を直接持たせること。

完了条件:

- [ ] 追加冒険者職業が `ActorArchetypeMaster` で定義されている
- [ ] 固有名は `AdventurerSpawnMaster.DisplayName` にある
- [ ] SpawnTable から複数職業が重み付きで出現する
- [ ] 職業追加により AI / View に職業名分岐が増えていない

### Phase 5: 装備・アイテム・消耗品

武器、防具、アクセサリー、素材、消耗品を追加する。
アイテムの基本情報は `ItemMaster`、装備スロットや装備可能性は `EquipmentMaster`、武器の戦闘性能は `WeaponMaster`、効果発動は `ActorEffectMaster` へ接続する。

追加カテゴリ:

- 武器
  - Sword
  - Axe
  - Bow
  - Staff
  - Dagger
- 防具
  - LightArmor
  - HeavyArmor
  - Robe
- アクセサリー
  - Ring
  - Amulet
- 消耗品
  - Potion
  - HighPotion
- 素材
  - SlimeGel
  - WolfFang
  - BatWing
  - BoneShard
  - IronOre
  - GolemCore
  - DragonScale

責務境界:

- `ItemMaster` は名前、タグ、スタック、価格などの基本情報を持つ。
- `EquipmentMaster` は装備スロットと装備としての分類を持つ。
- `WeaponMaster` は武器種別、射程補正、攻撃間隔補正など戦闘向け情報を持つ。
- `ActorEffectMaster` は消耗品から付与される効果を持つ。
- Actor の Inventory は `ItemStack` を持ち、master 由来の表示名や効果説明を複製しない。

禁止:

- `ItemMaster` に武器固有の戦闘計算を持たせること。
- `WeaponMaster` に表示名や価格など Item 共通情報を重複保持すること。
- 消耗品効果を AI policy や UseCase 内の item id 分岐だけで実装すること。
- 新規装備ごとに View prefab を増やすこと。

完了条件:

- [ ] 追加アイテムが正しい master に分離されている
- [ ] 武器の戦闘性能が `WeaponMaster` / weapon combat calculator から解決される
- [ ] 消耗品が `ActorEffectMaster` に接続されている
- [ ] Inventory / SaveData が master 由来の不変値を複製していない

### Phase 6: Drop Table と報酬の整備

`SpeciesMaster.SpeciesDrops` を本格設定し、モンスター種族ごとに素材、換金品、装備候補を落とすようにする。
深度帯や Elite / Boss による報酬補正は、drop table の選択または Application policy で扱う。

Drop 方針:

- 種族素材は `SpeciesMaster.SpeciesDrops`。
- M11 の装備ドロップは `SpeciesMaster.SpeciesDrops` に ItemId として含める。
- 装備中アイテムを死亡時に落とす仕組みは M11 対象外とする。
- Boss 固有素材は Boss 用 Species または Boss 用 drop table に寄せる。
- Gold は Item として持つ既存方針に従い、特別な通貨フィールドを増やさない。

責務境界:

- Drop 候補の正典は master / config。
- Drop の抽選は Application UseCase / Policy。
- ItemInstance 生成と World 登録は UseCase。
- View は drop 結果を表示するだけ。

禁止:

- Monster defeat 処理に item id の固定配列を直書きすること。
- Drop 抽選を `SpeciesMaster` 自身のメソッドとして実装すること。
- View / Presenter で Drop 内容を決定すること。

完了条件:

- [ ] 追加 Species に drop table が設定されている
- [ ] Drop 抽選が master / config を参照している
- [ ] Boss / Elite の報酬補正方針がコードまたは docs から追える
- [ ] 主要 drop の EditMode test が存在する

### Phase 7: 回復アイテムと AI 組み込み

Actor は Inventory に存在する回復アイテムを使用できる。
回復アイテムは ActorEffect / StatusEffect を通じて時間経過回復を行う。

Potion 仕様:

- Potion は使用時に 1 個消費される。
- Potion は `ActorEffectMaster` を通じて使用者へ ActorEffect を付与する。
- Potion の効果は 10 秒かけて HP を 30 回復する。
- 最大 HP を超えて回復しない。
- 同じ Potion を再使用した場合は `AppendDuration` として扱う。
- HP 回復 StatusEffect の合成は `Sum` とする。

HighPotion 仕様:

- HighPotion は M11 で master 定義、ActorEffectMaster、StatusEffectSpec まで追加する。
- 初期効果は 10 秒かけて HP を 60 回復する。
- AI 使用対象に含める。
- Potion と HighPotion の両方を持つ場合、AI は回復量の無駄が少ない候補を優先する。
- どちらも無駄が同程度なら Potion を優先し、HighPotion の消費を抑える。

AI 使用条件:

- Actor が回復アイテムを持っている。
- Actor の現在 HP が最大 HP の 50% 以下。
- または、Potion の総回復量を無駄なく受けられるだけ HP が減っている。
- 戦闘中は、回復後の推定 HP をもとに戦闘継続 / 撤退判断を行う。
- AI は item id 直指定ではなく、`ItemTag.Recovery` と `ActorEffectMaster` の効果内容から候補を選ぶ。

責務境界:

- Item 使用可能判定は Application UseCase。
- ActorEffect の付与と時間経過処理は ActorEffect / StatusEffect 基盤。
- AI は「どのタイミングで使うか」を決める。
- Domain は ActorEffect の状態を保持するが、AI の cooldown / dirty / 評価時刻は持たない。

禁止:

- Potion 専用の HP 直接加算処理を AI policy に書くこと。
- 回復効果を即時回復として実装し、ActorEffect / StatusEffect 基盤を迂回すること。
- AI が `CurrentScheduleTick` を短期判断間隔として使うこと。
- 回復アイテムの表示名や説明文を ActorEffectInstance / Inventory に複製すること。

完了条件:

- [ ] Potion 使用で Inventory が 1 減る
- [ ] Potion 使用で ActorEffect が付与される
- [ ] 10 秒で HP が 30 回復し、最大 HP を超えない
- [ ] 同一 Potion 再使用が `AppendDuration` として動く
- [ ] AI が HP 条件に応じて Potion 使用を選択できる
- [ ] AI の戦闘継続 / 撤退判断が回復後推定 HP を考慮する

### Phase 8: 統合確認とレビュー

M11 の完了確認は、単に master 行が増えていることではなく、ゲームループ上でコンテンツが実際に利用されていることを確認する。

確認シナリオ:

- Launcher -> Title -> Start からゲームを開始する。
- 複数深度の Dungeon floor を生成する。
- 追加モンスター、Elite、Boss が SpawnTable 経由で出現する。
- 追加冒険者職業が SpawnTable 経由で来訪する。
- 追加装備と素材が Inventory / Drop / Sell の流れに乗る。
- Actor が Potion を使用し、時間経過で回復する。
- AI が Potion を持っている場合に回復後 HP を考慮して行動する。

必須確認:

- [ ] `uloop.cmd compile --project-path Client` が成功している
- [ ] `uloop.cmd run-tests --project-path Client --test-mode EditMode` が pass している
- [ ] Launcher -> Title -> Start 経由の PlayMode で `[World] GameWorldState initialized` が確認できる
- [ ] PlayMode ログに error が存在しない
- [ ] M11 完了セルフレビューを `docs/self-review/` に保存している

## 配置方針

Runtime:

- Domain
  - Dungeon の深度帯、部屋分類、ActorEffect 実行状態など、ゲーム上の状態と不変条件。
- Application
  - Spawn、Drop、Item 使用、ActorEffect 進行、AI 判断、Dungeon floor 選択。
- Master
  - `SpeciesMaster`、`ActorArchetypeMaster`、`SpawnTableMaster`、`ItemMaster`、`EquipmentMaster`、`WeaponMaster`、`ActorEffectMaster`。
- View
  - 既存 visual id / placeholder / 同一アセットの表示適用。

Editor:

- master / placeholder / Addressable の生成補助が必要な場合のみ `Editor/OneShot/` に置く。
- OneShot は自動実行しない。

Prefab / Addressable:

- 新規画像アセットは追加しない。
- 既存 Actor visual / Item icon / placeholder を参照する。
- Prefab address は発生元 Master / Visual Master / Definition から解決する。
- LifetimeScope に新規コンテンツ Prefab を直接 `SerializedField` しない。

LifetimeScope:

- M11 の追加 UseCase / Service / Repository は、ゲームセッション中に必要なものは GameSession scope に登録する。
- View / Presenter / Pool / Factory は所有する MainScene / ModuleScene scope に置く。
- Product scope にゲームセッション固有の content state を登録しない。

## 既存実装に合わせる点

- Clean Architecture の Domain / Application / Infrastructure / View 分離を維持する。
- Master は `DungeonInn.Master` 名前空間を使う。
- Actor 生成単位は `ActorArchetypeMaster` とする。
- 種族固有情報は `SpeciesMaster` に置く。
- Spawn の抽選入口は `SpawnTableMaster` とする。
- 回復効果は ActorEffect / StatusEffect 基盤を使う。
- AI は `ActorDecisionScheduler`、`ActorAiContext`、`IActorAiPolicy`、`AdvanceActorAiOrchestrator` の流れに乗せる。
- Screen / HUD への表示追加が必要な場合は ScreenStack / GameHUD ModuleScene の所有境界を守る。

## 破壊的に直す点

- コンテンツ追加のために旧 master、旧 enum、旧互換 API を残さない。
- `IsBoss`、`IsElite`、`IsRecoveryItem` のような Runtime 分類 bool を安易に追加しない。
- item id / floor index / actor name の直書き分岐を見つけた場合は、master / config / policy へ移す。
- 最小差分を理由に、不適切な責務名、旧 API、旧 Scene / Prefab 要素を残さない。
- View 表示都合で Domain / Application のデータ構造を歪めない。

## テストと PlayMode で確認すること

EditMode:

- `DungeonDepthBandConfig` が floor index から期待どおり選択される。
- SpawnTable が深度帯ごとに期待する候補を返す。
- 追加 `ActorArchetypeMaster` の必須フィールドが設定されている。
- Species drop が抽選され、ItemStack / ItemInstance として生成される。
- Potion 使用で Inventory 減少、ActorEffect 付与、時間経過 HP 回復が発生する。
- AI が Potion 使用条件を満たす Actor に回復行動を選択する。

PlayMode:

- Launcher シーンから起動し、Title の Start ボタンを押して開始する。
- 地下 1 階、5 階、12 階以降の生成とスポーンを確認する。
- Boss floor で Boss archetype が出現候補に入ることを確認する。
- 追加冒険者職業が地上に来訪することを確認する。
- Potion を持つ Actor が HP 低下時に回復行動を取ることを確認する。
- エラーログがないことを確認する。

## 実装前の理想設計方針

### レビュー判断

Milestone 11 は、コンテンツ量を増やす milestone である一方、実装上は `Master`、Dungeon、Spawn、Drop、ActorEffect、AI の接続点を広げる作業になる。
レビューでは「master 行が増えたか」ではなく、「追加したコンテンツが既存の正しい入口から生成・使用・表示・検証されているか」を判定する。

実装前レビューの判断:

- M11 は新規の大規模システムを増やさず、既存の `Master/` と Application UseCase を正しい責務に拡張する。
- 既存の `HardcodedMasterRepository` は現時点の master 正典として使う。ただし、互換目的の旧 master / 旧 enum / 旧 public API は残さない。
- Actor の分類は `ActorArchetypeMaster`、`SpeciesMaster`、`IActorBehavior`、装備、AI policy の組み合わせで表現する。
- Boss / Elite / 職業 / 回復アイテムを Runtime の bool や View 分岐として追加しない。
- Dungeon の深度、Spawn、Drop、回復行動は Application から参照可能なデータ・UseCase・Policy に置き、View の見た目状態に閉じ込めない。
- 既存コードの現状と合わない箇所は、差分を小さくするために残さず、責務・依存方向・寿命が正しい形へ破壊的に置き換える。

### 全体原則

- コンテンツ正典は `DungeonInn.Master` 配下の master とし、Runtime Instance は master 由来の不変値をコピーしない。
- Dungeon / Actor / Item / Effect の Domain は UnityEngine、Addressables、Scene、View、LifetimeScope に依存しない。
- Application は master を読み、生成・抽選・状態変更・イベント発行を行う。View は表示専用 DTO / ViewData だけを受け取る。
- `GameSessionLifetimeScope` は M11 の Application service / UseCase / session state を所有する。
- `WorldLifetimeScope` は World MainScene が所有する 3D 表示、Actor view、Projectile / AreaEffect view、map view を所有する。
- `ScreenStackLifetimeScope` と `GameHUD` ModuleScene は UI window / HUD を所有する。M11 で新規確認 UI が必要になった場合も、World MainScene に Canvas を置かない。
- 新規画像アセットは追加しない。既存 `ActorVisualMaster`、`ActorVisualDefinitionSO`、placeholder、既存 Item 表示を使う。
- Spawn / Drop / Potion / AI の判断を item id、actor name、floor index の散在 if 文で実装しない。
- `IsBoss`、`IsElite`、`IsRecoveryItem`、`ClassType` のような Runtime 分類フィールドを追加しない。
- UseCase はステートレスにし、長期状態は `ActorDecisionScheduler`、`ActorNavigationService`、`ActorSpatialIndexService` などの Service が所有する。
- Event 購読コールバックで Domain state を変更しない。AI dirty、履歴記録、表示ログ更新に限定する。
- M10 方針どおり、座標、モンスター出現状態、ダンジョン内個体状態は Save 対象にしない。

### 望ましい大枠

```text
Domain
  Actor
    Actor
    IActorBehavior
    AdventurerBehavior
    MonsterBehavior
    ActorStats
    ActorEffectInstance
    ActiveStatusEffect
  Dungeon
    Dungeon
    DungeonFloor
    DungeonRoom
    DungeonDepthBandConfig
    DungeonSpecialRoomKind          // 新規候補。部屋分類が既存型で表現できない場合のみ追加
    DungeonRoomContentMarker        // 新規候補。特殊部屋を Application から参照する必要がある場合のみ追加
  Item
    ItemStack
    ActorDropEntry
    ItemTag

Master
  HardcodedMasterRepository
  IMasterRepository
  SpeciesMaster
  ActorArchetypeMaster
  AdventurerSpawnMaster
  SpawnTableMaster
  SpawnTableEntryMaster
  DungeonFloorExplorationMaster
  ItemMaster
  EquipmentMaster
  WeaponMaster
  WeaponTypeCombatMaster
  ActorEffectMaster
  StatusEffectSpec
  ActorVisualMaster
  EnvironmentPropVisualMaster

Application
  Dungeons
    GenerateDungeonFloorUseCase
    EnsureDungeonFloorGeneratedOrchestrator
    InitializeDungeonOrchestrator
    SelectDungeonDepthBandUseCase       // 新規候補
    BuildDungeonRoomContentPlanUseCase  // 新規候補
  Actors/Spawn
    SpawnAdventurerUseCase
    SpawnMonsterUseCase
    SpawnScheduledAdventurerOrchestrator
    SpawnScheduledMonsterOrchestrator
    CompleteActorSpawnUseCase
  Actors/Ai
    ActorDecisionScheduler
    ActorAiContext
    ActorAiRuntimeState
    IActorAiPolicy
    AdventurerAiPolicy
    MonsterAiPolicy
    ApplyActorAiDecisionUseCase
    AdvanceActorAiOrchestrator
    RecoveryItemSelectionPolicy         // 新規候補
  Items
    DropItemUseCase
    PickUpItemUseCase
    UseConsumableItemUseCase
    UseRecoveryItemOrchestrator
    RecoveryItemCandidateQuery          // 新規候補
  Combat
    AdvanceCombatUseCase
    CombatDefeatResolver
    ActorDefeatOrchestrator
    GrantExperienceUseCase
  World
    GameWorldState
    ActorViewDataStore
    GetActorStatusSummaryQuery
    GetActorDetailQuery

GameSession
  GameSessionLifetimeScope
  GameSessionAssetScopeHolder
  WorldGameSettingsRepository

View
  Scene/MainScene/World
    WorldLifetimeScope
    WorldGameLoopEntryPoint
    WorldPresenter
    WorldMapView
    WorldActorPresenter
    ActorVisualDefinitionLoader
    WorldAddressableViewFactory
  Scene/ModuleScene/GameHUD
    existing HUD / ScreenStack windows only if display is needed
  Scene/ModuleScene/ScreenStack
    ScreenStackLifetimeScope
    ScreenStackInstanceFactory

Editor
  OneShot
    M11 master validation / placeholder wiring one-shot only if needed

Prefab / Addressable
  Runtime/Prefab/World existing actor/projectile/area effect prefabs
  Existing ActorVisualDefinitionSO and ActorVisualMaster addresses
  No new image assets required for M11
```

新規概念追加ゲート:

- `DungeonSpecialRoomKind`、`DungeonRoomContentMarker`、`SelectDungeonDepthBandUseCase`、`BuildDungeonRoomContentPlanUseCase`、`RecoveryItemSelectionPolicy`、`RecoveryItemCandidateQuery` を追加する場合、実装時ログに以下を記録する。
- 既存類似概念: `DungeonRoom.RouteDepth`、`DungeonDepthBandConfig`、`DungeonFloorExplorationMaster`、`UseConsumableItemUseCase`、`UseRecoveryItemOrchestrator`、`AdventurerAiPolicy`。
- 意味差分: 新規型が「表示用の名前違い」ではなく、所有者、寿命、更新契機、入力データ、利用者のいずれかで既存概念と異なること。
- 代替不可理由: 既存概念で代替すると、floor index 分岐、item id 分岐、View 状態依存、または consume 競合が発生すること。
- 統合・削除条件: Dungeon content planning が master 化された場合、Recovery item 選択が汎用 Consumable policy に統合された場合、または GameSession 設定 repository へ移管された場合。
- 新規候補を追加しなかった場合も、実装ログに「新規概念追加なし」と記録する。

### Task 1: 既存 Master / Domain 契約の棚卸し の設計

目的:

M11 の追加コンテンツが `HardcodedMasterRepository` と既存 master 契約で表現できるか確認し、足りない契約だけを責務に沿って追加する。

理想構成:

- `HardcodedMasterRepository`
  - `CreateItemMasters`
  - `CreateEquipmentMasters`
  - `CreateWeaponMasters`
  - `CreateActorEffectMasters`
  - `CreateSpeciesMasters`
  - `CreateActorArchetypeMasters`
  - `CreateAdventurerSpawnMasters`
  - `CreateSpawnTableMasters`
  - `CreateDungeonFloorExplorationMasters`
  - `ValidateReferences`
- `IMasterRepository`
  - 既存 getter と dictionary access を維持する。
  - 新規 master が必要な場合だけ追加する。
- `DungeonFloorExplorationMaster`
  - 現在は floor index 固定。
  - 深度帯対応で固定 floor index だけでは不十分な場合、`DungeonDepthBandConfig` から解決する Application 経路を追加する。

所有者:

- Runtime master 正典: `Master/HardcodedMasterRepository`
- DI 登録: Product または GameSession の既存登録に従う。M11 用 session state は `GameSessionLifetimeScope`。
- Scene 所有なし。

依存方向:

- Application -> `IMasterRepository`
- Master -> Domain value object
- Domain -> Master なし
- View -> Master 直接参照なし

配置:

- Runtime: `Client/Assets/DungeonInn/Runtime/Scripts/Master/`
- Tests: `Client/Assets/DungeonInn/Tests/EditMode/`
- Editor: master 生成補助が必要な場合のみ `Client/Assets/DungeonInn/Editor/OneShot/`
- Prefab / Addressable: 追加なし

既存実装に合わせる点:

- `DungeonInn.Master` namespace を使う。
- `ValidateReferences()` で item、equipment、weapon、actor archetype、species、spawn table、actor effect の参照整合を fail fast する。
- `ActorArchetypeMaster` は `SpeciesId`、`VisualId`、`DefaultWeaponType`、`BehaviorType`、`LevelTableId` を持つ既存設計に合わせる。

破壊的に直す点:

- M11 で必要になった master 契約を item id / floor index の ad hoc 分岐で代替しない。
- 旧 `MonsterSpeciesMaster` 相当の互換層を復活させない。
- `HardcodedMasterRepository` の中で参照不整合を許容する fallback を追加しない。

禁止:

- Runtime constructor にテスト用 optional parameter を追加すること。
- Master 参照値を `Actor`、`ItemStack`、`ActorEffectInstance` にコピーすること。
- `Resources.Load` / `Addressables.LoadAssetAsync` を master から直接呼ぶこと。

完了条件:

- [ ] M11 追加 master の参照整合が `ValidateReferences()` で検出される
- [ ] Domain から `IMasterRepository` を参照していない
- [ ] 旧 master / 旧 enum / 互換 API が追加されていない
- [ ] 追加 master の constructor 代入漏れを EditMode test で検出できる
- [ ] 新規 DTO / Request / Store / Service / Calculator / Factory / Event / State / UseCase / Policy を追加した場合、新規概念追加ゲートの記録が実装ログにある
- [ ] 新規概念を追加していない場合、実装ログに「新規概念追加なし」と記録されている

### Task 2: ダンジョン深度帯と特殊部屋 の設計

目的:

地下階層ごとの生成設定、SpawnTable、報酬傾向、特殊部屋候補を Application から一貫して取得できるようにする。

理想構成:

- `DungeonDepthBandConfig`
  - 深度帯の値型または設定。
  - floor index -> band -> generation / spawn / reward への入口。
- `DungeonFloorExplorationMaster`
  - 既存の floor exploration 設定。
  - 固定 floor 用として残す場合も、深度帯解決と責務を混ぜない。
- `SelectDungeonDepthBandUseCase` または同等の Application service
  - floor index から深度帯を選ぶ。
- `BuildDungeonRoomContentPlanUseCase` または同等の Application service
  - `DungeonFloor.Rooms` と `RouteDepth` を読み、BossRoom / TreasureRoom / RestRoom 候補を決める。
- `GenerateDungeonFloorUseCase`
  - Dungeon floor 生成に集中する。
  - 特殊部屋表示や spawn 決定まで抱えない。
- `EnsureDungeonFloorGeneratedOrchestrator`
  - 生成済み確認と生成順序を所有する。

所有者:

- Domain: `Domain/Dungeon`
- Application: `Application/Dungeons`
- DI 登録: `GameSessionLifetimeScope`
- View 表示: `WorldLifetimeScope` 配下の `WorldMapView` / `EnvironmentObjectPlacer`

依存方向:

- Application/Dungeons -> Domain/Dungeon, Master
- View/World -> Application/World view data
- Domain/Dungeon -> View / Master / DI なし

配置:

- Runtime Domain: `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Dungeon/`
- Runtime Application: `Client/Assets/DungeonInn/Runtime/Scripts/Application/Dungeons/`
- Runtime Master: `Client/Assets/DungeonInn/Runtime/Scripts/Master/`
- View: 既存 `View/Scene/MainScene/World/Map/`
- Prefab / Addressable: 新規なし。既存 `EnvironmentPropVisualMaster` と placeholder を利用する。

既存実装に合わせる点:

- `DungeonRoom.RouteDepth` を特殊部屋選定に使う。
- `GenerateDungeonFloorUseCase`、`EnsureDungeonFloorGeneratedOrchestrator`、`InitializeDungeonOrchestrator` の入口は維持する。
- `WorldMapView` は Domain を直接組み立てず、Application が用意した表示データを使う。

破壊的に直す点:

- floor index 固定の `DungeonFloorExplorationMaster` だけで無限階層を表現できない場合、深度帯解決を追加して既存の固定前提を置き換える。
- floor index の `if (floor == 5)` 分岐が複数箇所に散る場合、深度帯 / boss floor policy に集約する。

禁止:

- `WorldMapView` や `EnvironmentObjectPlacer` が BossRoom 判定を所有すること。
- Dungeon Domain に `GameObject`、`Material`、`Addressable address` を持たせること。
- BossRoom / TreasureRoom を表示だけのタグにすること。

完了条件:

- [ ] floor index から深度帯を決定する単一入口がある
- [ ] Boss floor / Endless floor の判定が散在していない
- [ ] 特殊部屋候補が Application から参照できる
- [ ] `GenerateDungeonFloorUseCase` が View / Addressable に依存していない

### Task 3: モンスター種族・エリート・ボス の設計

目的:

通常、Elite、Boss のモンスターを、Runtime 分類 bool ではなく master の組み合わせで表現する。

理想構成:

- `SpeciesMaster`
  - 種族名と `SpeciesDrops` を持つ。
  - Slime / Goblin / Wolf / Skeleton / Bat / Orc / Golem / Dragonkin などを定義する。
- `ActorArchetypeMaster`
  - 通常 / Elite / Boss を別 archetype として定義する。
  - `BehaviorType.Monster`、`SpeciesId`、`DefaultWeaponType`、`BaseStats`、`InitialLevel`、`LevelTableId`、`VisualId` を持つ。
- `SpawnTableMaster`
  - 深度帯別の Monster spawn table を持つ。
  - `SpawnTableTargetType.ActorArchetype` を使う。
- `SpawnMonsterUseCase`
  - archetype から Actor を生成する。
- `CompleteActorSpawnUseCase`
  - profile 登録、world 登録、view data 登録など生成完了処理を担う。
- `ActorProfileRegistry`
  - 削除済み Actor の種族 / archetype 参照を保持する。

所有者:

- Master: `HardcodedMasterRepository`
- Application生成: `GameSessionLifetimeScope`
- World表示: `WorldLifetimeScope` の `WorldActorPresenter`

依存方向:

- `SpawnMonsterUseCase` -> `IMasterRepository`, `IGameWorldStateWriter`, `ActorProfileRegistry`
- `WorldActorPresenter` -> `ActorViewDataStore` / visual loader
- Domain Actor -> Master repository なし

配置:

- Master: `Runtime/Scripts/Master/SpeciesMaster.cs`, `ActorArchetypeMaster.cs`, `SpawnTableMaster.cs`
- Application: `Runtime/Scripts/Application/Actors/Spawn/`
- View: `Runtime/Scripts/View/Scene/MainScene/World/ActorVisual/`
- Addressable: 既存 `ActorVisualMaster` の visual id を参照。新規画像なし。

既存実装に合わせる点:

- `ActorArchetypeMaster.VisualId` から `ActorVisualMaster` を解決する。
- `ActorBehaviorType.Monster` を生成時識別として使う。
- `WeaponType.Claws` / `Fangs` / `Bow` など既存 `WeaponTypeCombatMaster` と `WeaponMaster` の流れを使う。

破壊的に直す点:

- 現在の `SpeciesMaster` や `ActorArchetypeMaster` の責務を越えるフィールドを見つけたら移す。
- Boss / Elite を `Actor` の状態フラグで表現しようとしている箇所があれば、別 archetype へ置き換える。

禁止:

- `Actor.IsBoss`、`Actor.IsElite` を追加すること。
- `SpeciesMaster` に能力値、Behavior、visual id を持たせること。
- `ActorArchetypeMaster` に `SpeciesDrops` を持たせること。
- Boss 討伐を勝利条件に接続すること。

完了条件:

- [ ] 通常 / Elite / Boss が別 `ActorArchetypeMaster` で定義されている
- [ ] 種族ドロップは `SpeciesMaster.SpeciesDrops` にある
- [ ] SpawnTable は `ActorArchetypeMaster.Id` を参照している
- [ ] Boss を倒してもゲームセッションが継続する

### Task 4: 冒険者職業と Spawn パターン の設計

目的:

冒険者の職業差を Behavior 型の増殖ではなく、archetype、装備、能力、AI 判断で表現する。

理想構成:

- `ActorArchetypeMaster`
  - Warrior / Mage / Archer / Healer / Scout 相当の能力と初期装備を定義する。
  - すべて `ActorBehaviorType.Adventurer` を使う。
- `AdventurerSpawnMaster`
  - 固有表示名、参照 archetype、`SpawnOnce` を持つ。
- `SpawnTableMaster`
  - `SpawnTableTargetType.AdventurerSpawn` を使う。
  - 序盤 / 中盤以降の重み差を table で表現する。
- `SpawnScheduledAdventurerOrchestrator`
  - スケジュール上の来訪処理を所有する。
- `AdventurerAiPolicy`
  - 職業名ではなく、能力、装備、現在 HP、目標、Inventory を読んで判断する。

所有者:

- Master: `HardcodedMasterRepository`
- Application: `GameSessionLifetimeScope`
- View表示: `WorldLifetimeScope` と GameHUD 既存 actor detail

依存方向:

- Spawn orchestrator -> Spawn usecase -> Master / WorldState
- AI policy -> Actor / Context / readable world data
- View -> Query / ViewData

配置:

- Master: `Runtime/Scripts/Master/ActorArchetypeMaster.cs`, `AdventurerSpawnMaster.cs`, `SpawnTableMaster.cs`
- Application: `Runtime/Scripts/Application/Actors/Spawn/`, `Application/Actors/Ai/`
- ViewData: 既存 `ActorViewData`, `ActorStatusViewData`, `ActorDetailViewData`
- Prefab / Addressable: 既存 adventurer visual を共有する。

既存実装に合わせる点:

- `AdventurerSpawnMaster.DisplayName` を個体名の初期値として使う。
- `ActorArchetypeMaster.Name` はテンプレート名として扱う。
- `SpawnOnce` は固有名付き定義だけに使う。

破壊的に直す点:

- 職業別 Behavior 型を作るより、既存 `AdventurerBehavior` と master / AI / equipment に寄せる。
- 職業名分岐が View / AI に必要になった場合は、能力・装備・目標で表現できないかを先に見直す。

禁止:

- `WarriorBehavior`、`MageBehavior`、`ArcherBehavior` を追加すること。
- SpawnTable に display name、stats、equipment を直接持たせること。
- `ActorArchetypeMaster.Name` を生成後の現在表示名として使うこと。

完了条件:

- [ ] 複数 adventurer archetype が `ActorBehaviorType.Adventurer` で定義されている
- [ ] `AdventurerSpawnMaster` が archetype と display name を分離している
- [ ] `SpawnScheduledAdventurerOrchestrator` から追加 archetype が出現する
- [ ] AI / View に職業名 switch が追加されていない

### Task 5: 装備・アイテム・消耗品 の設計

目的:

武器、防具、アクセサリー、素材、回復アイテムを、Item / Equipment / Weapon / ActorEffect の責務に分けて追加する。

理想構成:

- `ItemMaster`
  - Id、表示名、`ItemTag`、価格、重さ、売買可否、最大スタック数、`ActorEffectMasterId`。
- `EquipmentMaster`
  - `ItemId`、`EquipmentSlot`、防御値、`StatBonus`。
- `WeaponMaster`
  - `ItemId`、`WeaponType`、`WeaponTypeCombatMaster`、攻撃値、射程補正、攻撃間隔補正。
- `WeaponTypeCombatMasterCatalog`
  - 武器種別ごとの基本戦闘定義。
- `ActorEffectMaster`
  - Potion / HighPotion の外側の効果定義。
- `StatusEffectSpec`
  - HP 回復量、Duration、TickInterval、AggregationPolicy。
- `UseConsumableItemUseCase`
  - 消耗品使用の Application 入口。
- `UseRecoveryItemOrchestrator`
  - 回復アイテム使用の順序制御。

所有者:

- Master: `HardcodedMasterRepository`
- Application UseCase: `GameSessionLifetimeScope`
- View: 既存 inventory / detail 表示が必要な範囲。新規 Window は必須にしない。

依存方向:

- UseCase -> `IMasterRepository`, Domain Actor / Inventory
- Combat calculator -> Weapon master / type combat master
- View -> Application Query / ViewData

配置:

- Master: `Runtime/Scripts/Master/`
- Domain Item: `Runtime/Scripts/Domain/Item/`
- Application Items: `Runtime/Scripts/Application/Items/`
- Tests: `Tests/EditMode/`
- Prefab / Addressable: 新規 icon / prefab なし

既存実装に合わせる点:

- `ItemMaster.ActorEffectMasterId` で回復効果へ接続する。
- `ItemTag.Recovery` を回復候補判定の入口にする。
- 装備 item は `ItemMaster` と `EquipmentMaster`、武器なら `WeaponMaster` を同じ `ItemId` で揃える。

破壊的に直す点:

- `ItemMaster` に武器攻撃式や防御式が入っている場合は `WeaponMaster` / `EquipmentMaster` に移す。
- 消耗品効果が item id switch で書かれている場合は `ActorEffectMaster` 接続へ置き換える。

禁止:

- `WeaponMaster` に表示名、価格、stack 情報を重複保持すること。
- `EquipmentMaster` に武器射程や攻撃間隔を持たせること。
- `UseConsumableItemUseCase` 内に Potion / HighPotion 固定効果を直書きすること。

完了条件:

- [ ] 追加装備 item が `ItemMaster` / `EquipmentMaster` / `WeaponMaster` に正しく分離されている
- [ ] Potion / HighPotion が `ActorEffectMaster` に接続されている
- [ ] `ValidateReferences()` が item / equipment / weapon / effect の整合を検証する
- [ ] `UseConsumableItemUseCase` が item id 固定効果を持っていない

### Task 6: Drop Table と報酬 の設計

目的:

追加 Species と深度帯に応じた素材・装備・Gold drop を、master 正典から抽選できるようにする。

理想構成:

- `SpeciesMaster.SpeciesDrops`
  - 種族由来の drop 候補。
  - `ActorDropEntry` の item id、確率、最小数、最大数。
- `DropItemUseCase`
  - defeated actor の species / archetype 情報を読み、drop を抽選する。
- `ActorDefeatOrchestrator`
  - defeat の順序制御。
  - `CombatDefeatResolver`、`GrantExperienceUseCase`、`DropItemUseCase` などを明示順序で呼ぶ。
- `ItemSpatialIndexService`
  - World 上の item 検索状態を持つ。
- `PickUpItemUseCase`
  - Actor が drop item を拾う。

所有者:

- Drop 抽選: `GameSessionLifetimeScope`
- World item 表示: `WorldLifetimeScope`
- Drop 正典: `SpeciesMaster`

依存方向:

- Drop usecase -> Master / WorldState / Random / EventPublisher
- SpeciesMaster -> ActorDropEntry value
- View -> item view data / world item state

配置:

- Domain: `Runtime/Scripts/Domain/Item/ActorDropEntry.cs`
- Master: `Runtime/Scripts/Master/SpeciesMaster.cs`
- Application: `Runtime/Scripts/Application/Items/DropItemUseCase.cs`
- View: existing World item / map view if present

既存実装に合わせる点:

- Gold は `SpecialItemIds.Money` / Item として扱う。
- Drop item は `ItemStack` / ItemInstance の既存経路に乗せる。
- Random は `IGameRandom` を使い、UnityEngine.Random を使わない。

破壊的に直す点:

- DropItemUseCase が defeated actor の Behavior 型だけで drop を決めている場合は、Species / Profile 参照へ置き換える。
- item id 配列の直書きがある場合は `SpeciesMaster.SpeciesDrops` に移す。

禁止:

- `SpeciesMaster` 自身が乱数抽選すること。
- View / Presenter が drop 内容を決めること。
- 装備中 item を死亡時に落とす仕組みを M11 に混ぜること。

完了条件:

- [ ] 追加 Species の `SpeciesDrops` が設定されている
- [ ] Drop 抽選が `IGameRandom` と master を使っている
- [ ] Drop 結果が ItemStack / world item として生成される
- [ ] 主要 drop の EditMode test がある

### Task 7: 回復アイテムと AI 組み込み の設計

目的:

Potion / HighPotion を ActorEffect / StatusEffect 基盤で使用し、AI が HP 状態と Inventory から回復アイテム使用を選べるようにする。

理想構成:

- `ItemMaster`
  - Potion / HighPotion に `ItemTag.Recovery` と `ActorEffectMasterId` を持たせる。
- `ActorEffectMaster`
  - Potion: 10 秒 HP 30。
  - HighPotion: 10 秒 HP 60。
- `UseConsumableItemUseCase`
  - Inventory から item を消費し、ActorEffect を付与する。
- `UseRecoveryItemOrchestrator`
  - 回復アイテム使用時の順序制御。
- `AdvanceActorEffectsUseCase`
  - ActorEffect / StatusEffect の時間経過を進める。
- `RecoveryItemCandidateQuery` または同等の Application query
  - Actor の Inventory と master から回復候補を列挙する。
- `RecoveryItemSelectionPolicy`
  - 現在 HP、最大 HP、回復量、無駄回復量から Potion / HighPotion を選ぶ。
- `AdventurerAiPolicy` / `MonsterAiPolicy`
  - 回復候補がある場合、回復後推定 HP を行動判断に使う。

所有者:

- 回復効果 state: Domain Actor / ActorEffect
- 回復使用 UseCase / AI policy: `GameSessionLifetimeScope`
- HUD 表示: 既存 `ActorEffectIconViewData`、`ActorStatusViewData`、GameHUD ModuleScene

依存方向:

- AI policy -> `ActorAiContext` / readable master query / Domain Actor
- UseCase -> `IMasterRepository` / Actor inventory / EventPublisher
- Domain ActorEffect -> Master id と runtime state のみ
- View -> ViewData

配置:

- Domain: `Runtime/Scripts/Domain/Actor/` または既存 ActorEffect 配置
- Master: `Runtime/Scripts/Master/ActorEffectMaster.cs`, `StatusEffectSpec.cs`, `ItemMaster.cs`
- Application: `Runtime/Scripts/Application/Items/`, `Application/Actors/Ai/`
- ViewData: `Runtime/Scripts/Application/World/ActorEffectIconViewData.cs`
- Prefab / Addressable: 追加なし

既存実装に合わせる点:

- `ActorEffectReapplyPolicy.AppendDuration` を Potion / HighPotion に使う。
- `StatusEffectAggregationPolicy.Sum` を HP 回復に使う。
- AI dirty / cooldown / evaluated frame id は `ActorAiRuntimeState` 側に置く。

破壊的に直す点:

- Potion が即時回復になっている場合は ActorEffect / StatusEffect 経路に置き換える。
- AI が item id だけで Potion を選ぶ場合は、`ItemTag.Recovery` と `ActorEffectMaster` の効果内容から選ぶ形に置き換える。

禁止:

- `AdventurerAiPolicy` に HP 直接加算を書くこと。
- `Actor` に AI cooldown / dirty / last evaluated time を持たせること。
- `CurrentScheduleTick` を短期 AI cooldown に使うこと。
- ActorEffectInstance に表示名、説明、回復量固定値をコピーすること。

完了条件:

- [ ] Potion / HighPotion が Inventory から消費される
- [ ] ActorEffect が付与され、10 秒で HP が回復する
- [ ] 最大 HP を超えない
- [ ] AI が回復候補を item id 直指定なしで選べる
- [ ] 回復後推定 HP が戦闘継続 / 撤退判断に使われる

### Task 8: 統合確認とレビュー の設計

目的:

M11 の追加コンテンツが Launcher -> Title -> Start の通常起動経路で実際に生成・使用・表示され、guideline に反していないことを確認する。

理想構成:

- `WorldGameLoopEntryPoint`
  - Game loop の入口。詳細順序は `WorldSimulationOrchestrator` / UseCase に委譲する。
- `WorldSimulationOrchestrator`
  - spawn、AI、combat、drop、effect advance、economy などの順序制御。
- `GameLoopUseCase`
  - clock と frame update の入口。
- `GameWorldState`
  - session 中の World state。
- `ActorViewDataStore`
  - View へ渡す Actor 表示差分。
- `PlayerEventLogStore`
  - 表示ログ記録。

所有者:

- GameSession state / Application: `GameSessionLifetimeScope`
- World MainScene View: `WorldLifetimeScope`
- ScreenStack / HUD: `ScreenStackLifetimeScope` / GameHUD ModuleScene
- Product / Launcher: M11 content state を持たない

依存方向:

- `WorldGameLoopEntryPoint` -> `IGameLoopUseCase`
- GameLoop / Orchestrator -> Application UseCase
- View Presenter -> ViewData / Provider
- Product / Launcher -> session 開始だけ

配置:

- Runtime Application: existing Application folders
- Runtime View: existing World / GameHUD folders
- Tests: EditMode tests
- Docs: M11 completion self-review under `docs/self-review/`

既存実装に合わせる点:

- Play 確認は Launcher から起動し、Title Start を押す前提にする。
- `[World] GameWorldState initialized` を統合確認の必須ログにする。
- `uloop.cmd compile`、EditMode test、PlayMode の順で確認する。

破壊的に直す点:

- Play 確認のためだけに Launcher から World へ直遷移する fallback を追加しない。
- Product / bootstrap に NewGame / content start を混ぜない。
- World MainScene に Screen Space Overlay Canvas を直接追加しない。

禁止:

- `WorldGameLoopEntryPoint` に個別 UseCase の詳細順序を増やし続けること。
- PlayMode 確認未実施のまま M11 完了扱いにすること。
- Error log を既知問題として握りつぶすこと。

完了条件:

- [ ] Launcher -> Title -> Start から World が開始する
- [ ] `[World] GameWorldState initialized` が出る
- [ ] 地下 1 階、5 階、12 階以降の generation / spawn が確認できる
- [ ] Potion 使用と AI 回復判断が PlayMode で確認できる
- [ ] error log がない

### 実装開始前ゲート

以下に該当した場合は実装を止め、ユーザー判断待ちまたは設計修正として扱う。

- M11 の対象外である Faction、Pet、Audio、Particle、Visual polish、勝利 / 敗北条件が必要になった。
- `Actor`、`ItemStack`、`ActorEffectInstance`、SaveData に master 由来の不変値を追加したくなった。
- roadmap に記載されていない Runtime public API、DTO、Request、Event、Service、UseCase、Repository を追加・変更する必要がある。
- roadmap に記載済みの新規候補を追加する場合でも、新規概念追加ゲートの記録理由を書けない。
- `IMasterRepository` を変更する必要があり、既存 master で代替できない理由、利用者、DI 登録、テスト範囲を roadmap または task log に記録できない。
- roadmap に記載されていない `GameSessionLifetimeScope`、`WorldLifetimeScope`、`ScreenStackLifetimeScope` の登録変更が必要になった。
- 新しい Scene / ModuleScene / ScreenStack Window / Prefab / Addressable が必要になった。
- `HardcodedMasterRepository` の責務を超えて、外部ファイルロードや Addressable master load が必要になった。
- `DungeonDepthBandConfig` と `DungeonFloorExplorationMaster` のどちらを正典にするか判断が割れた。
- BossRoom / TreasureRoom / RestRoom の Domain 表現が既存 `DungeonRoom` だけで足りない。
- Potion / HighPotion 以外の回復・buff・debuff を M11 内に含めたくなった。
- AI が職業名、item id、actor name、floor index の直書き分岐を必要とする設計になった。
- UI 追加のために World MainScene 側へ Canvas / Popup / Window を直接置く必要が出た。
- テスト都合だけで Runtime constructor、optional parameter、public setter、internal API を追加したくなった。

ユーザー判断待ちとして扱う項目:

- M11 の確定仕様では、Boss floor は 5 階ごととする。変更したくなった場合は実装を止めてユーザー判断待ちに戻す。
- M11 の確定仕様では、特殊部屋の専用見た目は必須にせず内部効果として扱う。専用表示が必要になった場合は実装を止めてユーザー判断待ちに戻す。

### テスト / PlayMode 確認

EditMode test:

- `HardcodedMasterRepository` の `ValidateReferences()` が M11 追加 master の参照不整合を検出する。
- `DungeonDepthBandConfig` または深度帯 resolver が floor 1、5、8、12 以降で期待する band を返す。
- `SpawnTableMaster` が `SpawnTableTargetType.ActorArchetype` / `AdventurerSpawn` の参照先を正しく検証する。
- `ActorArchetypeMaster` の追加 Monster / Adventurer が `SpeciesId`、`VisualId`、`DefaultWeaponType`、`LevelTableId` を持つ。
- `SpeciesMaster.SpeciesDrops` の item id がすべて存在する。
- `DropItemUseCase` が `IGameRandom` と `SpeciesDrops` から ItemStack / ItemInstance を生成する。
- `UseConsumableItemUseCase` または `UseRecoveryItemOrchestrator` が Potion / HighPotion を 1 個消費する。
- `AdvanceActorEffectsUseCase` が 10 秒で HP 30 / 60 を回復し、最大 HP を超えない。
- `ActorEffectReapplyPolicy.AppendDuration` が同一 Potion 再使用で残り時間を加算する。
- `RecoveryItemSelectionPolicy` が Potion / HighPotion の無駄回復量を比較して候補を選ぶ。
- `AdventurerAiPolicy` が HP 条件を満たす Actor に回復行動を選ぶ。

静的検索:

- `rg -n "IsBoss|IsElite|IsRecoveryItem|WarriorBehavior|MageBehavior|ArcherBehavior" Client/Assets/DungeonInn/Runtime/Scripts`
- `rg -n "Resources\\.Load|Addressables\\.LoadAssetAsync|SceneManager\\.LoadScene|UnityEngine\\.Random" Client/Assets/DungeonInn/Runtime/Scripts`
- `rg -n "new .*UseCase|new .*Service|new .*Repository" Client/Assets/DungeonInn/Runtime/Scripts`
- `rg -n "CurrentScheduleTick" Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Ai`
- `rg -n "WorldLifetimeScope|GameSessionLifetimeScope|ScreenStackLifetimeScope" Client/Assets/DungeonInn/Runtime/Scripts`
- `rg -n "Button " Client/Assets/DungeonInn/Runtime/Scripts/View`

uLoop:

- `uloop.cmd compile --project-path Client`
- `uloop.cmd run-tests --project-path Client --test-mode EditMode`
- Launcher シーンから Play を開始する。
- Title の Start ボタンを押して World を開始する。
- 30 秒以上 Play し、`[World] GameWorldState initialized` が出ていることを確認する。
- Dungeon floor 1、5、12 以降の生成・spawn を確認する。
- Actor の HP を減らした状態で Potion / HighPotion 使用と ActorEffect 回復を確認する。
- `uloop.cmd get-logs --project-path Client` で error log が存在しないことを確認する。

## Milestone 12 へ移動する項目

- Faction と勢力関係
- Pet システム（初期実装）
- オーディオ全般
- パーティクルエフェクト
- ビジュアルポリッシュ
