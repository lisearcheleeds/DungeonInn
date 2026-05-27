# Milestone 9 Completion Review 1 - Claude

レビュー日時: 2026-05-26

対象マイルストーン: Milestone 9 — ギルド経営UI / Market

参照ドキュメント:
- `docs/guidelines/lighthouse-patterns.md`
- `docs/guidelines/coding-rules.md`
- `docs/guidelines/domain-design-guidelines.md`
- `docs/guidelines/application-boundary-guidelines.md`
- `docs/guidelines/implementation-quality-guidelines.md`
- `docs/guidelines/debugging-policy.md`
- `docs/guidelines/self-review-guidelines.md`
- `docs/roadmap/milestone9-roadmap.md`

レビュー対象の主要ファイル:
- `Application/Economy/` 配下の全 UseCase / Service
- `View/Scene/ModuleScene/GameHUD/` 配下のウィンドウ実装
- `Master/HardcodedMasterRepository.cs`
- `Master/ActorArchetypeMaster.cs`
- `Application/Actors/Spawn/ActorFactory.cs`
- `Application/World/IGameWorldState.cs`

---

## 総合判定: NO GO

以下の理由から Milestone 9 を完了扱いにはできない。

1. Task 8（新米冒険者の初期装備付与削除）が実装ログで「完了」と報告されているが、コードに反映されていない。ロードマップ完了条件「新米冒険者が Spawn 時に装備を直接受け取らない」が未達。
2. `UpgradeFacilityUseCase` / `FulfillMarketOfferUseCase` が `IGameWorldStateReader` を注入しながら Domain エンティティへの書き込み操作を行っており、Application Boundary Guidelines §11 の Reader/Writer 分離に違反している。
3. View 層クラス `GameHudWindowOpenService` が Application UseCase `FulfillMarketOfferUseCase` を直接注入・呼び出しており、Application Boundary Guidelines §7 の View → UseCase 直接依存禁止に違反している。
4. Codex が既に指摘しているが、`GuildManagementWindow` からアップグレード実行ボタンへの導線がなく、Task 5 の完了条件「合算 inventory 上で素材が足りる場合にアップグレードできる」が UI 操作として成立していない。

---

## フェーズ 1: コーディングルール (Coding Rules)

### F1-1: `private set` アクセサは現行コードでは許容される範囲

重大度: 情報

問題: Domain / Application の各プロパティに `{ get; private set; }` が多数存在する。

原因: `coding-rules.md` §3「明示的 `private` は書かない」の対象はフィールドおよびメソッドであり、プロパティの `private set` は対象外。既存コードを確認した限り、M9 追加範囲において `private void` / `private static` / `private readonly` などのフィールド・メソッドへの明示 `private` は検出されなかった。

解決案: 対応不要。

根拠となるファイルリスト:
- `docs/guidelines/coding-rules.md`

完了条件:
- [x] 確認済み。M9 追加ファイルで `private` フィールド・メソッドは存在しない。

---

### F1-2: `>` 比較演算子の使用なし

重大度: 情報

問題: `coding-rules.md` §11-1「比較演算子は `<` のみ使用、`>` は禁止」について、M9 追加範囲（Economy、GameHUD、Master）のすべてのファイルを grep で確認した。

原因: 違反なし。

解決案: 対応不要。

根拠となるファイルリスト:
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Economy/`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/`
- `Client/Assets/DungeonInn/Runtime/Scripts/Master/`

完了条件:
- [x] 確認済み。`>` 比較は検出されなかった。

---

### F1-3: `ViewData` サフィックスの命名は正しく適用されている

重大度: 情報

問題: `implementation-quality-guidelines.md` §5「View 向けの DTO には `ViewData` サフィックスを付ける」について、M9 追加の ViewData クラスを確認した。

原因: `GuildManagementWindowViewData`、`DungeonInfoWindowViewData`、`DungeonLayerListItemViewData`、`MarketWindowViewData`、`MarketOfferViewData`、`MarketOfferRequirementViewData` はすべて `ViewData` サフィックスを使用している。

解決案: 対応不要。

根拠となるファイルリスト:
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/`

完了条件:
- [x] 確認済み。ViewData 命名規則に違反なし。

---

## フェーズ 2: Lighthouse パターン

### F2-1: ScreenStack の開閉は Lighthouse 経由で正しく実装されている

重大度: 情報

問題: Lighthouse ScreenStack パターン (P9) の適用確認。

原因: `GameHudWindowOpenService` は `IScreenStackModule.Open(...)` を通してウィンドウを開いており、`screenStackModule.Open(new DungeonInfoWindowData(...)).Forget()` / `GuildManagementWindowData` / `MarketWindowData` の 3 パターンとも Lighthouse パターンに準拠している。

解決案: 対応不要。

根拠となるファイルリスト:
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/ScreenStack/GameHudWindowOpenService.cs`

完了条件:
- [x] 確認済み。ScreenStack 開閉は Lighthouse IScreenStackModule 経由。

---

### F2-2: LHButton を使用している

重大度: 情報

問題: Lighthouse パターン P9「Button は LHButton を使用する」の確認。

原因: `MarketWindow.CreateOfferButton` では `typeof(LHButton)` を使用しており、`button.onClick.AddListener(...)` を通して操作している。`UnityEngine.UI.Button` の直接使用は見つからなかった。

解決案: 対応不要。

根拠となるファイルリスト:
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/MarketWindow/MarketWindow.cs`

完了条件:
- [x] 確認済み。LHButton を使用している。

---

### F2-3: Resources.Load は使用されていない

重大度: 情報

問題: Lighthouse ハードゲート「Resources.Load 禁止」の確認。

原因: M9 追加ファイルに Resources.Load の使用は見つからなかった。

解決案: 対応不要。

根拠となるファイルリスト:
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Economy/`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/`

完了条件:
- [x] 確認済み。Resources.Load の使用なし。

---

## フェーズ 3: Domain 設計

### F3-1: Domain エンティティへの書き込みは UseCase 経由で行われているが、読み取り専用インターフェース越しのアクセスを意図的に迂回している

重大度: 高

問題: `UpgradeFacilityUseCase.Execute` は `worldState.Guild.GetFacility(facilityId)` を通して `Facility` エンティティを取得し、`facility.UpgradeTo(...)` を直接呼び出している。`IGameWorldStateReader.Guild` が `AdventurerGuild` 集約オブジェクトそのものを返すため、`IGameWorldStateReader` として注入していても実態として Domain エンティティの変異メソッドを呼び出せてしまう。

原因: `IGameWorldStateReader` の設計では `Guild` プロパティが `AdventurerGuild` (mutable Domain Entity) を返しており、Reader インターフェース越しでも write が可能な状態になっている。`FulfillMarketOfferUseCase` も `((IExchangeParticipant)worldState.Guild).AddRange(offer.Rewards)` でキャストして write している。これは Reader/Writer 境界を宣言レベルで分離していても意味をなさない。

解決案: 短期対応として両 UseCase が `IGameWorldStateWriter` を追加注入し、write の意図を明示する。中長期対応として `IGameWorldStateReader.Guild` が `IAdventurerGuildReader` など読み取り専用インターフェースを返すよう Domain を整備する。少なくとも UseCase コンストラクタの注入型を `IGameWorldStateWriter` に変更することで「書き込む意図」を DI の型レベルで表明できる。

根拠となるファイルリスト:
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Economy/UpgradeFacilityUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Economy/FulfillMarketOfferUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/IGameWorldState.cs`
- `docs/guidelines/application-boundary-guidelines.md`

完了条件:
- [ ] `UpgradeFacilityUseCase` が `IGameWorldStateWriter` を注入し、write 操作を `IGameWorldStateWriter` 経由で呼ぶか、write 意図を型で表明している
- [ ] `FulfillMarketOfferUseCase` が同様に `IGameWorldStateWriter` を注入する
- [ ] あるいは `IGameWorldStateReader.Guild` が不変読み取り型を返す設計変更が承認される

---

### F3-2: Domain エンティティに UI 向け文字列は存在しない

重大度: 情報

問題: `domain-design-guidelines.md` §4「Domain エンティティに UI 向け文字列を持たせない」の確認。

原因: M9 追加のすべての表示文字列は ViewData の Factory (`GameHudScreenStackViewDataFactory`) が生成しており、Domain エンティティには含まれていない。

解決案: 対応不要。

根拠となるファイルリスト:
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/ScreenStack/GameHudScreenStackViewDataFactory.cs`

完了条件:
- [x] 確認済み。Domain エンティティへの UI 文字列混入なし。

---

### F3-3: Master / Spec / Params 命名は正しく使われている

重大度: 情報

問題: `domain-design-guidelines.md` の Master / Spec 命名規約確認。

原因: `FacilityUpgradeMaster`、`MarketOfferMaster` はいずれも変更不可のマスターデータクラスとして正しく `Master` サフィックスを使用している。コンストラクタで検証を行い、`IReadOnlyList<ItemStack>` を公開している。

解決案: 対応不要。

根拠となるファイルリスト:
- `Client/Assets/DungeonInn/Runtime/Scripts/Master/FacilityUpgradeMaster.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Master/MarketOfferMaster.cs`

完了条件:
- [x] 確認済み。Master 命名規約に準拠。

---

## フェーズ 4: Application 境界

### F4-1: View 層クラスが Application UseCase を直接注入している

重大度: 高

問題: `GameHudWindowOpenService` は `View.Scene.ModuleScene.GameHUD.ScreenStack` 名前空間のクラスであり、`FulfillMarketOfferUseCase`（Application 層）を `[Inject]` コンストラクタで直接注入している。View 層から Application UseCase への直接依存は `application-boundary-guidelines.md` §7「View は UseCase を直接注入しない」に違反する。

`MarketWindowData` に `Action<int> FulfillOffer` デリゲートとして渡されるため、実行は間接的に見えるが、デリゲートの実体は `GameHudWindowOpenService.FulfillMarketOffer` メソッドであり、その内部で `fulfillMarketOfferUseCase.Execute(offerId)` を直接呼び出している。注入箇所が View 層クラスである点は変わらない。

原因: `UpgradeFacilityUseCase` が `GuildManagementWindow` 側のボタン導線と接続されていないため、`FulfillMarketOfferUseCase` だけを View 側 OpenService に注入する形になった。本来は Presenter / UseCase を仲介する層か、コールバック登録先を Application 層に留める設計が必要。

解決案: `IGameHudWindowOpenService` のインターフェース定義を Application 層に移動し、View が参照する側のインターフェースを抽象化する。あるいは `FulfillMarketOfferUseCase` の呼び出しを Application 層内の Service/Coordinator に移し、View は Application の Service インターフェースのみに依存する構造にする。ユーザーへの確認なしには設計変更の方向を確定できないため、本件はレビュー指摘として報告する。

根拠となるファイルリスト:
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/ScreenStack/GameHudWindowOpenService.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Economy/FulfillMarketOfferUseCase.cs`
- `docs/guidelines/application-boundary-guidelines.md`

完了条件:
- [ ] View 層クラスが Application UseCase を直接注入しない構造になっている
- [ ] `FulfillMarketOfferUseCase` の呼び出しが Application 層内に留まっている
- [ ] または、ユーザーの判断でこの設計を許容する旨が明文化されている

---

### F4-2: UseCase が UseCase を呼んでいない

重大度: 情報

問題: `application-boundary-guidelines.md` §6「UseCase は UseCase を呼ばない」の確認。

原因: M9 追加の `UpgradeFacilityUseCase`、`FulfillMarketOfferUseCase`、`GetGuildManagementStatusUseCase`、`GetMarketOffersUseCase` はすべて他 UseCase を呼び出していない。`GetGuildManagementStatusUseCase` は Service (`GuildCombinedInventoryViewService` 等) を注入しており、UseCase → Service 依存は許容。

解決案: 対応不要。

根拠となるファイルリスト:
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Economy/`

完了条件:
- [x] 確認済み。UseCase 間呼び出しなし。

---

### F4-3: IEventPublisher / IEventSubscriber の分離は正しく行われている

重大度: 情報

問題: `application-boundary-guidelines.md` §8 の Publisher/Subscriber 分離確認。

原因: `UpgradeFacilityUseCase`、`FulfillMarketOfferUseCase` はいずれも `IEventPublisher` を注入しており、`IEventSubscriber` を注入していない。Subscriber はゲームループ側でのみ使用されている。

解決案: 対応不要。

根拠となるファイルリスト:
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Economy/UpgradeFacilityUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Economy/FulfillMarketOfferUseCase.cs`

完了条件:
- [x] 確認済み。IEventPublisher のみ注入、IEventSubscriber は混入なし。

---

### F4-4: UseCase の IDisposable 実装なし

重大度: 情報

問題: `application-boundary-guidelines.md` §5「UseCase は IDisposable を実装しない（ステートレス）」の確認。

原因: M9 追加の全 UseCase に `IDisposable` の実装は存在しない。`sealed` クラスで状態を持たない構造は正しい。

解決案: 対応不要。

根拠となるファイルリスト:
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Economy/`

完了条件:
- [x] 確認済み。UseCase に IDisposable なし。

---

## フェーズ 5: 実装品質

### F5-1: Task 8（新米冒険者の初期装備付与削除）が未実装のまま「完了」と報告されている

重大度: 高 (マイルストーン完了条件違反)

問題: `docs/roadmap/milestone9-roadmap.md` の Task 8 完了条件は「新米冒険者が Spawn 時に装備を直接受け取らない」だが、現行コードでは以下の 3 か所が未変更のまま残っている。

1. `ActorArchetypeMaster.InitialInventoryItemIds` プロパティが存在し、冒険者 Archetype (id=1) に `new[] { new ItemStack(Money, 100), new ItemStack(2001, 1) }` が設定されている
2. `ActorFactory.Create` 内で `actor.GainItems(archetypeMaster.InitialInventoryItemIds)` が呼ばれている
3. `HardcodedMasterRepository.ValidateReferences` で `ValidateItemStacks(archetypeMaster.InitialInventoryItemIds)` が呼ばれている

Codex の実装ログには「ActorArchetypeMaster.InitialEquipmentItemIds を削除した」と記載されているが、フィールド名が `InitialInventoryItemIds` のまま存在しており、ActorFactory も依然これを使用している。

原因: Codex が別の名前 (`InitialEquipmentItemIds`) を想定して作業した可能性がある。あるいは変更が中途半端で、片方のファイルしか修正されなかった可能性がある。

解決案:
1. `ActorArchetypeMaster` から `InitialInventoryItemIds` プロパティを削除する
2. `ActorArchetypeMaster` コンストラクタから `initialInventoryItemIds` パラメータを削除する
3. `ActorFactory.Create` から `actor.GainItems(...)` の呼び出し行を削除する
4. `HardcodedMasterRepository` の冒険者 archetype 定義から `new[] { ... }` の初期アイテム引数を `Array.Empty<ItemStack>()` に変更する
5. `ValidateReferences` から `ValidateItemStacks(archetypeMaster.InitialInventoryItemIds)` 呼び出しを削除する

根拠となるファイルリスト:
- `Client/Assets/DungeonInn/Runtime/Scripts/Master/ActorArchetypeMaster.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Spawn/ActorFactory.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Master/HardcodedMasterRepository.cs`
- `docs/roadmap/milestone9-roadmap.md` (Task 8 完了条件)

完了条件:
- [ ] `ActorArchetypeMaster` に `InitialInventoryItemIds` プロパティが存在しない
- [ ] `ActorFactory.Create` が `GainItems` を呼ばない
- [ ] `HardcodedMasterRepository` の冒険者 archetype 定義に初期アイテムが渡されていない
- [ ] コンパイルが成功する
- [ ] EditMode テストが全 pass する

---

### F5-2: `HardcodedMasterRepository.TryGetFacilityUpgradeMaster` が O(n) 線形探索を使用している

重大度: 低

問題: `TryGetFacilityUpgradeMaster` の実装が `facilityUpgradeMasters.Values.FirstOrDefault(x => x.FacilityType == facilityType && x.FromLevel == fromLevel)` という O(n) 線形探索になっている。他の Master 取得メソッドはキー直引き (O(1)) を使用しており、一貫性がない。

原因: `FacilityUpgradeMaster` は複合キー（FacilityType + FromLevel）でアクセスされるため、単純な `int` キーの Dictionary では検索できない。M9 実装時に複合キー Dictionary は作成されず、`Values.FirstOrDefault` で代替された。

解決案: Dictionary のキーを `(FacilityType, int)` タプルに変更する。`CreateFacilityUpgradeMasters()` 内で `.ToDictionary(x => (x.FacilityType, x.FromLevel))` としてインデックスを構築し、`TryGetFacilityUpgradeMaster` では `facilityUpgradeMasters.TryGetValue((facilityType, fromLevel), out master)` で O(1) 検索にする。現在のマスター件数はわずかなため実害は小さいが、将来的に件数が増えた場合の影響を防ぐために対応する。

根拠となるファイルリスト:
- `Client/Assets/DungeonInn/Runtime/Scripts/Master/HardcodedMasterRepository.cs`
- `docs/guidelines/implementation-quality-guidelines.md`

完了条件:
- [ ] `TryGetFacilityUpgradeMaster` が O(1) で動作する
- [ ] 複合キーによる Dictionary が `CreateFacilityUpgradeMasters` 内で構築されている

---

### F5-3: `GetMarketOffersUseCase` が `RequiredGuildTotalLevel` を無視している

重大度: 中

問題: `MarketOfferMaster` には `RequiredGuildTotalLevel` が定義されており、ロードマップは「解放済み候補から 3 件を表示する」と規定している。`GetMarketOffersUseCase.Execute()` は `DisplayPriority` 順に先頭 3 件を取るだけであり、解放条件を一切確認しない。現在の hardcoded データはすべて `RequiredGuildTotalLevel = 0` のため実害は出ていないが、`RequiredGuildTotalLevel` のコントラクトを満たしていない。

原因: `GetMarketOffersUseCase` が `IGameWorldStateReader` を注入しておらず、現在のギルド施設レベル合計を計算できない。マスターデータに解放条件フィールドがある一方で、フィルタリングロジックが実装されなかった。

解決案: `GetMarketOffersUseCase` に `IGameWorldStateReader` を追加注入し、`worldState.Guild.Facilities` から施設レベル合計を計算する補助メソッドを追加する。`.Where(x => x.RequiredGuildTotalLevel <= totalFacilityLevel)` でフィルタしてから `OrderBy(DisplayPriority).Take(3)` する。`FulfillMarketOfferUseCase.Execute` でも同じ条件で再チェックする。

根拠となるファイルリスト:
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Economy/GetMarketOffersUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Master/MarketOfferMaster.cs`
- `docs/roadmap/milestone9-roadmap.md`

完了条件:
- [ ] `GetMarketOffersUseCase` が `RequiredGuildTotalLevel` でフィルタする
- [ ] `FulfillMarketOfferUseCase` が実行時に解放条件を再確認する
- [ ] 未解放 offer が Market window に表示されないことを EditMode テストで確認する

---

### F5-4: `GuildCombinedInventoryViewService.HasAll` / `GetSnapshot` が毎回 Collection を生成している

重大度: 低

問題: `GuildCombinedInventoryViewService.HasAll(costs)` は呼び出しごとに `Dictionary<int, int>` を `new` して集計し、`GetSnapshot()` も `new List<>` を生成する。これらは UseCase が Window を開くタイミングで呼ばれるため毎フレーム問題ではないが、`HasAll` は `UpgradeFacilityUseCase` と `FulfillMarketOfferUseCase` でそれぞれ 1 度ずつ呼ばれており、Window 操作ごとに Dictionary アロケーションが発生する。

原因: Snapshot という設計の性質上、スナップショット取得のたびに生成するのは意図的な設計といえる。ただし `HasAll` の Dictionary 生成は毎回行う必要はなく、`GetSnapshot()` の結果から判定することで削減できる。

解決案: 緊急度は低い。将来的に呼び出し頻度が増える場合は `HasAll` 内のカウント集計を Dictionary ではなく直接 LINQ で代替するか、Window 表示中にキャッシュを持つ設計を検討する。現状は許容範囲。

根拠となるファイルリスト:
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Economy/GuildCombinedInventoryViewService.cs`

完了条件:
- [ ] (オプション) 将来呼び出し頻度が増える前に再設計を検討するタスクをバックログに記録する

---

## フェーズ 6: 統合レビュー

### F6-1: Task 5 完了条件「アップグレードの UI 導線」が未達

重大度: 高 (ロードマップ完了条件違反)

問題: ロードマップ Task 5 完了条件「合算 inventory 上で素材が足りる場合にアップグレードできる」について、`UpgradeFacilityUseCase` は Application 層に実装済みだが、`GuildManagementWindow` からアップグレードボタンを押して実行する導線がない。`GuildManagementWindowData` は `ViewData` のみを持ち、`MarketWindowData` が持つような `Action<Guid>` / reload デリゲートがない。

Codex のセルフレビューでもこの点を Blocker として指摘しているが、milestone9-completion-review-1-codex.md では修正されていない。

原因: Task 5 の Application 実装と Task 4 の View 実装が接続されておらず、実装完了の報告が不正確。Market 側は `MarketWindowData` にコールバックを持たせて接続できているが、Guild Management 側は同様のパターンが適用されなかった。

解決案:
1. `GuildManagementWindowData` に施設アップグレード用コールバック `Action<Guid> upgradeFacility` と再描画用 `Func<GuildManagementWindowViewData> reload` を追加する
2. `GameHudWindowOpenService.OpenGuildManagement` で `UpgradeFacilityUseCase` を注入し、コールバックを渡す
3. `GuildManagementWindow` が施設ごとのアップグレードボタンを生成し、クリック時に `screenStackData.UpgradeFacility(facilityId)` を呼ぶ
4. ただし F4-1 で指摘した View 層 → UseCase 直接依存の問題も同時に解決すること

根拠となるファイルリスト:
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/GuildManagementWindow/GuildManagementWindow.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/GuildManagementWindow/GuildManagementWindowData.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/ScreenStack/GameHudWindowOpenService.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Economy/UpgradeFacilityUseCase.cs`
- `docs/roadmap/milestone9-roadmap.md` (Task 5 完了条件)

完了条件:
- [ ] Guild Management window から施設アップグレードボタンが操作できる
- [ ] ボタン押下後に `UpgradeFacilityUseCase.Execute` が呼ばれる
- [ ] アップグレード後に window が再描画される（施設レベル、消費素材、取引履歴が更新される）

---

### F6-2: Codex 実装ログと実際のコードの乖離

重大度: 高 (プロセス問題)

問題: Codex の実装ログ（milestone9-roadmap.md 内）は Task 8 について「完了」と記録しているが、実際のコードは Task 8 の変更が全く反映されていない。`ActorArchetypeMaster.InitialInventoryItemIds`、`ActorFactory.GainItems` 呼び出し、冒険者 archetype の初期アイテム定義がすべて残っている。

これはレビューの信頼性に関わる問題であり、「ログが pass を報告しているから確認不要」とするレビュープロセスを無効化する。

原因: Codex が変更対象ファイルを誤認した、または変更を保存せずに完了報告した可能性がある。

解決案: 今後の Codex 完了報告では、「変更したファイルのパスと変更内容の要約」を作業ログに必ず記載し、Claude Code がそれを実際のファイルと突き合わせて確認する。本件は F5-1 の修正作業で解消する。

根拠となるファイルリスト:
- `Client/Assets/DungeonInn/Runtime/Scripts/Master/ActorArchetypeMaster.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Spawn/ActorFactory.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Master/HardcodedMasterRepository.cs`
- `docs/roadmap/milestone9-roadmap.md` (Codex implementation log)

完了条件:
- [ ] F5-1 が修正されている
- [ ] 今後の Codex 作業ログに変更ファイルリストを記載するルールを確認する

---

## 優先度別まとめ

### 必須（ NO GO 判定に直結）

| ID | 問題 | 該当ファイル |
|---|---|---|
| F5-1 | Task 8 未実装：`ActorFactory` が依然 `InitialInventoryItemIds` を使用 | `ActorFactory.cs`, `ActorArchetypeMaster.cs`, `HardcodedMasterRepository.cs` |
| F6-1 | Task 5 未達：Guild Management から `UpgradeFacilityUseCase` への UI 導線なし | `GuildManagementWindowData.cs`, `GuildManagementWindow.cs` |

### 高（Milestone 完了前に対応推奨）

| ID | 問題 | 該当ファイル |
|---|---|---|
| F3-1 / F4-1 | `IGameWorldStateReader` 越しの write、View 層の UseCase 直接注入 | `UpgradeFacilityUseCase.cs`, `FulfillMarketOfferUseCase.cs`, `GameHudWindowOpenService.cs` |
| F5-3 | `GetMarketOffersUseCase` が `RequiredGuildTotalLevel` フィルタを未実装 | `GetMarketOffersUseCase.cs` |

### 中（次マイルストーンまでに対応）

| ID | 問題 | 該当ファイル |
|---|---|---|
| F5-2 | `TryGetFacilityUpgradeMaster` が O(n) 線形探索 | `HardcodedMasterRepository.cs` |

### 低（バックログ記録）

| ID | 問題 | 該当ファイル |
|---|---|---|
| F5-4 | `GuildCombinedInventoryViewService` の毎回 Collection 生成 | `GuildCombinedInventoryViewService.cs` |

---

## 確認チェックリスト

- [x] `docs/guidelines/` 配下の guideline 本文を確認した
- [x] 各 guideline のハードゲートに違反していないことを確認した（F3-1, F4-1 で違反を検出）
- [x] 各 guideline の完了前チェックリストを確認した
- [x] ハードゲートだけでなく、本文の設計方針・判断基準に反していないことを確認した
- [ ] コンパイルが通っている（Codex ログで確認済みだが Task 8 修正後に再確認が必要）
- [ ] Play 30 秒確認 — Codex ログでは確認済みだが、F5-1 修正後に再確認が必要

---

## Codex へのアクションリスト

以下を完了させてから「Milestone 9 完了」と報告すること。

1. **F5-1 の修正**: `ActorArchetypeMaster.InitialInventoryItemIds` 削除、`ActorFactory` の `GainItems` 呼び出し削除、`HardcodedMasterRepository` の冒険者 archetype 初期アイテム引数を `Array.Empty<ItemStack>()` へ変更、`ValidateReferences` の `ValidateItemStacks` 呼び出し削除
2. **F6-1 の修正**: `GuildManagementWindowData` にアップグレードコールバックと reload コールバックを追加し、`GuildManagementWindow` にアップグレードボタンを実装する（F4-1 の設計方針はユーザー確認後に適用）
3. **F5-3 の修正**: `GetMarketOffersUseCase` に `RequiredGuildTotalLevel` フィルタを追加する
4. **F4-1 についてはユーザーに設計方針を確認してから対応する**（View 層からの UseCase 直接注入を許容するか、Application 層 Service インターフェース経由に変更するか）
5. 上記 1〜3 完了後に `uloop.cmd compile` → `uloop.cmd run-tests` → Play 30 秒確認を再実行し、結果をロードマップの作業ログに追記する
