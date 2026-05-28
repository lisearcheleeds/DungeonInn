# Milestone 11 After Review 1 — Codex

## 前提

- 対象: `docs/roadmap/milestone11-roadmap.md`
- レビュー主体: Codex 単独セルフレビュー
- 実装確認:
  - `uloop.cmd compile --project-path Client`: 成功
  - `uloop.cmd run-tests --project-path Client --test-mode EditMode`: 318 / 318 pass
  - Launcher から Play 開始後、World 遷移を実行し 30 秒動作確認
  - `[World] GameWorldState initialized. Facilities=3 DungeonFloors=1 Actors=0` を確認
  - Play ログに Error なし

## 差分分類と許可理由

production 契約変更:

- `IMasterRepository`
  - `DungeonFloorExplorationMaster` / `DungeonDepthBandConfig` をやめ、`DungeonDepthBandMaster` を正典に変更した。
  - ユーザー確認により、ゲームバランス値は Config ではなく Master に置く方針で合意済み。
- `InitializeGameWorldRequest` / Dungeon 生成系 public method
  - 旧 depth band config 受け渡しを削除し、Master repository 参照へ寄せた。
  - 旧 API を互換目的で残さないという roadmap / AGENTS 方針に従う変更。
- `GameSessionLifetimeScope`
  - `RecoveryItemSelectionPolicy` と `GetDungeonSpecialRoomTypeUseCase` を登録した。
  - どちらも Application 層の判断責務で、GameSession 中に使うため GameSession scope が妥当。

新規概念追加ゲート:

- `DungeonDepthBandMaster`
  - 類似: `DungeonDepthBandConfig`, `DungeonFloorExplorationMaster`
  - 差分: 深度帯、スポーン表、難度係数、特殊部屋種別、生成設定を Master 正典として束ねる。
  - 代替不可理由: Config はバランス正典として不適切、Floor master は固定階層しか扱えない。
  - 統合削除条件: 外部 Master load へ移行し、同等 schema が導入された場合。
- `DungeonSpecialRoomType`
  - 類似: `DungeonRoom`
  - 差分: 部屋インスタンスではなく、深度帯から選ばれる特殊部屋分類。
  - 代替不可理由: View 専用状態に寄せず Application から判定可能にする必要がある。
  - 統合削除条件: `DungeonRoom` に特殊部屋分類を持たせる設計へ移行した場合。
- `GetDungeonSpecialRoomTypeUseCase`
  - 類似: `GetDungeonLayerInfoUseCase`
  - 差分: UI summary ではなく、floor index から Application が特殊部屋分類を判定する入口。
  - 代替不可理由: Layer info は表示 summary で、特殊部屋判定の用途と責務が違う。
  - 統合削除条件: Dungeon floor 生成時に room 単位の特殊部屋情報を保持するようになった場合。
- `RecoveryItemSelectionPolicy`
  - 類似: `UseRecoveryItemOrchestrator`
  - 差分: 使用順序ではなく、候補 item の回復量と無駄回復量による選択を担当する。
  - 代替不可理由: Orchestrator に候補比較を入れると順序制御と選択 policy が混ざる。
  - 統合削除条件: AI policy 側に共通 consumable selection policy が導入された場合。

## Phase 1: 既存 Master / Domain 契約の棚卸し

判定: 一部達成

満たした点:

- `SpeciesMaster`, `ActorArchetypeMaster`, `AdventurerSpawnMaster`, `SpawnTableMaster`, `ItemMaster`, `EquipmentMaster`, `WeaponMaster`, `ActorEffectMaster` を既存責務に沿って拡張した。
- 旧 `DungeonDepthBandConfig` と `DungeonFloorExplorationMaster` は互換目的で残さず削除した。
- Runtime Instance に Master 由来の名前や効果説明を複製していない。

問題:

M11 追加 content の完全な一覧と参照 ID が roadmap / task log に事前記録されていない。現状は `HardcodedMasterRepository` とこのレビューから逆引きする状態。

原因:

実装時に Master 行の追加を先行し、ID 一覧を docs 側に同期する作業を別工程として扱わなかった。

解決案:

`docs/roadmap/milestone11-roadmap.md` または別 self-review log に、追加 item / species / archetype / spawn table / actor effect / dungeon band の ID 一覧を追記する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Master/HardcodedMasterRepository.cs`
- `docs/roadmap/milestone11-roadmap.md`

完了条件:

- [ ] M11 追加 Master の ID 一覧が docs から確認できる
- [ ] `HardcodedMasterRepository.ValidateReferences()` が引き続き成功する

## Phase 2: ダンジョン深度帯と特殊部屋

判定: 一部達成

満たした点:

- `DungeonDepthBandMaster` に Shallow / Middle / Deep / Boss / Endless を定義した。
- `GenerateDungeonFloorUseCase`, `SelectDungeonTargetFloorUseCase`, `SpawnScheduledMonsterOrchestrator`, `GetDungeonLayerInfoUseCase` が深度帯 Master を参照する。
- 12 階以降は Endless band として解決される。
- `GetDungeonSpecialRoomTypeUseCase` から BossRoom / TreasureRoom / RestRoom の分類を Application が判定できる。

問題:

特殊部屋は「floor の band から分類を返す」段階で止まっており、`DungeonRoom.RouteDepth` などを使った実際の room candidate 選定や報酬・敵出現補正には接続されていない。

原因:

M11 実装では特殊部屋の専用見た目不要という条件を優先し、Application 判定入口だけを追加した。Dungeon floor 内の room 単位状態へ反映する設計までは進めていない。

解決案:

`SelectDungeonSpecialRoomUseCase` 相当を追加し、`DungeonDepthBandMaster.SpecialRoomType` と `DungeonFloor.Rooms` から BossRoom / TreasureRoom / RestRoom の対象 room id を選ぶ。補正は Spawn / Drop policy から参照できるようにする。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Master/DungeonDepthBandMaster.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Dungeons/GetDungeonSpecialRoomTypeUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Dungeons/GenerateDungeonFloorUseCase.cs`

完了条件:

- [ ] floor 5 で BossRoom candidate room id が Application から取得できる
- [ ] TreasureRoom / RestRoom の対象 room id が Application から取得できる
- [ ] RouteDepth が高い room を優先する EditMode test がある

## Phase 3: モンスター種族・エリート・ボス

判定: おおむね達成

満たした点:

- Slime / Goblin / Wolf / Skeleton / Bat / Orc / Golem / Dragonkin を `SpeciesMaster` に定義した。
- 能力値、Behavior、自然武器、visual id は `ActorArchetypeMaster` に置いた。
- Elite / Boss は `Actor` の bool ではなく、`ActorArchetypeMaster` id 201 / 202 として定義した。
- 深度帯ごとの monster `SpawnTableMaster` から通常 / Elite / Boss 候補を解決できる。
- Boss 討伐を勝利条件に接続する変更は入れていない。

問題:

Boss を倒してもゲームが継続することを直接検証するテストは追加していない。

原因:

Boss は通常 monster archetype と同じ生成・戦闘・defeat 経路に乗るため、専用終了条件を追加しなければ継続するという実装上の推論に依存している。

解決案:

Boss archetype を倒す `ActorDefeatOrchestrator` の EditMode test を追加し、WorldState / GameSession が終了しないこと、通常 drop / xp 経路だけが動くことを確認する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Master/HardcodedMasterRepository.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat/ActorDefeatOrchestrator.cs`

完了条件:

- [ ] archetype 202 の Actor defeat 後も `IGameWorldState.IsInitialized` が true
- [ ] Boss defeat で勝利 / 終了 event が発行されない

## Phase 4: 冒険者職業と Spawn パターン

判定: 一部達成

満たした点:

- Warrior / Mage / Archer / Healer / Scout 相当の `ActorArchetypeMaster` を追加した。
- すべて `ActorBehaviorType.Adventurer` を使用し、職業別 Behavior 型は追加していない。
- 固有名は `AdventurerSpawnMaster.DisplayName`、テンプレート名は `ActorArchetypeMaster.Name` として分離した。
- `SpawnOnce` は固有名付き spawn に true、汎用 spawn に false を設定した。
- AI / View に職業名 switch は追加していない。

問題:

冒険者の初期装備テンプレートは未実装。現状は `ActorArchetypeMaster.DefaultWeaponType` による自然武器差で表現しており、Inventory / Equipment としての初期装備ではない。

原因:

既存 `ActorArchetypeMaster` に初期装備 item id を持つ契約がなく、M11 実装では契約拡張を最小限にして既存生成経路を維持した。

解決案:

`ActorArchetypeMaster` に初期装備 item id 群を追加するか、別 `ActorArchetypeEquipmentMaster` を導入し、`ActorFactory` / `SpawnAdventurerUseCase` が装備状態へ反映する。追加時は既存の「SpawnAdventurerDoesNotGrantRookieEquipment」系テストを仕様に合わせて更新する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Master/ActorArchetypeMaster.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Master/HardcodedMasterRepository.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Spawn/ActorFactory.cs`

完了条件:

- [ ] Warrior / Mage / Archer / Healer / Scout の初期装備 item id が Master から解決できる
- [ ] Spawn 後の Actor に装備が反映される
- [ ] 固有名付き spawn だけが `SpawnOnce == true`

追加問題:

序盤 / 中盤以降の冒険者 spawn 比率は 1 つの default adventurer spawn table 内の重みだけで表現されており、ゲーム進行に応じた table 切替は未実装。

完了条件:

- [ ] 進行段階または施設 / 時間条件から adventurer spawn table を切り替える policy がある
- [ ] 序盤 table で Warrior / Scout 比率が高い
- [ ] 中盤以降 table で Mage / Archer / Healer 比率が上がる

## Phase 5: 装備・アイテム・消耗品

判定: おおむね達成

満たした点:

- 追加 item を `ItemMaster` に定義した。
- 装備は `EquipmentMaster`、武器戦闘値は `WeaponMaster` / `WeaponTypeCombatMasterCatalog` に分離した。
- Potion / HighPotion は `ItemMaster.ActorEffectMasterId` から `ActorEffectMaster` に接続されている。
- `UseConsumableItemUseCase` に Potion / HighPotion 固定効果の直書きはない。

問題:

`WeaponType.Dagger` の攻撃計算が `BowWeaponCalculator` を再利用している。動作はするが、Dagger が遠距離武器ではないにもかかわらず Bow の計算名に依存しており、武器種の意味と実装名が一致していない。

原因:

Dagger 用 calculator を新規追加せず、Dexterity 主体の既存計算を流用した。

解決案:

`DaggerWeaponCalculator` を追加し、Dexterity 主体の近接攻撃計算として明示する。`WeaponCalculatorFactory` の Dagger 分岐を専用 calculator へ変更する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Actor/WeaponCalculatorFactory.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Item/WeaponType.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Master/WeaponTypeCombatMasterCatalog.cs`

完了条件:

- [ ] `WeaponType.Dagger` が `DaggerWeaponCalculator` を返す
- [ ] Dagger の stat weight と attack 計算の EditMode test がある

## Phase 6: Drop Table と報酬の整備

判定: 一部達成

満たした点:

- 追加 Species に `SpeciesDrops` を設定した。
- Drop 抽選は既存 `DropItemUseCase` が `IActorDropSource.DropTable` と `IGameRandom` を使う。
- Gold は Item id 1 として扱う既存方針を維持した。
- View / Presenter 側に Drop 決定処理は追加していない。

問題:

M11 追加 Species の主要 drop を直接検証する EditMode test が不足している。既存 `DropItemUseCaseTests` は drop 抽選機構の検証であり、Slime / Golem / Dragonkin などの Master drop 内容までは検証していない。

原因:

`MasterRepositoryTests` では一部 item と archetype の存在確認に留め、全追加 Species の drop id / 確率 / count の確認を網羅しなかった。

解決案:

`MasterRepositoryTests` または専用 test に、追加 Species 全件の `SpeciesDrops` が空でないこと、drop item id が存在すること、代表種族の主要素材 id が含まれることを追加する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Master/HardcodedMasterRepository.cs`
- `Client/Assets/DungeonInn/Tests/EditMode/DropItemUseCaseTests.cs`
- `Client/Assets/DungeonInn/Tests/EditMode/MasterRepositoryTests.cs`

完了条件:

- [ ] Slime が `Slime Gel` を drop table に持つ test がある
- [ ] Golem が `Golem Core` を drop table に持つ test がある
- [ ] Dragonkin が `Dragon Scale` を drop table に持つ test がある

追加問題:

Elite / Boss の報酬補正方針は Master の spawn level / archetype 能力差に留まり、drop / reward policy として明文化・実装されていない。

完了条件:

- [ ] Elite / Boss の報酬補正を Master または Application policy で追える
- [ ] Boss archetype defeat 時の drop / xp が通常個体より高いことを test で確認できる

## Phase 7: 回復アイテムと AI 組み込み

判定: 一部達成

満たした点:

- Potion / HighPotion は Inventory から 1 個消費される。
- ActorEffect が付与され、時間経過で HP が回復する。
- 最大 HP 超過は `Actor` 側の回復処理で抑制される。
- 同一 Potion 再使用の `AppendDuration` は既存 test で確認済み。
- `RecoveryItemSelectionPolicy` は `ItemTag.Recovery` と `ActorEffectMaster` の効果内容から候補を選ぶ。
- Potion / HighPotion の無駄回復量比較 test を追加した。

問題:

AI の戦闘継続 / 撤退判断に「回復後推定 HP」を使う実装は未対応。現在は低 HP の探索中 Actor が回復アイテムを使う経路まで。

原因:

`UseRecoveryItemOrchestrator` は回復使用の順序制御であり、`DecideAdventurerReturnUseCase` や combat AI の評価式に回復後 HP 推定を渡す設計までは追加していない。

解決案:

回復候補の推定回復量を返す query / policy を `DecideAdventurerReturnUseCase` または combat continuation 判定に接続し、回復 item 使用後の expected hp を行動判断に含める。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Lifecycle/RecoveryItemSelectionPolicy.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Lifecycle/UseRecoveryItemOrchestrator.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Lifecycle/DecideAdventurerReturnUseCase.cs`

完了条件:

- [ ] 回復候補ありの場合、撤退判断が回復後推定 HP を参照する
- [ ] 回復候補なしの場合、現 HP で撤退判断する
- [ ] 上記 2 ケースの EditMode test がある

追加問題:

「HP 50% 以下、または Potion の総回復量を無駄なく受けられるだけ HP が減っている」という条件の後半は明示実装されていない。

完了条件:

- [ ] LowHpRatio を超えていても、missing HP が Potion 回復量以上なら使用候補になる
- [ ] Potion / HighPotion の選択が item id 直指定なしで行われる

## Phase 8: 統合確認とレビュー

判定: 一部達成

満たした点:

- compile 成功。
- EditMode test 318 件 pass。
- Launcher から Play を開始し、World 遷移後の `[World] GameWorldState initialized` を確認した。
- Play ログに Error はない。

問題:

PlayMode で地下 5 階、12 階以降の生成・spawn、Boss floor の Boss 候補、Potion / HighPotion の実使用までは直接確認していない。

原因:

30 秒起動確認は World 初期化と通常 game loop の起動確認に留まり、深層 floor 生成や HP 減少状態の人工再現までは行っていない。

解決案:

uLoop dynamic code または専用 EditMode / PlayMode test で、floor 5 / 12 を生成し、該当 depth band の spawn table と特殊部屋分類を確認する。HP を減らした Actor に Potion / HighPotion を持たせ、AI recovery 経路を PlayMode で確認する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Dungeons/EnsureDungeonFloorGeneratedOrchestrator.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Spawn/SpawnScheduledMonsterOrchestrator.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Lifecycle/UseRecoveryItemOrchestrator.cs`

完了条件:

- [ ] PlayMode または integration test で floor 5 の Boss band 解決を確認する
- [ ] PlayMode または integration test で floor 12 の Endless band 解決を確認する
- [ ] PlayMode で Potion / HighPotion 使用ログまたは ActorEffect 付与を確認する

## 配置方針レビュー

判定: おおむね達成

満たした点:

- Master は `DungeonInn.Master` に配置した。
- Application の判断は `Application/Dungeons`, `Application/Actors/Lifecycle`, `Application/Actors/Ai`, `Application/Actors/Spawn` に配置した。
- 新規画像、Prefab、Addressable は追加していない。
- GameSession 中に使う UseCase / Policy は `GameSessionLifetimeScope` に登録した。

問題:

`GetDungeonSpecialRoomTypeUseCase` は Application 判定入口として妥当だが、現時点では呼び出し元がなく、特殊部屋効果の実利用に至っていない。

完了条件:

- [ ] Spawn / Drop / Dungeon exploration のいずれかが特殊部屋判定を参照する

## 静的検索レビュー

判定: おおむね達成

確認済み:

- `DungeonDepthBandConfig`, `DungeonFloorExplorationMaster` の Runtime 参照は消えている。
- `IsBoss`, `IsElite`, `WarriorBehavior`, `MageBehavior`, `ArcherBehavior` は追加していない。
- 対象差分内で `Resources.Load`, `Addressables.`, `SceneManager.LoadScene`, `UnityEngine.Random` は追加していない。

注意:

- `private set` は既存 Domain / Runtime state に残っているが、今回差分で新規追加した禁止対象ではない。

## 総合判定

Milestone 11 は、Master 正典化、深度帯、追加コンテンツ、HighPotion、回復選択、基本統合確認までは達成している。

ただし、roadmap の全完了条件で見ると以下が未達のため、「M11 完全完了」ではなく「主要実装完了、一部レビュー指摘あり」と判定する。

- 特殊部屋の room candidate 選定と効果接続
- 冒険者の初期装備
- 序盤 / 中盤以降の冒険者 spawn table 切替
- M11 追加 Species drop の代表 test
- Elite / Boss 報酬補正の明文化・検証
- 回復後推定 HP を使った戦闘継続 / 撤退判断
- PlayMode で floor 5 / 12 / Potion 実使用までの確認
