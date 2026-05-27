# Milestone 9 Completion Review 1 - Total

レビュー日時: 2026-05-26

対象マイルストーン: Milestone 9 — ギルド経営UI / Market

## 統合レビューの前提

本文書は以下の 2 つの専任レビューを統合したものである。

| レビュー | 主担当軸 | ファイル |
|---|---|---|
| Claude (milestone9-completion-review-1-claude.md) | Coding Rules（先行）/ Lighthouse / Domain Design / Application Boundary / Implementation Quality / 統合 | 上記ファイル参照 |
| Codex (milestone9-completion-review-1-codex.md) | ロードマップ完了条件照合 / guideline 全軸チェック / Play 動作確認 | 上記ファイル参照 |

統合レビューでは重複指摘を 1 項目に統合し、guideline 間評価が割れた場合は優先順位を明示する。

---

## 総合判定: **NO GO**

以下の理由から Milestone 9 を完了扱いにできない。

1. Task 8（新米冒険者の初期装備付与削除）がコードに未反映のまま「完了」と報告されている
2. Task 5 完了条件「合算 inventory 上で素材が足りる場合にアップグレードできる」について、`UpgradeFacilityUseCase` は実装済みだが Guild Management UI から到達する導線がない
3. Task 5 完了条件「宿屋の回復速度増加が反映される」「雑貨屋 / 装備屋の固定ラインナップがレベルに応じて増える」が未実装
4. Task 2 完了条件「地上を含む階層リストが表示される」が未達（地上階層が Dungeon Info window に出ない）
5. Application Boundary Guidelines 違反：`IGameWorldStateReader` 越しの write 操作 / View 層クラスが Application UseCase を直接注入

---

## 必須（マイルストーン完了ブロック）

### T1. Task 8 未実装 — 初期装備付与がコードに残っている

重大度: 高（ロードマップ完了条件違反）

出典: Claude F5-1 / Codex F6-2

問題:

実装ログは Task 8「新米冒険者の初期装備付与削除」を「完了」と記録しているが、以下の 3 か所が未変更のまま残っている。
- `ActorArchetypeMaster.InitialInventoryItemIds` プロパティが存在し、冒険者 archetype (id=1) に `new[] { ItemStack(Money, 100), ItemStack(2001, 1) }` が設定されている
- `ActorFactory.Create` 内で `actor.GainItems(archetypeMaster.InitialInventoryItemIds)` が呼ばれている
- `HardcodedMasterRepository.ValidateReferences` で `ValidateItemStacks(archetypeMaster.InitialInventoryItemIds)` が呼ばれている

Codex の実装ログには「ActorArchetypeMaster.InitialEquipmentItemIds を削除した」とあるが、フィールド名が `InitialInventoryItemIds` のまま存在しており、変更対象のファイルを誤認した可能性がある。

原因:

Codex が別のフィールド名 (`InitialEquipmentItemIds`) を想定して作業した、または変更が中途半端で保存されなかった。実装ログの完了報告が実ファイルと突き合わせられていなかった。

解決案:

1. `ActorArchetypeMaster` から `InitialInventoryItemIds` プロパティとコンストラクタパラメータを削除する
2. `ActorFactory.Create` から `actor.GainItems(...)` の呼び出し行を削除する
3. `HardcodedMasterRepository` の冒険者 archetype 定義から初期アイテム引数を `Array.Empty<ItemStack>()` に変更する
4. `ValidateReferences` から `ValidateItemStacks(archetypeMaster.InitialInventoryItemIds)` 呼び出しを削除する
5. 無装備 Actor が combat power 計算・AI 更新・戦闘進行で例外にならないことを EditMode test で確認する

理想設計・修正方針:

Actor 生成の責務を「Actor の基礎状態を作ること」に戻し、初期所持品・装備支給・購入導線を Factory / Archetype から切り離す。`ActorArchetypeMaster` は能力値、behavior 種別、visual id、初期武器種などの Actor 定義だけを持ち、実行時 inventory へ item を追加する契約を持たない。将来の新米装備は Milestone 9.5 の「装備屋 0 gold 商品購入 AI」で扱い、Spawn 時に直接支給しない。

実装では `ActorFactory` から item 付与処理を削除し、Factory のテストも「Actor が基礎状態で生成される」「初期 inventory が空でも combat / AI が成立する」ことを確認する形へ更新する。初期 gold や potion がゲームデザイン上必要な場合でも、Factory ではなく Spawn UseCase / Rookie Purchase UseCase / Master-driven purchase flow の責務として別タスクで扱う。

配置・依存方向:

- `Master`: `ActorArchetypeMaster` は Actor 定義のみを保持する
- `Application/Actors/Spawn`: Spawn UseCase は Actor 生成と world 登録だけを行う
- `Domain/Actor`: 無装備 / 空 inventory を正規状態として扱う
- `Application/Actors/Equipment` または Milestone 9.5: 装備購入・自動装備導線を担当する

テスト方針:

- `ActorFactoryTests` で冒険者生成時に inventory item が付与されないことを確認する
- 無装備 Actor の combat power / attack calculator / AI / 戦闘進行が例外にならない既存テストを維持または追加する
- `rg "InitialInventoryItemIds|GainItems\\("` で Factory 経由の初期付与が残っていないことを確認する

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Master/ActorArchetypeMaster.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Spawn/ActorFactory.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Master/HardcodedMasterRepository.cs`
- `docs/roadmap/milestone9-roadmap.md` (Task 8 完了条件)

完了条件:

- [ ] `ActorArchetypeMaster` に `InitialInventoryItemIds` プロパティが存在しない
- [ ] `ActorFactory.Create` が `GainItems` を呼ばない
- [ ] `HardcodedMasterRepository` の冒険者 archetype 定義に初期アイテムが渡されていない
- [ ] `uloop compile` が成功する
- [ ] EditMode test が全 pass する

---

### T2. Task 5 未達 — Guild Management から施設アップグレードを実行できない

重大度: 高（ロードマップ完了条件違反）

出典: Claude F6-1 / Codex item 1（同一問題、統合）

問題:

`UpgradeFacilityUseCase` は Application 層に実装済みだが、`GuildManagementWindow` は `GuildManagementWindowViewData` を文字列表示するだけで、施設別のアップグレードボタンや `UpgradeFacilityUseCase.Execute` への導線を持たない。Market 側は `MarketWindowData` に refresh / fulfill callback を持たせて接続できているが、Guild Management 側に同様のパターンが適用されていない。

原因:

Task 5 の Application 実装と Task 4 の View 実装が接続されていない。`GuildManagementWindowData` が `ViewData` のみを持つ構造で止まっている。

解決案:

1. `GuildManagementWindowData` に `Action<Guid> upgradeFacility` と `Func<GuildManagementWindowViewData> reload` デリゲートを追加する
2. `GameHudWindowOpenService.OpenGuildManagement` で `UpgradeFacilityUseCase` を注入しコールバックを渡す（F4-1 の設計方針はユーザー確認後に適用）
3. `GuildManagementWindow` が施設ごとの Upgrade ボタンを生成し、クリック時に `screenStackData.UpgradeFacility(facilityId)` を呼ぶ
4. 実行後に `reload()` で再取得して window を refresh する
5. 実行不可施設は button disabled にし、不可理由を表示する

理想設計・修正方針:

Guild Management window は ScreenStack が所有する View として、表示とユーザー操作の発火だけを担当する。施設アップグレードの判定・実行・refresh 用データ作成は View に置かず、Application 側の画面用 coordinator / command service に寄せる。Market window と同じ「WindowData に表示データと callback を渡す」形は利用できるが、callback の実体は View 層クラスではなく Application 境界の service に閉じるのが理想。

推奨構成:

- `Application/Economy/GuildManagementWindowService` または `GuildManagementScreenService`
  - `CreateViewData()` で `GetGuildManagementStatusUseCase` 等を呼び、ViewData を返す
  - `UpgradeFacility(Guid facilityId)` で `UpgradeFacilityUseCase` を実行し、結果を返す
- `View/Scene/ModuleScene/GameHUD/ScreenStack/GameHudWindowOpenService`
  - ScreenStack を開く責務だけを持つ
  - UseCase ではなく Application 側 service interface を注入する
- `GuildManagementWindowData`
  - `GuildManagementWindowViewData`
  - `Func<GuildManagementWindowViewData> Reload`
  - `Action<Guid> UpgradeFacility`
  - を持つが、Domain / UseCase 具象は持たない

ViewData には施設ごとに `FacilityId`、`CanUpgrade`、`UnavailableReason`、表示用コスト、ボタン活性状態を含める。`GuildManagementWindow` は ViewData に従って固定寸法の行と `LHButton` を生成し、実行後は reload で再描画する。

テスト方針:

- Application service の EditMode test で、upgrade 実行後に level / inventory / transaction / preview が更新されることを確認する
- View は可能なら WindowData callback の呼び出しと refresh を軽量テストする
- uLoop で Guild Management window を開き、Upgrade ボタン押下ログと Error 0 を確認する

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/GuildManagementWindow/GuildManagementWindow.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/GuildManagementWindow/GuildManagementWindowData.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/ScreenStack/GameHudWindowOpenService.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Economy/UpgradeFacilityUseCase.cs`
- `docs/roadmap/milestone9-roadmap.md` (Task 5 完了条件)

完了条件:

- [ ] Guild Management window に施設ごとの Upgrade ボタンがある
- [ ] ボタン押下で `UpgradeFacilityUseCase.Execute(facilityId)` が呼ばれる
- [ ] 実行後に施設レベル・消費済み inventory・取引履歴・preview が refresh される
- [ ] 実行不可施設の不可理由が表示される

---

### T3. Task 5 未達 — 施設アップグレード効果が仕様に接続されていない

重大度: 高（ロードマップ完了条件違反）

出典: Codex item 2

問題:

ロードマップ Task 5 完了条件には「宿屋の回復速度増加が反映される」「雑貨屋 / 装備屋の固定ラインナップがレベルに応じて増える」が含まれる。現行コードでは `Facility.UpgradeTo` が `Level` / `Quality` / `Capacity` を更新するだけで、宿屋の回復速度は `WorldGameSettingsSO` 由来の固定値のまま。また `FacilityLineupMaster` / `FacilityLineupItemMaster` / `GetFacilityLineupUseCase` はいずれも存在しない。

原因:

`FacilityUpgradeMaster` に quality / capacity はあるが、施設効果を実行系が参照する契約が不足している。ラインナップ系はロードマップに想定型として記載されているだけで Master / UseCase / View のいずれにも実装されていない。

解決案:

宿屋回復速度: `RecoverAdventurerAtInnUseCase` が予約中の Inn facility を解決し、facility level / quality または upgrade master 由来の効果値で回復量を計算する。

雑貨屋 / 装備屋ラインナップ: `FacilityLineupMaster` / `FacilityLineupItemMaster` を追加し、`GetFacilityLineupUseCase` で現在ラインナップを解決する。Guild Management の施設表示に現在ラインナップを含める。Milestone 9 では在庫が減らない固定販売ラインナップ。

理想設計・修正方針:

施設アップグレード効果は `Facility.Level` の更新だけで完了させず、「施設状態」と「効果解決」の責務を分ける。`Facility` は現在 level / quality / capacity を保持する Domain Entity とし、回復速度や販売ラインナップの具体値は Master / Application Service が解決する。Domain Entity に UI 表示用文言やラインナップ配列を持たせない。

宿屋回復速度:

- `FacilityUpgradeMaster` に効果値を直接持たせるか、`FacilityEffectMaster` 相当を追加して `FacilityType + Level` から効果を解決する
- `RecoverAdventurerAtInnUseCase` は予約情報から inn facility を特定し、Application service 経由で recovery rate を取得する
- `WorldGameSettingsSO` の固定 recovery rate は base rate として残し、facility 効果は multiplier / additive bonus として合成する
- 予約中 facility が見つからない場合は正常系ではないため、フォールバック生成ではなく失敗状態を明確化する

雑貨屋 / 装備屋ラインナップ:

- `FacilityLineupMaster`: `FacilityType`, `RequiredLevel`, `DisplayPriority` を持つ
- `FacilityLineupItemMaster`: lineup id と `ItemStack` または `ItemId` を持つ
- `GetFacilityLineupUseCase`: 現在 facility level から解放済み item を固定順で返す
- Milestone 9 では販売 inventory とは分離し、ラインナップは「在庫が減らない固定販売候補」として扱う

Guild Management 表示:

`GetGuildManagementStatusUseCase` は施設 summary にラインナップ summary を含めるか、`GetFacilityLineupUseCase` の結果を画面用 service で統合する。View はラインナップを直接 master から読まない。

テスト方針:

- Inn level 1 / level 2 で同じ deltaGameSeconds を流し、level 2 の回復量が大きいことを確認する
- GeneralStore / EquipmentShop の level を上げると解放ラインナップ件数が増えることを確認する
- 生産・購入 AI・在庫減算が Milestone 9 に混入していないことを確認する

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Actors/Lifecycle/RecoverAdventurerAtInnUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Facility/Facility.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Master/FacilityUpgradeMaster.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Master/HardcodedMasterRepository.cs`
- `docs/roadmap/milestone9-roadmap.md` (Task 5 完了条件)

完了条件:

- [ ] Inn upgrade 後の回復速度が upgrade 前より増えることを EditMode test で検証する
- [ ] `FacilityLineupMaster` / `FacilityLineupItemMaster` 相当の固定ラインナップ定義が存在する
- [ ] `GetFacilityLineupUseCase` 相当の Application Query が存在する
- [ ] GeneralStore / EquipmentShop の表示ラインナップが level に応じて増える

---

### T4. Task 2 未達 — Dungeon Info に地上階層が含まれていない

重大度: 中（ロードマップ完了条件違反）

出典: Codex item 3

問題:

Task 2 完了条件は「地上を含む階層リストが表示される」だが、`GetDungeonLayerInfoUseCase.Execute()` は `DungeonFloorExplorationMasters.Values` だけを列挙しており、`HardcodedMasterRepository` の dungeon floor master は floor 1〜3 のみで `MapLayerId.Ground`（0）を含まない。結果として Dungeon Info window に地上が出ない。

原因:

Dungeon floor master をそのまま画面リストの正典として扱ったため、探索 master を持たない地上 layer が漏れている。

解決案:

`GetDungeonLayerInfoUseCase` の先頭に ground summary を明示追加する。地上は spawn / drop なし、または地上用 master があるならそれを参照する。actor count は `MapLayerId.Ground.Value` で集計する。popup は空の場合 `None` と表示する。

理想設計・修正方針:

Dungeon Info の階層リストは「DungeonFloorExplorationMaster の一覧」ではなく、「プレイヤーが確認できる world layer の一覧」として扱う。地上は dungeon floor master を持たない特別 layer なので、Application Query が ground summary を明示的に合成する。Domain / Master 側に地上を無理に dungeon floor として追加しない。

実装方針:

- `GetDungeonLayerInfoUseCase.Execute()` は最初に `MapLayerId.Ground` の summary を追加する
- ground summary は `FloorIndex = 0` または専用 `MapLayerId` を持つ summary へ拡張する
- 表示名は ViewData 変換時に `Ground` / `地上` とする
- Spawn / Drop は空 list を返し、popup は既存の `None` 表示を使う
- actor count は `actor.Position.LayerId.Equals(MapLayerId.Ground)` で集計する

より堅い設計にする場合は、`DungeonLayerInfoSummary` の `FloorIndex` だけで layer を表さず、`MapLayerId LayerId` と `DungeonLayerKind` 相当を追加する。ただし既存 UI 変更範囲が大きくなるため、Milestone 9 修正では ground summary の明示追加を優先する。

テスト方針:

- `GetDungeonLayerInfoUseCase.Execute()` の先頭または結果に ground summary が含まれることを確認する
- 地上にいる冒険者数が ground summary に反映されることを確認する
- Dungeon floor 1〜3 の spawn / drop 表示が既存通り master 由来であることを確認する

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Dungeons/GetDungeonLayerInfoUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Master/HardcodedMasterRepository.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Domain/Map/MapLayerId.cs`
- `docs/roadmap/milestone9-roadmap.md` (Task 2 完了条件)

完了条件:

- [ ] Dungeon Info window に Ground / 地上が表示される
- [ ] 地上の冒険者数が `MapLayerId.Ground` から集計される
- [ ] 地上 popup で spawn / drop が空の場合 `None` と表示される

---

## 高優先度（完了前対応推奨）

### H1. `IGameWorldStateReader` 越しの write 操作

重大度: 高

出典: Claude F3-1

問題:

`UpgradeFacilityUseCase` と `FulfillMarketOfferUseCase` が `IGameWorldStateReader` を注入しながら、`facility.UpgradeTo(...)` / `worldState.Guild.RecordTransaction(...)` / `((IExchangeParticipant)worldState.Guild).AddRange(...)` で Domain エンティティを書き換えている。Reader インターフェース越しでも mutable な集約を取得できるため、Reader/Writer 分離が宣言上のものになっている。

原因:

`IGameWorldStateReader.Guild` が `AdventurerGuild`（mutable Domain Entity）をそのまま返す設計になっており、Reader として注入しても write を防げない。

解決案:

短期: 両 UseCase が `IGameWorldStateWriter` を追加注入し、write 意図を DI の型レベルで表明する。
中長期: `IGameWorldStateReader.Guild` が `IAdventurerGuildReader` など読み取り専用インターフェースを返すよう整備する。

理想設計・修正方針:

短期修正では、状態変更を行う UseCase が reader だけを要求する状態をやめ、write 意図を型で表す。`UpgradeFacilityUseCase` と `FulfillMarketOfferUseCase` は明確に Domain 状態を変更する command use case なので、constructor では `IGameWorldStateWriter` または read/write を含む明示的な command 用 interface を要求する。

ただし、単に `IGameWorldStateWriter` を追加注入しても `IGameWorldStateReader.Guild` が mutable entity を返す問題は残る。中長期の理想は以下。

- Query UseCase は `IGameWorldStateReader` と読み取り専用 projection / summary だけを使う
- Command UseCase は `IGameWorldStateWriter` または対象 aggregate repository / accessor を使う
- `IGameWorldStateReader.Guild` は mutable `AdventurerGuild` ではなく `IAdventurerGuildReader` を返す
- `AdventurerGuild` の write メソッドは command UseCase からのみ到達可能にする

Milestone 9 の修正範囲では、公開 interface の大規模変更は影響が大きいため、まず command UseCase の注入型を writer に変更し、将来の reader projection 分離を `future-refactor-notes` または該当 roadmap に記録するのが現実的。

テスト方針:

- command UseCase が writer 登録で DI 解決できることを compile / Play で確認する
- query UseCase に writer 依存が混ざらないことを constructor grep で確認する
- 長期対応時は reader interface から mutable メソッドへ到達できないことを compile レベルで確認する

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Economy/UpgradeFacilityUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Economy/FulfillMarketOfferUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/World/IGameWorldState.cs`
- `docs/guidelines/application-boundary-guidelines.md`

完了条件:

- [ ] `UpgradeFacilityUseCase` が `IGameWorldStateWriter` を注入し write 意図を型で表明している
- [ ] `FulfillMarketOfferUseCase` が同様に `IGameWorldStateWriter` を注入する
- [ ] または `IGameWorldStateReader.Guild` が読み取り専用型を返す設計変更が承認・適用される

---

### H2. View 層クラスが Application UseCase を直接注入している（ユーザー確認事項）

重大度: 高

出典: Claude F4-1

問題:

`GameHudWindowOpenService`（`View.Scene.ModuleScene.GameHUD.ScreenStack` 名前空間）が `FulfillMarketOfferUseCase`（Application 層）を `[Inject]` コンストラクタで直接注入している。`MarketWindowData` に `Action<int> FulfillOffer` デリゲートとして渡されるため呼び出しは間接的に見えるが、注入箇所が View 層クラスである点に変わりはなく、`application-boundary-guidelines.md` §7「View は UseCase を直接注入しない」に違反する。

T2 の解決（GuildManagement アップグレードボタン実装）で `UpgradeFacilityUseCase` も同様のパターンで View 側に注入されるリスクがある。

原因:

Presenter / Application Service を仲介する層がなく、View 側 OpenService に UseCase を直接注入する形で実装された。

解決案 A（推奨）: `IGameHudWindowOpenService` のインターフェース定義を Application 層に移動し、`FulfillMarketOfferUseCase` / `UpgradeFacilityUseCase` の呼び出しを Application 層内 Service/Coordinator に閉じる。View は Application の Service インターフェースのみに依存する。

解決案 B（許容）: ユーザーが「GameHudWindowOpenService は View と Application の橋渡しレイヤーであり、この直接注入を許容する」と明文化する。その場合は設計ドキュメントに許容理由を記載する。

理想設計・修正方針:

理想は、View 層の `GameHudWindowOpenService` を「ScreenStack を開くだけの View service」に保ち、Application UseCase を直接注入しない構造にすること。Window 用の表示データ作成と command 実行は Application 側の screen / facade service が担い、View はその service interface を呼ぶだけにする。

推奨構成:

- `Application/Economy/IGuildManagementScreenService`
  - `GuildManagementWindowViewData CreateGuildManagementViewData()`
  - `FacilityUpgradeResult UpgradeFacility(Guid facilityId)`
- `Application/Economy/IMarketScreenService`
  - `MarketWindowViewData CreateMarketViewData()`
  - `MarketOfferFulfillmentResult FulfillOffer(int offerId)`
- `View/Scene/ModuleScene/GameHUD/ScreenStack/GameHudWindowOpenService`
  - `IScreenStackModule` と上記 screen service interface を注入する
  - UseCase 具象は注入しない
  - WindowData に reload / command callback を渡す

この形なら View は Application の「画面ユースケース用 facade」に依存し、個別 UseCase の順序や transaction を知らない。Application service 側で `GetMarketOffersUseCase` / `FulfillMarketOfferUseCase` / `GetGuildManagementStatusUseCase` / `UpgradeFacilityUseCase` を組み合わせるため、T2 の Guild upgrade 導線追加でも View に UseCase 依存が増えない。

配置:

- interface と実装は Application 層へ置く
- ViewData 型は現在 View 配下にあるが、Application service が ViewData を返す設計にするなら「画面表示へ直接適用する ViewData を Application が知る」問題が出る。より厳密には Application は `Summary` を返し、View 側 mapper が ViewData に変換する
- Milestone 9 修正では既存構造との整合を優先し、UseCase 直接注入を screen service 注入へ置き換える段階対応が妥当

テスト方針:

- `GameHudWindowOpenService` の constructor に `*UseCase` が存在しないことを grep で確認する
- Market / Guild の command は Application screen service の EditMode test で確認する
- WindowData callback は screen service 経由で実行されることを確認する

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/ModuleScene/GameHUD/ScreenStack/GameHudWindowOpenService.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Economy/FulfillMarketOfferUseCase.cs`
- `docs/guidelines/application-boundary-guidelines.md`

完了条件:

- [ ] View 層クラスが Application UseCase を直接注入しない構造になっている
- [ ] または、ユーザーの判断でこの設計を許容する旨が設計ドキュメントに明文化されている

---

### H3. `GetMarketOffersUseCase` が `RequiredGuildTotalLevel` フィルタを未実装

重大度: 中〜高

出典: Claude F5-3 / Codex item 4（同一問題、統合）

問題:

`MarketOfferMaster` は `RequiredGuildTotalLevel` を持つが、`GetMarketOffersUseCase.Execute()` は `DisplayPriority` 順に先頭 3 件を取るだけで解放条件を見ていない。`FulfillMarketOfferUseCase.Execute()` も実行時に解放済みか再判定しない。現在の hardcoded データはすべて `RequiredGuildTotalLevel = 0` のため実害は出ていないが、マスター設計コントラクトとして定義されたフィールドが無視された状態はデータ追加時に未解放 offer が表示・納品可能になる欠陥となる。

原因:

表示件数の固定 3 件だけが実装され、解放条件の判定ロジックが抜けている。`IGameWorldStateReader` を注入していないため施設レベル合計が計算できない。

解決案:

`GetMarketOffersUseCase` に `IGameWorldStateReader` を追加注入し、`worldState.Guild.Facilities` から施設レベル合計を計算する。`.Where(x => x.RequiredGuildTotalLevel <= totalLevel).OrderBy(DisplayPriority).Take(3)` でフィルタする。`FulfillMarketOfferUseCase.Execute` でも同条件を再判定する。

理想設計・修正方針:

MarketOffer の解放条件は表示と実行で同じルールを使う必要があるため、条件計算を `GetMarketOffersUseCase` と `FulfillMarketOfferUseCase` に重複実装しない。`GuildProgressService` / `GuildTotalLevelService` のような小さな Application Service を用意し、「Milestone 9 では施設レベル合計を guild total level とする」という契約を 1 か所に閉じる。

実装方針:

- `GuildProgressService.GetTotalFacilityLevel()` を追加し、`worldState.Guild.Facilities` の level 合計を返す
- `GetMarketOffersUseCase` は `RequiredGuildTotalLevel <= totalLevel` の offer だけを `DisplayPriority` 順に 3 件返す
- `FulfillMarketOfferUseCase` は offer 再解決後、同じ service で解放条件を再判定する
- 未解放 offer の直接実行は状態を変えず、失敗 result または例外で止める
- 将来 offer 更新周期や日替わり抽選が入る場合は `MarketOfferStateService` に分離する

この修正では Market を Facility にしない。Market は引き続き固定外部取引先 ID として扱い、在庫や gold を Domain Entity として持たせない。

テスト方針:

- required level を満たさない offer が `GetMarketOffersUseCase` の結果に含まれない
- required level を満たした後に表示される
- 未解放 offer ID を `FulfillMarketOfferUseCase` に渡しても inventory / gold / transaction が変わらない

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Economy/GetMarketOffersUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Economy/FulfillMarketOfferUseCase.cs`
- `Client/Assets/DungeonInn/Runtime/Scripts/Master/MarketOfferMaster.cs`
- `docs/roadmap/milestone9-roadmap.md`

完了条件:

- [ ] `GetMarketOffersUseCase` が `RequiredGuildTotalLevel` でフィルタする
- [ ] `FulfillMarketOfferUseCase` が実行時に解放条件を再確認する
- [ ] 未解放 offer が Market window に表示されないことを EditMode test で確認する

---

## 中優先度（次マイルストーンまでに対応）

### M1. Play 30 秒確認（通常ルート）が Codex レビューで未達

重大度: 中

出典: Codex item 5

問題:

Codex レビュー実行時、`uloop control-play-mode --action Play` だけでは Title 止まりとなり `[World] GameWorldState initialized` を確認できなかった。AGENTS.md のハードゲートは「通常ルート（Title → World 遷移）で 30 秒確認し、必須ログを確認すること」を定めている。Codex の実装ログには確認済みとあるが、レビュー担当として現行状態での再現確認が未達。

原因:

Play mode 起動だけでは World へ遷移しない。Title の `startGameButton` を uLoop で確実に操作する手順が整備されていない。

解決案:

T1〜T4 の修正完了後に uLoop UI simulation で Title → World 遷移を経て 30 秒 Play 確認を再実行する。`[World] GameWorldState initialized` と error 0 を確認し、手順をロードマップ作業ログに残す。

理想設計・修正方針:

PlayMode 確認は手作業依存にせず、uLoop dynamic code または UI simulation で再利用できる手順にする。Title scene の `TitleView` から start button を確実に押す、または `TitlePresenter` と同じ public UI 経路を通す。`SceneManager` 直呼びや GameSession 直接生成で World へ飛ばす確認は、通常ルート確認にならないため使わない。

確認手順の理想:

1. `uloop.cmd control-play-mode --project-path Client --action Play`
2. Title scene 初期化完了を短時間待つ
3. `TitleView` が持つ start button を UI 経由で invoke する
4. `[World] GameWorldState initialized` が出るまで待つ
5. 30 秒以上 Play 継続
6. `uloop.cmd control-play-mode --project-path Client --action Stop`
7. `uloop.cmd get-logs --project-path Client`
8. 必須ログあり、Error / Exception なしを確認する

この手順は `docs/roadmap/milestone9-roadmap.md` の作業ログか、検証用メモに残す。今回の修正対象が UI / ScreenStack / DI を含むため、さらに DungeonInfo / GuildManagement / Market の各 window を開くログも確認するのが望ましい。

テスト方針:

- compile / EditMode test 後に Play 確認を実施する
- Warning は既知の TextTable / font warning と区別し、Error 0 を必須とする
- 確認ログに `[World] GameWorldState initialized` と対象 window 操作ログを残す

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/Title/TitleView.cs`
- `AGENTS.md`

完了条件:

- [ ] Title -> World の通常ルートで Play 30 秒確認を再実行する
- [ ] `[World] GameWorldState initialized` がログに出る
- [ ] Error log が 0 件である
- [ ] 確認結果を作業ログに残す

---

### M2. `TryGetFacilityUpgradeMaster` が O(n) 線形探索

重大度: 低〜中

出典: Claude F5-2

問題:

`HardcodedMasterRepository.TryGetFacilityUpgradeMaster` が `facilityUpgradeMasters.Values.FirstOrDefault(x => x.FacilityType == facilityType && x.FromLevel == fromLevel)` という O(n) 線形探索になっている。他の Master 取得メソッドはキー直引き O(1) を使用しており一貫性がない。マスター件数が増えた場合の影響を防ぐために対応する。

原因:

複合キー（FacilityType + FromLevel）の Dictionary が構築されず、`Values.FirstOrDefault` で代替された。

解決案:

`CreateFacilityUpgradeMasters()` 内で `.ToDictionary(x => (x.FacilityType, x.FromLevel))` としてインデックスを構築し、`TryGetFacilityUpgradeMaster` では `TryGetValue((facilityType, fromLevel), out master)` で O(1) 検索にする。

理想設計・修正方針:

Master repository は「正典データ」と「検索用 index」を分けて持つ。外部公開用には既存の `IReadOnlyDictionary<int, FacilityUpgradeMaster>` を維持しつつ、内部に `(FacilityType, FromLevel)` keyed dictionary を追加する。`TryGetFacilityUpgradeMaster` は複合キー index を使い、Values 走査をしない。

実装方針:

- `readonly IReadOnlyDictionary<(FacilityType FacilityType, int FromLevel), FacilityUpgradeMaster> facilityUpgradeMasterByFacilityAndLevel;` を追加する
- constructor で id dictionary と複合キー dictionary の両方を構築する
- 重複キーは master 定義ミスとして初期化時に例外で検出する
- `IFacilityUpgradeMasterRepository` の公開契約は必要がなければ変えない

テスト方針:

- `TryGetFacilityUpgradeMaster(FacilityType.Inn, 1, out master)` が期待 master を返す
- 存在しない level は false を返す
- 重複定義を検出できる構造であることを repository 構築テストまたはコード構造で確認する

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Master/HardcodedMasterRepository.cs`

完了条件:

- [ ] `TryGetFacilityUpgradeMaster` が複合キー Dictionary を使い O(1) で動作する

---

## 低優先度（バックログ記録）

### L1. `GuildCombinedInventoryViewService` の毎回 Collection 生成

重大度: 低

出典: Claude F5-4

問題:

`HasAll(costs)` は呼び出しごとに `Dictionary<int, int>` を `new` して集計する。現状は Window 操作ごとに呼ばれる程度で毎フレーム問題ではないが、将来呼び出し頻度が増えた場合に備えてバックログに記録する。

解決案:

`HasAll` 内のカウント集計を LINQ で代替するか、Window 表示中にキャッシュを持つ設計を検討する。現状は許容範囲。

理想設計・修正方針:

現状は window open / command 実行時の低頻度処理なので、Milestone 9 完了ブロックにはしない。将来、Market offer 更新や施設 preview が頻繁に再計算される場合は、`GuildCombinedInventoryViewService` に snapshot reuse / caller-provided buffer / revision based cache を導入する。

注意点:

- LINQ への置き換えは allocation 削減にならない場合があるため、性能目的なら loop と再利用 buffer を優先する
- Domain に合算 inventory cache を持たせない
- View / Presenter が facility inventory を直接集計しない
- revision / dirty flag を追加する場合は Application service が所有する

将来対応案:

- `Fill(List<GuildCombinedInventoryItemSummary> buffer)` を既に持っているため、Window service 側で buffer を再利用する
- `HasAll(IReadOnlyList<ItemStack> required, Dictionary<int, int> scratch)` のように scratch buffer を呼び出し側から渡す overload を検討する
- 経済状態 revision を導入し、変更がない場合は snapshot を再利用する

バックログ条件:

- Market / Guild window が常時 refresh される
- offer 数や facility 数が増え、preview 計算が UI 操作で重くなる
- PlayMode profiler / GC alloc で問題が観測される

根拠となるファイルリスト:

- `Client/Assets/DungeonInn/Runtime/Scripts/Application/Economy/GuildCombinedInventoryViewService.cs`

完了条件:

- [ ] （オプション）将来呼び出し頻度が増える前に再設計を検討するタスクをバックログに記録する

---

## 優先度別まとめ

### 必須（NO GO 直結）

| ID | 問題 | 該当ファイル |
|---|---|---|
| T1 | Task 8 未実装：InitialInventoryItemIds / GainItems が残存 | `ActorFactory.cs`, `ActorArchetypeMaster.cs`, `HardcodedMasterRepository.cs` |
| T2 | Task 5：Guild Management から UpgradeFacilityUseCase への UI 導線なし | `GuildManagementWindowData.cs`, `GuildManagementWindow.cs` |
| T3 | Task 5：施設アップグレード効果（宿屋回復速度、FacilityLineupMaster）未実装 | `RecoverAdventurerAtInnUseCase.cs`, `HardcodedMasterRepository.cs` |
| T4 | Task 2：Dungeon Info に地上階層が含まれていない | `GetDungeonLayerInfoUseCase.cs` |

### 高（完了前対応推奨）

| ID | 問題 | 該当ファイル |
|---|---|---|
| H1 | IGameWorldStateReader 越しの write 操作 | `UpgradeFacilityUseCase.cs`, `FulfillMarketOfferUseCase.cs` |
| H2 | View 層クラスが Application UseCase を直接注入（ユーザー確認事項） | `GameHudWindowOpenService.cs` |
| H3 | GetMarketOffersUseCase が RequiredGuildTotalLevel フィルタ未実装 | `GetMarketOffersUseCase.cs` |

### 中（次マイルストーンまでに対応）

| ID | 問題 | 該当ファイル |
|---|---|---|
| M1 | Play 30 秒確認（通常ルート）が Codex レビューで未達 | ロードマップ作業ログ |
| M2 | TryGetFacilityUpgradeMaster が O(n) 線形探索 | `HardcodedMasterRepository.cs` |

### 低（バックログ記録）

| ID | 問題 | 該当ファイル |
|---|---|---|
| L1 | GuildCombinedInventoryViewService の毎回 Collection 生成 | `GuildCombinedInventoryViewService.cs` |

---

## 統合判断メモ

- Claude / Codex ともに T2（Guild Management UI 導線なし）と H3（RequiredGuildTotalLevel フィルタ未実装）を独立に検出した。両指摘の内容は一致しており、本文書では 1 項目に統合した。
- T3（施設アップグレード効果）と T4（地上階層）は Codex のみが検出。Claude レビューは実装品質・guideline 違反の軸でレビューしており、ロードマップ完了条件の詳細照合は Codex が補完した。
- H2（View 層の UseCase 直接注入）は Claude のみが検出。設計方針の判断が必要なため「ユーザー確認事項」とし、T2 の解決と連動して対応方針を確定する。
- T1 に関するプロセス問題（実装ログとコードの乖離）は個別 guideline 違反ではなくプロセス上の問題であるため、T1 の完了条件の中に含めた。

---

## 確認チェックリスト

- [x] `docs/guidelines/` 配下の guideline 本文を確認した
- [x] 各 guideline のハードゲートに違反していないことを確認した（H1, H2 で違反を検出）
- [x] 各 guideline の完了前チェックリストを確認した
- [x] ハードゲートだけでなく、本文の設計方針・判断基準に反していないことを確認した
- [x] 専任レビューを使ったこと、担当分担、統合判断の結果を記録した
- [x] T1〜T4 修正後に `uloop compile` が成功する
- [x] T1〜T4 修正後に `uloop run-tests EditMode` が全 pass する
- [x] T1〜T4 修正後に通常ルート Play 30 秒確認で `[World] GameWorldState initialized` を確認し、error 0 を確認する

---

## Codex 対応結果（2026-05-27）

- T1: `ActorArchetypeMaster.InitialInventoryItemIds` と `ActorFactory.Create` の初期 item 付与を削除し、ActorFactoryTests を空 inventory 前提へ更新した。
- T2/H2: `GuildManagementScreenService` / `MarketScreenService` / `DungeonInfoScreenService` を追加し、`GameHudWindowOpenService` から UseCase 直接注入を外した。Guild Management には施設別 Upgrade ボタン、reload callback、実行後 refresh を追加した。
- T3: `FacilityEffectService` で宿屋 Quality による回復速度倍率を反映し、`FacilityLineupMaster` / `FacilityLineupItemMaster` / `GetFacilityLineupUseCase` と Guild Management 表示ラインナップを追加した。
- T4: `GetDungeonLayerInfoUseCase` に Ground summary を追加し、Dungeon Info の表示名を `Ground` に変換するようにした。
- H1: 状態変更を行う `UpgradeFacilityUseCase` / `FulfillMarketOfferUseCase` の依存を `IGameWorldState` に変更し、Reader だけを要求する状態を解消した。
- H3: `GuildProgressService` を追加し、Market offer の表示と納品実行の両方で `RequiredGuildTotalLevel` を判定するようにした。
- M1: Title の StartGame button を uLoop dynamic code で invoke し、通常ルートで World へ遷移して 30 秒 Play 確認を実施した。`[World] GameWorldState initialized. Facilities=3 DungeonFloors=1 Actors=0` を確認し、Error / Exception ログは 0 件だった。
- M2: `TryGetFacilityUpgradeMaster` を `(FacilityType, FromLevel)` 複合キー Dictionary 参照に変更した。
- L1: 低優先度バックログ扱いのため、今回の実装修正対象外として維持した。

検証結果:

- `uloop.cmd compile --project-path Client`: success
- `uloop.cmd run-tests --project-path Client --test-mode EditMode`: 312 / 312 passed
- `uloop.cmd control-play-mode --project-path Client --action Play` → Title StartGame invoke → 30 秒 → Stop → `uloop.cmd get-logs --project-path Client`: 必須ログあり、Error / Exception 0 件

### Codex 追加対応結果（2026-05-27）

- H1 追加対応: `IGameWorldStateWriter` に書き込み用の `WritableGuild` を追加し、`UpgradeFacilityUseCase` / `FulfillMarketOfferUseCase` は `IGameWorldStateWriter` 依存で書き込み対象へ到達する形に変更した。`IGameWorldState` 依存の暫定対応から、完了条件に沿った writer 依存へ修正済み。
- H2 追加対応: `InnStatusPanelPresenter` / `WorldHudPresenter` から UseCase 直接注入を外し、`InnStatusPanelScreenService` / `WorldHudScreenService` 経由に変更した。これにより GameHUD 配下の対象 Presenter / OpenService から UseCase 直接注入を排除した。

追加検証結果:

- `rg` で `View/Scene/ModuleScene/GameHUD` 配下の `readonly .*UseCase` / Presenter constructor UseCase 注入が存在しないことを確認
- `uloop.cmd compile --project-path Client`: success
- `uloop.cmd run-tests --project-path Client --test-mode EditMode`: 312 / 312 passed
- 通常ルート Play 30 秒確認: `[World] GameWorldState initialized. Facilities=3 DungeonFloors=1 Actors=0` を確認し、Error / Exception 0 件
