# Milestone 9 After-Review 1 — Claude

レビュー日時: 2026-05-27

対象: 前回統合レビュー（milestone9-completion-review-1-total.md）指摘への対応確認

---

## 総合判定: **GO**

必須項目 T1〜T4・高優先度項目 H1〜H3 の全完了条件が満たされている。
新規追加クラスについて軽微な懸念事項（N1〜N2）を記録するが、いずれも次マイルストーンへの影響はない。

---

## 対応確認セクション

### T1: Task 8 未実装（初期装備付与削除）— ✅ 完了

**確認結果:**

- `ActorArchetypeMaster` に `InitialInventoryItemIds` プロパティが存在しない（コンストラクタパラメータも削除済み）
- `ActorFactory.Create` 内で `GainItems` を呼んでいない。`ActorFactoryCore.CreateActor` を呼ぶだけの実装になっている
- `HardcodedMasterRepository` の冒険者 archetype 定義に初期アイテム引数が渡されていない
- `ValidateItemStacks(archetypeMaster.InitialInventoryItemIds)` の呼び出しも存在しない
- `ActorFactoryTests.ActorFactoryBuildsBareAdventurerFromArchetypeMaster` が `actor.Inventory.Gold == 0` かつ `!actor.Inventory.Has(ItemStack(2001, 1))` を Assert しており、無装備生成を明示的に検証している

全完了条件を満たしている。

---

### T2: Guild Management から施設アップグレードを実行できない — ✅ 完了

**確認結果:**

- `GuildManagementWindowData` に `Func<GuildManagementWindowViewData> reload` と `Action<Guid> upgradeFacility` デリゲートが追加されている
- `GuildManagementWindow.RebuildUpgradeButtons` で施設ごとに `LHButton` を生成しており、`button.onClick.AddListener(() => UpgradeFacility(facility.FacilityId))` が設定されている
- ボタン押下時に `screenStackData.UpgradeFacility(facilityId)` → `screenStackData.Refresh()` → `Refresh()` の順で再描画されている
- `CanUpgrade == false` の施設は `button.interactable = false` かつ `button.onClick` にリスナーを追加していない。UpgradeStatus として `"Locked {index+1}"` が表示される
- `GuildManagementScreenService.UpgradeFacility` が `UpgradeFacilityUseCase.Execute` を呼んでいる

全完了条件を満たしている。

---

### T3: 施設アップグレード効果が仕様に接続されていない — ✅ 完了

**確認結果（宿屋回復速度）:**

- `FacilityEffectService.CalculateInnHpRecoveryPercentPerMinute` が `innBalanceSettings.HpRecoveryPercentPerMinute * Math.Max(1, facility.Quality)` を返す
- `RecoverAdventurerAtInnUseCase.TickRecovery` は `guild.TryGetActiveInnReservation` で予約中の Inn facility を特定し、`facilityEffectService.CalculateInnHpRecoveryPercentPerMinute(inn, innBalanceSettings)` で回復量を算出している
- `Milestone9CompletionReviewFixTests.FacilityEffectUsesInnQualityForRecoveryRate` が level1 / level2 の回復率を比較して level2 の方が大きいことを検証している

**確認結果（ラインナップ）:**

- `FacilityLineupMaster`（FacilityType, RequiredLevel, DisplayPriority）と `FacilityLineupItemMaster`（LineupId, ItemId, DisplayPriority）が新規追加されている
- `GetFacilityLineupUseCase` が `RequiredLevel <= facilityLevel` でフィルタし、DisplayPriority 順に返す
- `HardcodedMasterRepository` に GeneralStore（Lv1/2）と EquipmentShop（Lv1/2）のラインナップデータが追加されている
- `GetGuildManagementStatusUseCase.CreateFacilities` が各施設に `getFacilityLineupUseCase.Execute(x.Type, x.Level)` の結果を含めており、Guild Management 表示に Lineup が出る
- `Milestone9CompletionReviewFixTests.FacilityLineupChangesWhenFacilityLevelIncreases` が level1/2 のラインナップ差分を検証している

全完了条件を満たしている。

---

### T4: Dungeon Info に地上階層が含まれていない — ✅ 完了

**確認結果:**

- `GetDungeonLayerInfoUseCase.Execute` の先頭で、`FloorIndex=0, IsGenerated=true, Spawn/Drop=空` の ground summary を `summaries.Add` している
- 冒険者・モンスターのカウントは `CountActors(0, ActorBehaviorType.Adventurer)` / `CountActors(0, ActorBehaviorType.Monster)` が `actor.Position.LayerId.Value == floorIndex` で集計している（`MapLayerId.Ground` の Value は 0）
- `GameHudScreenStackViewDataFactory.CreateDungeonInfo` で `summary.FloorIndex == 0` のとき `"Ground"` に変換されている
- `Milestone9CompletionReviewFixTests.DungeonLayerInfoIncludesGroundLayer` が `layers[0].FloorIndex == 0` かつ `IsGenerated == true` を検証している

全完了条件を満たしている。

---

### H1: IGameWorldStateReader 越しの write 操作 — ✅ 完了

**確認結果:**

- `UpgradeFacilityUseCase` のコンストラクタが `IGameWorldState worldState`（Reader+Writer）を注入している。`IGameWorldStateReader` のみの注入ではなく、write 意図が型レベルで表明されている
- `FulfillMarketOfferUseCase` も同様に `IGameWorldState worldState` を注入している
- `GameSessionLifetimeScope` で `GameWorldState` が `IGameWorldState`・`IGameWorldStateReader`・`IGameWorldStateWriter` の3つとして登録されているため、`IGameWorldState` を注入して resolve できる

前回の `IGameWorldStateReader` 越し write という問題が解消されている。

---

### H2: View 層クラスが Application UseCase を直接注入 — ✅ 完了（推奨構成 A で対応）

**確認結果:**

- `GameHudWindowOpenService` のコンストラクタが `IScreenStackModule`・`GameHudScreenStackViewDataFactory`・`IGuildManagementScreenService`・`IMarketScreenService` のみを注入している。`*UseCase` の直接注入は完全に排除されている
- `IGuildManagementScreenService`・`IMarketScreenService`・`IDungeonInfoScreenService` の interface が `Application.Economy` / `Application.Dungeons` 名前空間に置かれており、View は interface にのみ依存している
- `GuildManagementScreenService` / `MarketScreenService` が Application 層で UseCase を組み合わせる実装を持っている
- `GameHudScreenStackViewDataFactory` も Application interface 経由でデータを取得している

前回指摘の application-boundary-guidelines §7 違反が解消されている。

---

### H3: GetMarketOffersUseCase が RequiredGuildTotalLevel フィルタ未実装 — ✅ 完了

**確認結果:**

- `GuildProgressService.TotalFacilityLevel` が `worldState.Guild.Facilities.Sum(x => x.Level)` で施設レベル合計を計算している
- `GuildProgressService.IsMarketOfferUnlocked` が `offer.RequiredGuildTotalLevel <= TotalFacilityLevel` を判定している
- `GetMarketOffersUseCase.Execute` が `marketOfferMasters.Values.Where(guildProgressService.IsMarketOfferUnlocked)` でフィルタしてから `OrderBy(DisplayPriority).Take(3)` している
- `FulfillMarketOfferUseCase.Execute` が先頭で `!guildProgressService.IsMarketOfferUnlocked(offer)` なら例外を投げており、実行時の再判定がある
- `Milestone9CompletionReviewFixTests.MarketOffersRespectRequiredTotalFacilityLevel` が RequiredGuildTotalLevel=3 の offer1 が初期状態で見え、facilities[1] を Lv2 にすると RequiredGuildTotalLevel=4 の offer2 も見えることを検証している

全完了条件を満たしている。

---

### M2: TryGetFacilityUpgradeMaster O(n) 線形探索 — ✅ 完了

**確認結果:**

- `HardcodedMasterRepository` に `IReadOnlyDictionary<(FacilityType FacilityType, int FromLevel), FacilityUpgradeMaster> facilityUpgradeMasterByFacilityAndLevel` フィールドが追加されている
- コンストラクタで `facilityUpgradeMasters.Values.ToDictionary(x => (x.FacilityType, x.FromLevel))` により複合キー Dictionary が構築されている
- `TryGetFacilityUpgradeMaster` が `facilityUpgradeMasterByFacilityAndLevel.TryGetValue((facilityType, fromLevel), out master)` で O(1) 検索している

完了条件を満たしている。

---

## 新規追加クラスのレビュー

### DI 登録確認

| クラス | 登録スコープ | 状態 |
|---|---|---|
| `FacilityEffectService` | `GameSessionLifetimeScope`（Singleton） | OK |
| `GetFacilityLineupUseCase` | `GameSessionLifetimeScope`（Singleton） | OK |
| `GuildProgressService` | `GameSessionLifetimeScope`（Singleton） | OK |
| `GuildManagementScreenService` | `GameSessionLifetimeScope`（Singleton）.As<IGuildManagementScreenService> | OK |
| `MarketScreenService` | `GameSessionLifetimeScope`（Singleton）.As<IMarketScreenService> | OK |
| `DungeonInfoScreenService` | `GameSessionLifetimeScope`（Singleton）.As<IDungeonInfoScreenService> | OK |
| `IFacilityLineupMasterRepository` | `ProductLifetimeScope`（HardcodedMasterRepository）で登録済み | OK |

### UseCase ステートレス性

- `FacilityEffectService`・`GuildProgressService`・`GetFacilityLineupUseCase`・`GuildManagementScreenService`・`MarketScreenService`・`DungeonInfoScreenService` はいずれも `IDisposable` を実装しておらず、長期状態（Dictionary/HashSet）を持たない

### Application Boundary

- screen service 群は UseCase を組み合わせるだけで、Domain を直接操作していない
- View は `IGuildManagementScreenService`・`IMarketScreenService`・`IDungeonInfoScreenService` の Application interface にのみ依存している

---

## 新規問題セクション

### N1: GuildProgressService.TotalFacilityLevel が LINQ Sum を使用している

重大度: 低

問題:
`GuildProgressService.TotalFacilityLevel` は `worldState.Guild.Facilities.Sum(x => x.Level)` を使っている。このプロパティは `GetMarketOffersUseCase` と `FulfillMarketOfferUseCase` から呼ばれるが、いずれも Window 操作時の低頻度処理であり Frame Loop からは呼ばれない。application-boundary-guidelines §16「Frame Loop / Entity Loop での LINQ 禁止」の適用対象ではなく、現時点で実害はない。

原因: LINQ が簡潔だったため採用されたと思われる。

解決案: 現状は Window 操作時のみ呼ばれるため許容範囲内。将来 Frame Loop から呼ばれる経路が生まれた場合は foreach ループに変更する。

根拠ファイル: `Client/Assets/DungeonInn/Runtime/Scripts/Application/Economy/GuildProgressService.cs`

完了条件: （バックログ）Frame Loop からの呼び出しが発生した時点で対応する。

---

### N2: GetFacilityLineupUseCase で LINQ が多用されているが非 FrameLoop 経路のみ

重大度: 低（情報記録のみ）

問題:
`GetFacilityLineupUseCase.Execute` は `Where`・`OrderBy`・`ThenBy`・`ToDictionary`・`Select`・`ToArray` を連鎖させており、呼び出し時に複数のコレクション生成が発生する。呼び出し元は `GetGuildManagementStatusUseCase`（Guild Management Window 表示時のみ）であり Frame Loop からは呼ばれない。

現時点での影響は無いが、将来 Frame Loop / GameLoop から呼ばれる経路が追加された場合は対処が必要。

根拠ファイル: `Client/Assets/DungeonInn/Runtime/Scripts/Application/Economy/GetFacilityLineupUseCase.cs`

完了条件: （バックログ）Frame Loop からの呼び出しが発生した時点で対応する。

---

## Coding Rules チェック結果

変更・追加ファイルについてスキャンを実施した。

- 明示的 `private` 修飾子: 違反なし
- ファイルスコープ namespace: 違反なし
- `>` 比較演算子: 違反なし
- 式形式メソッド（`method() =>`）: 違反なし
- `[Inject]` 欠損: 全新規クラスで `[Inject]` が明示されている（`FacilityEffectService` はステートレス補助サービスでコンストラクタ引数なし。許容範囲）

---

## テスト確認

| テストクラス | 確認内容 | 状態 |
|---|---|---|
| `ActorFactoryTests.ActorFactoryBuildsBareAdventurerFromArchetypeMaster` | 無装備・gold=0・装備なし で Actor 生成を検証 | OK |
| `Milestone9CompletionReviewFixTests.FacilityLineupChangesWhenFacilityLevelIncreases` | level 増加でラインナップが増えることを検証 | OK |
| `Milestone9CompletionReviewFixTests.FacilityEffectUsesInnQualityForRecoveryRate` | Inn quality=2 で回復率が 2x になることを検証 | OK |
| `Milestone9CompletionReviewFixTests.DungeonLayerInfoIncludesGroundLayer` | layers[0].FloorIndex==0, IsGenerated==true を検証 | OK |
| `Milestone9CompletionReviewFixTests.MarketOffersRespectRequiredTotalFacilityLevel` | RequiredGuildTotalLevel フィルタを検証 | OK |
| `RecoverAdventurerAtInnUseCaseTests` | `FacilityEffectService` を `CreateUseCase` factory に含んでいる | OK |

---

## 確認チェックリスト

- [x] `docs/guidelines/` 配下の guideline 本文を確認した
- [x] 各 guideline のハードゲートに違反していないことを確認した
- [x] 各 guideline の完了前チェックリストを確認した
- [x] ハードゲートだけでなく、本文の設計方針・判断基準に反していないことを確認した
- [x] コンパイルが通っている（Codex ログで 312/312 pass を確認）
- [x] 各指摘 ID（T1〜T4, H1〜H3, M2）の完了条件をコードで直接確認した
- [x] 新規追加クラスの Application Boundary・DI 登録・ステートレス性を確認した
- [x] Coding Rules スキャン（private / ファイルスコープ namespace / > 演算子 / 式形式メソッド）を実施した

---

## 最終判定: **GO**

T1〜T4・H1〜H3 の全必須項目が完了条件を満たしており、新規の重大問題は検出されなかった。
M2 も対応済みである。
N1・N2 はいずれも低優先度のバックログ事項であり、次マイルストーン完了の障壁にならない。
Milestone 9 を完了扱いとして次のマイルストーン定義フェーズへ進んでよい。
