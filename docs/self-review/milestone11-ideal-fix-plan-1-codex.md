# Milestone 11 Ideal Fix Plan 1 — Codex

## 方針

この修正案は、既存のセルフレビューに書いた個別解決案には引っ張られず、M11 の完成形として技術的負債を残さない設計を優先する。

破壊的変更は許容する。
旧 API、互換用 wrapper、移行期間だけの fallback は置かない。

最終状態の判断基準:

- バランス値の正典は Master。
- Runtime Instance は Master 由来の不変値を複製しない。
- Application は Master を参照して判断するが、Domain は Master repository に依存しない。
- Spawn / Drop / Recovery / Dungeon band / Room role は、ID の直書きではなく Master と Policy で解決する。
- `HardcodedMasterRepository` は巨大な content dump ではなく、検証可能な Master catalog の合成 root にする。

## 1. Master 構造を分割し、HardcodedMasterRepository を Facade にする

### 問題

現在の `HardcodedMasterRepository` は、item、equipment、weapon、species、archetype、spawn table、depth band、facility、market などを 1 クラスで生成している。
M11 で content が増えた結果、Master の正典ではあるが、変更単位・レビュー単位・参照検証単位が大きすぎる。

### 理想設計

`HardcodedMasterRepository` は repository facade として残すが、content 定義は catalog 単位に分割する。

配置案:

- `Master/Catalogs/ItemMasterCatalog`
- `Master/Catalogs/EquipmentMasterCatalog`
- `Master/Catalogs/WeaponMasterCatalog`
- `Master/Catalogs/ActorEffectMasterCatalog`
- `Master/Catalogs/SpeciesMasterCatalog`
- `Master/Catalogs/ActorArchetypeMasterCatalog`
- `Master/Catalogs/AdventurerSpawnMasterCatalog`
- `Master/Catalogs/SpawnTableMasterCatalog`
- `Master/Catalogs/DungeonDepthBandMasterCatalog`
- `Master/Validation/MasterReferenceValidator`

`HardcodedMasterRepository` は各 catalog を作成し、`MasterReferenceValidator` に渡して整合性を検証するだけにする。

### 破壊的変更

- `HardcodedMasterRepository.CreateXxxMasters()` 群を削除する。
- 参照検証を repository 本体から `MasterReferenceValidator` へ移す。
- Master ID の一覧性は catalog class と test で担保する。

### 完了条件

- [ ] `HardcodedMasterRepository` が content 行を直接持たない
- [ ] 各 catalog が単一 Master 種別だけを返す
- [ ] `MasterReferenceValidator` が item / equipment / weapon / effect / species / archetype / spawn / dungeon band を横断検証する
- [ ] `MasterRepositoryTests` が catalog 単位の代表検証を持つ

## 2. Drop 正典を Runtime Behavior から Master 解決へ戻す

### 問題

現在の Drop は `DropItemUseCase` が `IActorDropSource.DropTable` を読む。
Monster 生成時に `SpeciesMaster.SpeciesDrops` が `MonsterBehavior` 側へ渡されている場合、Runtime Instance が Master 由来の不変 drop table を複製保持する。

これは M11 の「Runtime Instance に master 由来の不変値を複製保持しない」に反する。

### 理想設計

Drop の正典は常に `SpeciesMaster.SpeciesDrops`。
Runtime の `MonsterBehavior` は `SpeciesId` だけを持つ。
`DropItemUseCase` は `Actor.ArchetypeId` から `ActorArchetypeMaster` を引き、`SpeciesId` から `SpeciesMaster.SpeciesDrops` を解決する。

推奨構成:

- `MonsterBehavior`
  - `SpeciesId` のみ保持
  - `DropTable` は持たない
- `DropItemUseCase`
  - `IMasterRepository` を注入
  - defeated actor の `ArchetypeId` から species drop を解決
- `IActorDropSource`
  - 削除候補
  - Behavior 型に drop 正典を持たせる入口を閉じる

### 破壊的変更

- `IActorDropSource.DropTable` を削除する。
- `MonsterBehavior` constructor から drop table 引数を削除する。
- `ActorFactory` は species drop を Behavior に渡さない。
- Drop 関連 test は `MonsterBehavior(dropTable)` ではなく、Master 経由で drop を確認する形に変える。

### 完了条件

- [ ] Runtime behavior に `IReadOnlyList<ActorDropEntry>` が存在しない
- [ ] `DropItemUseCase` が `IMasterRepository` から drop table を解決する
- [ ] Slime / Golem / Dragonkin の主要 drop が Master から抽選される EditMode test がある
- [ ] `rg -n "DropTable" Client/Assets/DungeonInn/Runtime/Scripts` で Runtime の drop table 保持が残っていない

## 3. Dungeon 深度帯と特殊部屋を Floor 生成結果に反映する

### 問題

現在は `DungeonDepthBandMaster.SpecialRoomType` を Application から判定できるが、`DungeonFloor` / `DungeonRoom` の生成結果には特殊部屋が反映されていない。
そのため BossRoom / TreasureRoom / RestRoom は gameplay に影響しない。

### 理想設計

特殊部屋は Domain の floor 生成結果として保持する。
ただし、選定ロジックは Application が Master と生成済み room を見て決める。

推奨構成:

- Domain
  - `DungeonRoomRole`
    - `Normal`
    - `Boss`
    - `Treasure`
    - `Rest`
  - `DungeonRoom`
    - `Role` を持つ
- Master
  - `DungeonDepthBandMaster`
    - `SpecialRoomType`
    - `SpecialRoomSelectionPolicy`
      - `None`
      - `HighestRouteDepth`
      - `RandomHighRouteDepth`
- Application
  - `AssignDungeonRoomRolesUseCase`
    - `DungeonDepthBandMaster` と `DungeonFloor.Rooms` から role を割り当てる
  - `GenerateDungeonFloorUseCase`
    - floor 生成後に role assignment を実行してから `Dungeon` に追加する

### 破壊的変更

- `DungeonRoom` constructor に `DungeonRoomRole` を追加する。
- `GetDungeonSpecialRoomTypeUseCase` は削除するか、room role query に置き換える。
- Spawn / Drop / Exploration は floor-level special type ではなく room role を参照する。

### 完了条件

- [ ] floor 5 の route depth 最大 room が `DungeonRoomRole.Boss`
- [ ] Middle band の対象 room が `DungeonRoomRole.Treasure`
- [ ] Deep band の対象 room が `DungeonRoomRole.Rest`
- [ ] Spawn / Drop / Encounter policy が `DungeonRoom.Role` を参照できる

## 4. Spawn を「深度帯」だけでなく「Room Role」まで見る

### 問題

現在の monster spawn は floor の depth band から spawn table を決めるが、部屋ごとの特殊性は見ていない。
BossRoom が存在しても、通常 spawn と同じ room 選択になる。

### 理想設計

Spawn は次の順に解決する。

1. floor index から `DungeonDepthBandMaster` を解決
2. room role から spawn context を作成
3. `SpawnTableResolver` が context に応じて spawn table を返す
4. `SpawnScheduledMonsterOrchestrator` は resolver の結果だけを使う

推奨構成:

- `SpawnContext`
  - `FloorIndex`
  - `DungeonDepthBandId`
  - `DungeonRoomRole`
- `SpawnTableResolver`
  - `ResolveMonsterSpawnTable(SpawnContext context)`
  - `ResolveAdventurerSpawnTable(...)`
- `DungeonDepthBandMaster`
  - normal monster table
  - boss room table
  - optional treasure/rest modifiers

### 破壊的変更

- `SpawnScheduledMonsterOrchestrator` から `masterRepository.GetDungeonDepthBandMasterForFloor()` 直呼びを削除する。
- Spawn table 解決を resolver に集約する。

### 完了条件

- [ ] Boss room では Boss spawn table が選ばれる
- [ ] Rest room では monster spawn が抑制される、または spawn weight が下がる
- [ ] `SpawnScheduledMonsterOrchestrator` に floor index / room role 分岐がない

## 5. Adventurer の職業差は Loadout Master で表現する

### 問題

現在の adventurer 職業差は `ActorArchetypeMaster.DefaultWeaponType` と stats に寄っている。
これは自然武器差であり、装備アイテムとしての初期装備ではない。

### 理想設計

`ActorArchetypeMaster` は archetype の能力テンプレート。
装備・初期 inventory は別 Master に分離する。

推奨構成:

- `ActorArchetypeMaster`
  - stats
  - behavior type
  - species
  - visual id
  - default natural weapon
  - `LoadoutMasterId`
- `ActorLoadoutMaster`
  - weapon item id
  - armor item id
  - accessory item ids
  - initial inventory items
- `ActorFactory`
  - `ActorLoadoutMaster` を解決して装備・所持品を付与する

### 破壊的変更

- `ActorArchetypeMaster` に装備 item id を直接増やさない。
- `ActorFactory` constructor は `IMasterRepository` だけで loadout まで解決する。
- 既存の「冒険者は初期装備を持たない」テストは仕様変更として削除または置換する。

### 完了条件

- [ ] Warrior が Sword / LightArmor を装備して spawn する
- [ ] Mage が Staff / Robe を装備して spawn する
- [ ] Archer が Bow を装備して spawn する
- [ ] Healer が Recovery item を initial inventory に持つ
- [ ] Scout が Dagger を装備して spawn する

## 6. Adventurer Spawn Table を progression で切り替える

### 問題

現在は 1 つの default adventurer spawn table に複数職業を入れている。
roadmap の「序盤は Warrior / Scout、中盤以降は Mage / Archer / Healer」をゲーム進行で表現できていない。

### 理想設計

冒険者 spawn は progression context で table を選ぶ。

推奨構成:

- `AdventurerSpawnBandMaster`
  - min day
  - min guild rank
  - spawn table id
  - priority
- `AdventurerSpawnTableResolver`
  - world day / guild progression / facility level を読んで table を決定
- Spawn tables
  - `Early Adventurer Spawn`
  - `Middle Adventurer Spawn`
  - `Advanced Adventurer Spawn`

### 破壊的変更

- default adventurer spawn table 1 つに全職業を入れる構成をやめる。
- `SpawnScheduledAdventurerOrchestrator` は resolver 経由で table を取得する。

### 完了条件

- [ ] Early table は Warrior / Scout の weight が高い
- [ ] Middle table は Mage / Archer / Healer の weight が上がる
- [ ] Spawn orchestrator に day / guild rank の if 文が散らばっていない

## 7. Recovery は Candidate Query と Effect Estimator に分離する

### 問題

現在の `RecoveryItemSelectionPolicy` は候補探索と選択を同時に行う。
また、AI の撤退判断に回復後推定 HP が渡っていない。

### 理想設計

回復関連は 3 層に分ける。

- `RecoveryItemCandidateQuery`
  - Actor inventory と Master から候補を列挙する
  - `ItemTag.Recovery` と `ActorEffectMaster` の HealHpOverTime を見る
- `RecoveryEffectEstimator`
  - Candidate の総回復量、tick、duration、最大 HP clamp 後の推定 HP を計算する
- `RecoveryItemSelectionPolicy`
  - missing HP、waste、item value を見て候補を選ぶ

AI / lifecycle 側:

- `UseRecoveryItemOrchestrator`
  - 使用タイミングと消費順序だけ担当
- `DecideAdventurerReturnUseCase`
  - `RecoveryEffectEstimator` を使い、回復後推定 HP で撤退判断する

### 破壊的変更

- `RecoveryItemSelectionPolicy.SelectItemId(Actor)` を削除し、candidate list を受け取る形にする。
- `UseRecoveryItemOrchestrator` は query -> policy -> usecase の順序制御に限定する。
- `DecideAdventurerReturnUseCase` に回復推定依存を追加する。

### 完了条件

- [ ] Recovery candidate list が item id 直指定なしで作られる
- [ ] Potion / HighPotion の推定回復量が test できる
- [ ] 撤退判断が回復後推定 HP を使う
- [ ] 回復 item がない場合は現 HP だけで判断する

## 8. WeaponType.Dagger は専用 calculator を持つ

### 問題

`WeaponType.Dagger` は `BowWeaponCalculator` を再利用している。
計算式が近くても、遠距離武器名の calculator を近接武器に使うのは意味上の負債。

### 理想設計

`DaggerWeaponCalculator` を追加する。

特性:

- Dexterity 主体
- Strength 副
- 近接 direct attack
- `WeaponTypeCombatMasterCatalog` の range は短距離

### 破壊的変更

- `WeaponCalculatorFactory.Create(WeaponType.Dagger)` を `DaggerWeaponCalculator` に変更する。

### 完了条件

- [ ] `DaggerWeaponCalculator` が存在する
- [ ] Dagger の attack 計算 test がある
- [ ] Dagger は projectile attack にならない

## 9. DungeonDepthBandMaster は fallback なしで必須解決にする

### 問題

`GenerateDungeonFloorUseCase` は `GetDungeonDepthBandMasterForFloor()` が失敗した場合に default generation settings へ fallback する。
これは Master 正典不備を隠す。

### 理想設計

Depth band は無限 dungeon の必須 Master。
floor が解決できない場合は設定不備として即失敗させる。

### 破壊的変更

- `GenerateDungeonFloorUseCase.ResolveSettings()` の `catch (KeyNotFoundException)` fallback を削除する。
- `HardcodedMasterRepository.GetDungeonDepthBandMasterForFloor()` は floor 1 以降すべて解決できることを test する。

### 完了条件

- [ ] `GenerateDungeonFloorUseCase` に default band fallback がない
- [ ] floor 1, 5, 8, 12, 100 が Master から解決できる test がある

## 10. PlayMode 確認を手動操作ではなく検証コード化する

### 問題

現在の Play 確認は Launcher から World へ遷移させ、ログ確認で成功を判断している。
floor 5 / 12、Boss spawn、Potion 使用のような M11 固有シナリオは手動確認に依存している。

### 理想設計

M11 integration verification を EditMode または PlayMode test として固定化する。

推奨 test:

- `DungeonDepthBandIntegrationTests`
  - floor 1 / 5 / 8 / 12 / 100 の band 解決
  - floor generation settings 解決
- `MonsterSpawnIntegrationTests`
  - floor 5 boss table
  - floor 12 endless table
  - spawned actor archetype が Master と一致
- `RecoveryAiIntegrationTests`
  - low HP actor が Potion を使う
  - HighPotion が選択される条件
  - recovery estimated HP が retreat 判断に効く

PlayMode は smoke test として残し、M11 仕様の確認は自動 test へ寄せる。

### 完了条件

- [ ] floor 5 / floor 12 / Potion / HighPotion の確認が自動 test にある
- [ ] PlayMode smoke test は `[World] GameWorldState initialized` と Error なしに限定される

## 推奨実装順

1. Master catalog 分割と validator 化
2. Drop 正典を Master 解決へ戻す
3. DungeonRoomRole 導入と特殊部屋割り当て
4. SpawnTableResolver 導入
5. ActorLoadoutMaster 導入
6. AdventurerSpawn progression resolver 導入
7. Recovery candidate / estimator / selection 分離
8. Dagger calculator 専用化
9. DepthBand fallback 削除
10. M11 integration tests 追加

## 最終到達状態

M11 の content は Master catalog と validator で管理される。
Runtime Actor / Behavior は Master 由来の drop、装備、回復効果、表示名、説明文を複製しない。
Dungeon floor は depth band と room role を生成結果として持ち、Spawn / Drop / AI はそれを Application policy 経由で参照する。
PlayMode は smoke test、自動 test は M11 固有仕様を検証する。
