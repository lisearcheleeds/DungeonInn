# Milestone 5 完了レビュー 第6回 Codex 全行レビュー

作成日: 2026-05-16

## レビュー範囲

- `docs/` 配下の Markdown 39 ファイルを確認した。
- レビュー基準として `docs/guidelines/` 配下の以下を確認した。
  - `lighthouse-patterns.md`
  - `coding-rules.md`
  - `domain-design-guidelines.md`
  - `application-boundary-guidelines.md`
  - `implementation-quality-guidelines.md`
  - `self-review-preset.md`
- `Client/Assets/DungeonInn/Runtime/Scripts` 配下の C# Script 355 ファイルを全件対象にした。
- `rg` による禁止 API / TODO / LINQ / allocation / public API / dependency scan の後、該当箇所を実ファイルで確認した。
- 既存レビューの対応ログを確認し、現行コード上で解消済みの項目は未解決として再掲していない。

## 総評

Milestone 5 の主目的である debug primitive 表示の撤去、chunk mesh map 表示、SpriteRenderer actor 表示、`WorldSimulationOrchestrator` 経由の進行分離は到達している。

一方で、全行レビューではハードゲート級の残りがある。特に `UnityEngine.UI.Button` の直接利用、bootstrap 例外 TODO の欠落、Runtime TODO の未分類、内部バッファ返却契約、Domain の Master 参照保持は、compile / tests が通っていても guideline 上は完了扱いにしにくい。Milestone 6 に入る前に、対応タスクとして明示するべき。

---

### 1. `UnityEngine.UI.Button` を直接拡張する Runtime API が残っている

重大度: 高

問題:

`Extensions/ButtonRxExtensions.cs` が `using UnityEngine.UI;` を持ち、`SubscribeOnClick(this Button button, Action onClick)` を公開している。`lighthouse-patterns.md` は `UnityEngine.UI.Button` を使わず Lighthouse の `LHButton` を使うことをハードゲートにしているため、未使用の helper であっても Runtime API として残すのは違反である。

原因:

UI 実装が Milestone 5 の主対象外だったため、過去の Unity UI 用 helper が Lighthouse UI 方針へ移行されないまま残っている。呼び出し箇所は現行 Runtime / Tests では見つからないが、API があることで今後の UI 実装が誤って `Button` に乗る経路を提供してしまう。

解決案:

未使用なら `ButtonRxExtensions.cs` を削除する。必要なら `LighthouseExtends.UIComponent` の `LHButton` 向け extension に置き換え、呼び出し側も `LHButton` だけを受ける形にする。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Extensions/ButtonRxExtensions.cs`
- `docs/guidelines/lighthouse-patterns.md`

完了条件:

- [ ] `rg -n "UnityEngine\\.UI|\\bButton\\b" Client/Assets/DungeonInn/Runtime/Scripts -g "*.cs"` で Lighthouse 例外以外の `UnityEngine.UI.Button` が残っていない
- [ ] UI click helper が必要な場合は `LHButton` 向け API になっている
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

### 2. `Launcher` の `SceneManager.LoadSceneAsync` 例外に移行 TODO がない

重大度: 高

問題:

`Core/Launcher.cs` は reboot 時に `UnityEngine.SceneManagement.SceneManager.LoadSceneAsync` を直接呼んでいる。`lighthouse-patterns.md` は bootstrap / reboot 用 `Launcher` だけ例外を認めているが、例外条件として「将来 Lighthouse 側に正式な bootstrap API が用意された場合の移行 TODO を残す」ことを要求している。現行コードにはその TODO がない。

原因:

`Launcher` は正当な例外箇所だが、例外の根拠と移行条件がコード上に残されていない。レビュー履歴では「bootstrap / reboot 例外」として扱われているが、guideline の例外条件を満たし切っていない。

解決案:

`LoadSceneAsync` の直前に `TODO(backlog)` または `TODO(milestone:X)` で「Lighthouse bootstrap API が提供された場合に置き換える」旨を明記する。あわせて通常画面遷移では `ISceneManager` を使う方針を維持する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Core/Launcher.cs`
- `docs/guidelines/lighthouse-patterns.md`

完了条件:

- [ ] `Launcher.cs` の `SceneManager.LoadSceneAsync` 直前に bootstrap 例外理由と移行 TODO がある
- [ ] 通常ゲーム画面遷移で `SceneManager.LoadScene` / `LoadSceneAsync` を直接使っていない
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

### 3. 実挙動に影響する TODO が milestone / task に紐付いていない

重大度: 高

問題:

Runtime code に未分類の `TODO:` が 9 件残っている。Spawn timing / limit / spawn point / faction hostility / arrival distance は現在のゲーム挙動に直接影響する。`implementation-quality-guidelines.md` は、実挙動・性能に影響する TODO を milestone / task に紐付けず残すことをハードゲートにしている。

今回の全行レビューで、前回回答した Spawn / Faction 周辺に加えて `MoveActorTowardDestinationUseCase` の arrival distance TODO も確認した。

原因:

Milestone 2-5 の最小実装で仮値を置いた後、正式な master / constants / faction policy へ移すタスクが TODO コメント側に反映されていない。設計上は後続対応の可能性が高いが、コード上では対応予定と責務が追跡できない。

解決案:

各 TODO を `TODO(milestone:6):`、`TODO(task:xxxx):`、または `TODO(backlog):` に分類する。現在の挙動を仕様として残す場合は docs / task 側へ「暫定値ではなく仕様」と明記する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Movement/MoveActorTowardDestinationUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Spawn/SpawnScheduledAdventurerOrchestrator.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Spawn/SpawnScheduledMonsterOrchestrator.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat/CombatEncounterTargetResolver.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Event/GameEventBus.cs`
- `docs/guidelines/implementation-quality-guidelines.md`

完了条件:

- [ ] `rg -n "TODO:" Client/Assets/DungeonInn/Runtime/Scripts` で未分類 TODO が残っていない
- [ ] arrival distance / spawn timing / spawn limit / spawn point / faction hostility の正式対応先が task または docs に記録されている
- [ ] 暫定値を維持する場合、GameConstants または design docs に根拠が記録されている

---

### 4. Domain の Runtime Instance が Master row / Spec 参照を保持している

重大度: 高

問題:

`domain-design-guidelines.md` は Runtime Instance / State / Entity がマスタ行を参照する場合、原則として保持するのは `XxxMasterId` のみと定めている。現行 Domain では `ActorEquipment` が `EquipmentMaster` / `WeaponMaster` を保持し、`Actor` が `WeaponTypeCombatMaster` を保持し、`ActiveStatusEffect` が `StatusEffectSpec` を公開している。

これは動作上は成立しているが、MasterMemory や外部マスタ管理へ移行したときに Domain Entity がマスタ行の形へ密結合する。特に `IReadOnlyActorEquipment` が `EquipmentMaster` / `WeaponMaster` を外部へ公開しているため、集約外からマスタ行経由で表示・計算が広がる。

原因:

装備・武器・状態異常の計算に必要な値をすぐ参照できるよう、Domain Entity 内に Master オブジェクトを保持する設計になっている。過去タスクで Master 導入を優先した結果、`XxxMasterId` と Application 側 resolver / spec 変換の境界整理が後回しになっている。

解決案:

短期的には、この設計を意図的な例外として docs/design または task に記録する。根治する場合は、Runtime Instance は `EquipmentItemId` / `WeaponType` / `ActorEffectMasterId` などの ID と変化する runtime state だけを保持し、UseCase / Calculator 境界で Repository から Master を解決する。複数マスタを束ねて実行時に使うものは `Spec` として構築責務を明確にする。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Actor/Actor.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Actor/ActorEquipment.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Actor/ActorEffectInstance.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Actor/ActiveStatusEffect.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Combat/DirectWeaponCombatCalculator.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Combat/WeaponCombatCalculatorFactory.cs`
- `docs/guidelines/domain-design-guidelines.md`
- `docs/guidelines/application-boundary-guidelines.md`

完了条件:

- [ ] Runtime Instance / State / Entity が保持する Master 参照を ID / runtime state / Spec のいずれにするか task で決定されている
- [ ] 例外として継続する場合、docs/design または task ログに根拠と影響範囲が記録されている
- [ ] `IReadOnlyActorEquipment` が集約外へ Master 行を無制限に公開しない形になっている、または例外根拠がある
- [ ] 変更後に actor param / weapon combat / status effect の EditMode test が成功している

---

### 5. 表示 DataProvider が内部バッファ参照を契約不明な API として返している

重大度: 高

問題:

`ActorViewDataStore.ConsumeChanges()` は内部 `changedActors` / `removedActorIds` を `IReadOnlyList` として返し、次回呼び出しで `Clear()` する。`WorldMapViewDataProvider.GetLayers()` も内部 `layers` を返し、次回呼び出しで内容を組み替える。`implementation-quality-guidelines.md` は内部バッファ参照を契約不明な API として返さないことをハードゲートにしている。

現行の `WorldActorPresenter` / `WorldMapView` は即時消費しているため今は破綻しないが、呼び出し側が結果を保持した瞬間に内容が空または別内容へ変わる。

原因:

GC Alloc を避けるために provider 内部リストを再利用しているが、API 名・コメント・型が「次回呼び出しまでしか有効でない」契約を表していない。`ConsumeChanges` は名前にヒントがあるが、返却リスト寿命までは明記されていない。`GetLayers` は通常の snapshot 取得に見える。

解決案:

`DrainChangesTo` / `CopyLayersTo` のように呼び出し側バッファへ書き出す API にするか、callback 消費形式に変更する。内部バッファを返す方針を維持する場合は、返却値の有効期間を XML comment か型名で明示する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldViewDataProviders.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldActorPresenter.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldMapView.cs`
- `docs/guidelines/implementation-quality-guidelines.md`

完了条件:

- [ ] `ActorViewDataStore.ConsumeChanges()` が契約不明な内部 list を返していない
- [ ] `WorldMapViewDataProvider.GetLayers()` が契約不明な内部 list を返していない
- [ ] 内部バッファ再利用を続ける場合、返却値の寿命が API 名またはコメントで明示されている
- [ ] `WorldActorPresenter` / `WorldMapView` が新しい契約に従って即時消費または drain している

---

### 6. `ActorCombatPowerCalculator` が未使用 DI と allocation の両方を持っている

重大度: 中

問題:

`ActorCombatPowerCalculator` は `IMasterRepository masterRepository` を DI 注入してフィールド保持しているが、`Calculate()` 内では一度も使っていない。また `actor.Equipment.AllStatBonuses.Sum(...)` と `actor.Equipment.All.Sum(...)` を呼ぶため、`ActorEquipment.AllStatBonuses` の `new List<StatBonus>()`、`ActorEquipment.All` の `ToArray()`、LINQ iterator が重なる。

`SelectDungeonTargetFloorUseCase` から戦闘力評価に使われるため、Actor 数や floor 数が増えると不要な GC Alloc と不要依存が同時に残る。

原因:

当初は MasterRepository を使って戦闘力を解決する想定だった可能性があるが、現在は Actor が Master 参照を持つため repository が不要になっている。さらに装備一覧 API が snapshot 風の property で allocation を隠している。

解決案:

未使用の `IMasterRepository` 注入を削除し、LifetimeScope 登録とテストを合わせる。戦闘力計算は `foreach` で装備・ボーナスを走査し、必要なら `IReadOnlyActorEquipment` に allocation しない列挙 API を用意する。ただし API 追加は production 契約変更として task に記録する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat/ActorCombatPowerCalculator.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Actor/ActorEquipment.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Ai/SelectDungeonTargetFloorUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldLifetimeScope.cs`
- `docs/guidelines/application-boundary-guidelines.md`
- `docs/guidelines/implementation-quality-guidelines.md`

完了条件:

- [ ] `ActorCombatPowerCalculator` に未使用の `IMasterRepository` field / constructor parameter が残っていない
- [ ] 戦闘力計算経路に `Equipment.All` / `AllStatBonuses` 経由の `ToArray()` / `new List` / LINQ chain が残っていない
- [ ] 必要な Runtime API 変更は production 契約変更として task / review に記録されている
- [ ] `uloop.cmd run-tests --project-path Client --test-mode EditMode` が成功している

---

### 7. `DecideAdventurerReturnUseCase` が dirty 評価ごとに `HashSet<Guid>` を生成している

重大度: 中

問題:

`DecideAdventurerReturnUseCase.ExecuteAsync()` は dirty actor 評価時に `var foundDirtyActorIds = new HashSet<Guid>();` を生成する。毎フレーム全 Actor 走査ではないが、ゲームループ中に継続実行される dirty 評価で再利用可能な集合を都度生成している。

原因:

`actorIdBuffer` はフィールド再利用されている一方、missing dirty actor cleanup 用の set はメソッドローカルに残っている。buffer 所有者の方針が同一 UseCase 内で揃っていない。

解決案:

`readonly HashSet<Guid> foundDirtyActorIds = new();` を field 化し、実行前に `Clear()` する。あわせて `AdventurerReturnTrackingService.RemoveMissingDirtyActors()` の引数契約を確認し、必要なら caller buffer 方式へ統一する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Lifecycle/DecideAdventurerReturnUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Lifecycle/AdventurerReturnTrackingService.cs`
- `docs/guidelines/application-boundary-guidelines.md`

完了条件:

- [ ] `DecideAdventurerReturnUseCase.ExecuteAsync()` 内に `new HashSet<Guid>()` が残っていない
- [ ] dirty cleanup 用 set は field 再利用され、実行前に `Clear()` されている
- [ ] 帰還判断の EditMode test が成功している

---

### 8. `WorldMapView.UpdateVisuals()` が毎フレーム全 layer を確認している

重大度: 中

問題:

`WorldMapView.UpdateVisuals()` は毎フレーム `EnqueueMissingLayerTiles()` を呼び、`viewDataProvider.GetLayers()` で Ground と Dungeon floor を列挙する。`scheduledLayerIds` により chunk 二重生成は防いでいるが、未構築 layer がないフレームでも layer 一覧取得と scheduled check は残る。

原因:

map layer 追加の通知や revision がなく、View が毎フレーム polling して未構築 layer を探す構造になっている。Milestone 5 で chunk 生成の分割は行われたが、layer 追加検出は差分駆動になっていない。

解決案:

初期化時と floor 追加時だけ layer build request を投入する。`DungeonFloorGenerated` 相当のイベント、revision / dirty flag、または明示 refresh API のいずれかに寄せ、`UpdateVisuals()` は pending chunk の消化だけを担当する。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldMapView.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldViewDataProviders.cs`
- `docs/guidelines/application-boundary-guidelines.md`
- `docs/self-review/milestone5-completion-review-4-total-manual.md`

完了条件:

- [ ] `WorldMapView.UpdateVisuals()` が毎フレーム `GetLayers()` で全 layer を確認していない
- [ ] 新規 layer 追加検出がイベント、revision、明示 refresh のいずれかで行われている
- [ ] pending chunk の分割生成は維持されている
- [ ] floor 追加時の map chunk 生成が test または PlayMode 確認で検証されている

---

### 9. using 順序違反と self namespace using が複数残っている

重大度: 低

問題:

`coding-rules.md` は using 順序を `System`、Unity、LighthouseExtends、VContainer、プロジェクト内の順にする方針を定義している。現行コードでは `DungeonInn.*` が `System` より前にあるファイルや、同一 namespace の `using DungeonInn.Application.GameLoop;` のような self namespace using が残っている。

原因:

Milestone 5 の実装・修正で局所的な using 追加が行われ、全体の code cleanup が統一されていない。挙動には影響しないが、完了前チェックとしては未達である。

解決案:

対象ファイルを coding rule 順に整理する。可能なら IDE / `.editorconfig` で using order を固定し、レビュー時の手作業検出に依存しないようにする。

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/GameLoop/GameLoopUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/WorldViewDataProviders.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldMapView.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Movement/MoveActorTowardDestinationUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Lifecycle/DecideAdventurerReturnUseCase.cs`
- `docs/guidelines/coding-rules.md`

完了条件:

- [ ] 指摘ファイルの using が coding rule の順序に揃っている
- [ ] self namespace using と未使用 using が削除されている
- [ ] `uloop.cmd compile --project-path Client` が成功している

---

## 確認済みで再掲しない項目

- `WorldActorDebugVisualizer` は現行 Runtime scripts には残っていない。
- `GameObject.CreatePrimitive` / debug Sphere / Plane の通常表示経路は現行 Runtime scripts では見つからない。
- `Resources.Load` / `Addressables.LoadAssetAsync` の直接利用は現行 Runtime scripts では見つからない。
- `Camera.main` 依存は現行 Runtime scripts では見つからない。
- Domain Entity から `XxxCatalog.Get()` / `Xxx.Instance` を直接呼ぶ箇所は現行 Domain scripts では見つからない。`WeaponTypeCombatMasterCatalog.CreateAll()` は `Master/HardcodedMasterRepository` 側のみ。
- `AStarPathfinder.FindPath()` には allocation 版 API が残るが、Runtime の実呼び出しは `ActorNavigationService` から buffer 注入版 `TryFindPath()` のみだったため、今回の未解決項目からは外す。

## 全行レビューで実施した主な検索

- `rg --files Client/Assets/DungeonInn/Runtime/Scripts -g "*.cs"`: 355 files
- `rg -n "TODO|FIXME|HACK|XXX" Client/Assets/DungeonInn/Runtime/Scripts`
- `rg -n "Resources\\.Load|Addressables\\.LoadAssetAsync|SceneManager\\.LoadScene|UnityEngine\\.UI\\.Button|\\bButton\\b|Camera\\.main" Client/Assets/DungeonInn/Runtime/Scripts`
- `rg -n "ToArray\\(|ToList\\(|Where\\(|Select\\(|OrderBy|new HashSet<|new List<" Client/Assets/DungeonInn/Runtime/Scripts`
- `rg -n "Catalog\\.Get|Registry|Locator|Instance|WeaponTypeCombatMasterCatalog" Client/Assets/DungeonInn/Runtime/Scripts/Domain Client/Assets/DungeonInn/Runtime/Scripts/Application Client/Assets/DungeonInn/Runtime/Scripts/Master`
- `rg -n "UnityEngine\\.UI|SubscribeOnClick|LHButton|LighthouseExtends\\.UIComponent" Client/Assets/DungeonInn/Runtime/Scripts Client/Assets/DungeonInn/Tests`

## 最終チェック

- [x] 各レビュー項目に「問題」がある
- [x] 各レビュー項目に「原因」がある
- [x] 各レビュー項目に「解決案」がある
- [x] 各レビュー項目に「根拠となるファイルリスト」がある
- [x] 各レビュー項目に「完了条件」がある
- [x] 既存レビューの対応ログを確認した
- [x] 解消済み項目を未解決として扱っていない
- [x] 未解決項目を対応済みとして扱っていない
- [x] 同じ指摘は再発または既存未解決として扱った
- [x] レビュー結果を `docs/self-review/` 配下に保存した

## 検証

- [x] `uloop.cmd compile --project-path Client` 成功（Error 0 / Warning 0）
- [x] `uloop.cmd run-tests --project-path Client --test-mode EditMode` 成功（245 passed）
