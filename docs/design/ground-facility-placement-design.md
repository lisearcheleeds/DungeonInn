# Ground 施設配置設計

## 目的

このドキュメントは、地上マップ上の施設建物の配置仕様と、Actor が施設建物の「入口タイル」に入室することで施設を利用できる仕組みを定義する。

## 背景・方針

従来の実装では、施設エンティティ（`Facility`）は位置情報を持たず、Actor は地上でどこにいても施設を利用できた。

変更後は以下の方針とする。

- 各施設は地上マップ上に特定の建物（ブロックタイルの矩形）として存在する。
- 建物は向き（`BuildingFacingDirection`）を持ち、その向きの辺の中央に「入口タイル（`FacilityEntrance`）」が 1 つ設けられる。
- Actor がその入口タイルの入口接触位置まで移動（入室）したときにのみ、施設を利用できる。
- 入室中、ActorView は非表示になる（建物の中にいる状態を表現）。
- Inn が満員の場合、Actor は入口タイルで待機する（`WaitingForInn` 状態）。
- 施設の種類・配置・サイズ・向きはすべて設定で管理し、将来の施設追加・レイアウト変更に対応できる汎用構造とする。
- **入口タイルの位置は `BuildingFacingDirection` から一意に計算され、自由座標での指定は行わない。**

## 理想設計

互換性維持は考慮しない場合、Ground 施設は「マップ上の建物」「Actor の移動目標」「施設利用トランザクション」「View 表現」を分離して設計する。

### 実装時の優先順位

このドキュメント内に残っている `Facility.SetBuildingInfo()`、`Facility.EntrancePosition`、`Facility.IsAtEntrance()`、`ActorViewData.IsInsideFacility`、`Recovering` / `WaitingForInn`、`AdvanceInnRecoveryOrchestrator` への入口チェック追加といった記述は、既存実装からの差分案を説明するための参考である。実装時に理想設計と衝突する場合は、必ず理想設計を優先する。

採用する実装方針:

- `Facility` にマップ配置情報を直接持たせない。
- `FacilityBuilding` と `FacilityInteractionPoint` を施設配置の正とする。
- 到着・施設利用開始・ActorView 非表示判定は `FacilityInteractionPoint` と `ActorTaskState` / `ActorPresence` から導出する。
- AI は施設座標を計算せず、`FacilityNeed` と `GroundFacilityNavigationPlanner` を介する。
- Inn / Shop / Tavern の処理は `FacilityInteractionOrchestrator` と施設別 Handler に集約する。

理想的な責務分担:

- `Facility`: 施設種別、在庫、価格、品質、収容数など施設そのものの状態を持つ Domain Entity。
- `FacilityBuilding`: GroundMap 上の占有タイル、入口タイル、向き、入口接触位置を持つ配置 Value Object。
- `FacilityInteractionPoint`: Actor が到達・接触すべき連続座標と到着半径を持つ Value Object。
- `GroundNavigationService`: `FacilityInteractionPoint` までの NavMesh / A* 経路を解決する。
- `FacilityInteractionOrchestrator`: 入口接触後の施設利用を開始・進行・完了する Application Orchestrator。
- `ActorPresenceService`: Actor が地上に表示されるか、施設内として非表示になるかを決定する。

`Facility` 自体に `BuildingOrigin` や `EntrancePosition` を直接持たせるより、理想的には `FacilityId` で `Facility` と `FacilityBuilding` を紐付ける。施設の経済状態とマップ配置を分けることで、施設移転・建物差し替え・複数入口・仮設店舗などを Domain Entity の責務肥大なしに扱える。

理想的なデータ構造:

```csharp
public sealed class Facility
{
    public Guid Id { get; }
    public FacilityType Type { get; }
    public Inventory Inventory { get; }
    public int BasePrice { get; }
    public int Quality { get; }
    public int Capacity { get; }
}

public readonly struct FacilityBuilding
{
    public Guid FacilityId { get; }
    public GridPosition Origin { get; }
    public int WidthCells { get; }
    public int DepthCells { get; }
    public BuildingFacingDirection Facing { get; }
    public GridPosition EntranceCell { get; }
    public FacilityInteractionPoint InteractionPoint { get; }
}
```

この設計では、施設利用は `FacilityInteractionPoint` への接触で開始され、入口タイル中心・NavMesh 目標・ActorView 非表示判定はすべて同一の InteractionPoint を参照する。

## タイルタイプ

建物に関連するタイルタイプは以下の 2 種類。`GroundCellType` にどちらも定義済み。

| GroundCellType     | MapCellBlockType | 説明 |
|--------------------|-----------------|------|
| `Building`         | `Blocked`       | 建物本体タイル。通行不可。 |
| `FacilityEntrance` | `Walkable`      | 建物の向き面の中央にある入口タイル。Actor が通行・占有できる。 |

`FacilityEntrance` タイルは建物矩形の内側に存在するが、NavMesh では建物メッシュ形状によりタイル中心へ到達できない場合がある。そのため AI の移動目標は入口タイル中心ではなく、入口タイル中心から建物の向きへ半タイル分ずらした入口接触位置を使う。A* fallback では入口タイル自体が Walkable であるため経路探索の対象にできる。

**理想設計**

`FacilityEntrance` タイルは「建物の入口が存在するセル」を表すだけにし、Actor の到着判定には使わない。移動・到着・利用開始は `FacilityInteractionPoint` の連続座標と半径で統一する。

- NavMesh: `FacilityInteractionPoint.Position` を移動目標にする。
- A*: 入口セルまたは入口外側の隣接 walkable セルを経路目標にし、最終的な到着判定は InteractionPoint で行う。
- View: Actor が `FacilityInteractionState.Interacting` になったら非表示にする。タイルタイプを直接見ない。

## 建物の向き（BuildingFacingDirection）

```csharp
public enum BuildingFacingDirection
{
    North,  // +Z 方向
    South,  // -Z 方向
    East,   // +X 方向
    West    // -X 方向
}
```

- `Domain/Facility` に配置する。
- 建物アセットは向きに応じて 90° 単位で回転表示する。
- 入口タイルは向き面の中央（`SizeCells / 2` の位置）に配置する。

### 入口タイルの座標計算

`SizeCells` を `s` とすると：

| FacingDirection | EntranceLocalX | EntranceLocalZ | 説明 |
|-----------------|----------------|----------------|------|
| North (+Z)      | `s / 2`        | `s - 1`        | 北面（+Z 側）の中央 |
| South (-Z)      | `s / 2`        | `0`            | 南面（-Z 側）の中央 |
| East (+X)       | `s - 1`        | `s / 2`        | 東面（+X 側）の中央 |
| West (-X)       | `0`            | `s / 2`        | 西面（-X 側）の中央 |

入口の絶対座標 = `(OriginX + EntranceLocalX, OriginZ + EntranceLocalZ)`

**理想設計**

入口タイルの計算は `FacilityBuildingLayoutCalculator` に閉じ込める。`InitializeWorldMapUseCase`、AI、View、Interaction がそれぞれ入口計算を再実装しない。

```csharp
public sealed class FacilityBuildingLayout
{
    public GridPosition EntranceCell { get; }
    public FacilityInteractionPoint InteractionPoint { get; }
    public IReadOnlyList<GridPosition> OccupiedCells { get; }
    public IReadOnlyList<GridPosition> BlockedCells { get; }
}
```

将来、矩形以外の建物や複数入口に拡張する場合も、呼び出し側は `InteractionPoint` のリストを見るだけでよい。

## 施設建物定義（FacilityBuildingDefinition）

施設ごとの建物と向きを定義する Value Object。`FacilityBuildingSettingsSO` にシリアライズして保持する。

```csharp
[Serializable]
public struct FacilityBuildingDefinition
{
    public FacilityType FacilityType;       // どの施設の建物か
    public int OriginX;                     // 建物左下（南西）のグリッド X 座標
    public int OriginZ;                     // 建物左下（南西）のグリッド Z 座標
    public int SizeCells;                   // 1 辺のセル数（例: 3 → 3x3 の正方形建物）
    public BuildingFacingDirection Facing;  // 入口がある面の向き
}
```

入口タイルの絶対座標は `Facing` と `SizeCells` から上記テーブルで計算する。`EntranceLocalX/Z` の自由指定は行わない。

## 設定（FacilityBuildingSettingsSO）

施設建物の配置設定は `WorldGameSettingsSO` とは独立した専用の ScriptableObject で管理する。
`WorldGameSettingsSO` に建物に関する設定は持たせない。

```csharp
[CreateAssetMenu(menuName = "DungeonInn/Facility/FacilityBuildingSettings")]
public sealed class FacilityBuildingSettingsSO : ScriptableObject, IFacilityBuildingSettingsRepository
{
    [SerializeField] FacilityBuildingDefinition[] definitions;

    public IReadOnlyList<FacilityBuildingDefinition> GetDefinitions() => definitions;
}
```

`IFacilityBuildingSettingsRepository` を介して `InitializeWorldMapUseCase` および `InitializeGameWorldOrchestrator` から参照する。

### デフォルト配置

地上マップ（30x30）における初期デフォルト配置。町壁（wallOffset=3、壁タイルは x=3/26、z=3/26）の内側で、各建物がダンジョン入口（中央 15,15）側を向くように East / West を選択する。

| FacilityType   | OriginX | OriginZ | SizeCells | Facing | 入口座標 | 位置の意味 |
|----------------|---------|---------|-----------|--------|---------|------------|
| Inn            | 5       | 5       | 3         | East   | (7, 6)  | 西側・東面が入口 |
| Tavern         | 22      | 5       | 3         | West   | (22, 6) | 東側・西面が入口 |
| GeneralStore   | 5       | 22      | 3         | East   | (7, 23) | 西側・東面が入口 |
| EquipmentShop  | 22      | 22      | 3         | West   | (22, 23)| 東側・西面が入口 |

低 X 側の建物（Inn、GeneralStore）は East（+X）を向き、高 X 側の建物（Tavern、EquipmentShop）は West（-X）を向く。いずれもダンジョン入口の方向を正面とする。

## 施設エンティティと位置の紐付け（Facility）

`Facility` ドメインクラスに以下を追加する。

```csharp
public GridPosition BuildingOrigin { get; private set; }
public int BuildingSizeCells { get; private set; }
public BuildingFacingDirection FacingDirection { get; private set; }
public GridPosition EntrancePosition { get; private set; }

public bool IsAtEntrance(GridPosition position) => position.Equals(EntrancePosition);

public void SetBuildingInfo(
    GridPosition origin,
    int sizeCells,
    BuildingFacingDirection facing,
    GridPosition entrancePosition)
{
    BuildingOrigin = origin;
    BuildingSizeCells = sizeCells;
    FacingDirection = facing;
    EntrancePosition = entrancePosition;
}
```

- `InitializeGameWorldOrchestrator` が施設生成後に `SetBuildingInfo()` で位置情報を設定する。
- `EntrancePosition` は `FacingDirection` と `SizeCells` から計算した絶対座標を渡す。
- `IsAtEntrance()` は Domain 層での入室判定メソッドとして使用する。

**理想設計**

`Facility` に建物情報を直接持たせない。`Facility` は施設の経済・利用状態、`FacilityBuilding` はマップ配置、`FacilityInteractionPoint` は移動と入室判定の情報を持つ。

```text
FacilityCatalog
  FacilityId -> Facility
  FacilityId -> FacilityBuilding
  FacilityId -> FacilityInteractionDefinition
```

`FacilityCatalog` は Application の読み取りモデルとして扱い、AI と Navigation はこの Catalog を参照する。Domain の `Facility` は `GridPosition` や `LayerPosition` を知らないのが理想。

## 入室の判定

入室判定の根拠は `Facility.EntrancePosition` と `BuildingFacingDirection` に置く。View 層でグリッド位置を直接判定しない。

```
guild.Facilities のいずれかで
FacilityEntranceInteractionService.IsTouchingEntrance(layer, facility, actor.Position) == true → 入室中
```

- 入口タイル中心だけでなく、入口タイル中心から `BuildingFacingDirection` 方向へ `CellSizeMeters * 0.5f` ずらした入口接触位置を判定対象に含める。
- NavMesh で入口タイル中心へ入れない場合でも、入口に触れた時点で施設利用・ActorView 非表示へ進める。

**理想設計**

入室判定は「Actor の座標が入口に近い」だけで決めない。座標接触は Interaction 開始条件の 1 つであり、最終的な入室状態は Actor の TaskState から決める。

```text
MovingToInteractionPoint + InteractionPoint.Contains(actor.Position)
  → FacilityInteractionState.Interacting
  → ActorPresence.HiddenInsideFacility
```

これにより、Actor が偶然入口付近を通過しただけで施設利用や非表示が発生する事故を防げる。

また `GroundMap` に以下のメソッドを追加し、Application 層でセルタイプによる判定も可能にする。

```csharp
// GroundMap に追加
public bool IsAtFacilityEntrance(GridPosition position)
{
    if (!Layer.Contains(position)) return false;
    return GetCell(position).Type == GroundCellType.FacilityEntrance;
}
```

## Actor の入室状態と View の挙動

入室状態の判定は Application 層の施設入口接触判定で行い、結果を `ActorViewData.IsInsideFacility` フラグに反映する。View 層はフラグを参照するだけで判定ロジックを持たない。

- `ActorViewData` に `bool IsInsideFacility` フィールドを追加する。
- `ActorViewDataStore.SyncActor()` 内で `IsInsideFacility` を評価して更新する。
  - 地上レイヤー以外の Actor は常に `false`。
  - 地上レイヤーの Actor は `FacilityEntranceInteractionService.IsTouchingEntrance(layer, facility, actor.Position)` で評価する。

| 状態 | 条件 | IsInsideFacility | ActorView |
|------|------|-----------------|-----------|
| 地上歩行中 | 施設入口接触位置にいない | false | 通常表示 |
| 入室中 | いずれかの施設入口接触位置にいる | true | 非表示 |

## Actor の地上移動と施設利用フロー

地上帰還後の施設利用は **AI（Goal/Plan/Action 体系）が主導する**。`AdvanceActorLifecycleOrchestrator.Recovering` はダンジョン内のルーム・階段移動と同じパターンで `MoveTo` アクションを実行するだけであり、Inn 固有ロジックを持たない。詳細は `docs/design/ground-recovery-ai-correction.md` を参照。

```
[Returning → Ground 到着] → Recovering 状態
    ↓
AdvanceActorAiOrchestrator（AI 評価）
    AdventurerAiPolicy.EvaluateMidTerm()
        HP が低い             → ActorPlanType.Recover    (targetId = inn.FacilityId)
        GeneralStore で売却/ポーション補充が必要 → ActorPlanType.UseFacility (targetId = generalStore.FacilityId)
        EquipmentShop で売却/武器購入が必要      → ActorPlanType.UseFacility (targetId = equipmentShop.FacilityId)
        Tavern を使う明確な理由がある             → ActorPlanType.UseFacility (targetId = tavern.FacilityId)
        何も必要ない                              → ActorPlanType.Prepare
    ↓
    AdventurerAiPolicy.EvaluateShortTerm()
        現在プランに対応する施設の入口接触位置を取得
        → ActorAction.MoveTo(FacilityEntranceNavigationTargetCalculator.Calculate(layer, facility))
    ↓
AdvanceActorLifecycleOrchestrator.Recovering（MoveTo 実行のみ）
    actor.CurrentAction.Type == Move かつ HasTargetPosition
        → MoveActorTowardDestinationUseCase で実行
    到着 → actor.CurrentAction.Complete()
    ↓
施設ハンドラーが入口接触を検知
    Inn   → AdvanceGroundFacilityTaskOrchestrator → FacilityInteractionOrchestrator.UseInn()（入口到達後に予約・満室待機）
    GeneralStore → 不要な非装備アイテム売却、ポーションを2個まで購入
    EquipmentShop → 不要な未装備装備売却、より強い武器を購入可能なら購入
    Tavern → 明確な利用理由が仕様化されている場合のみ処理
    ↓
必要な施設利用がなくなる（AI が Prepare を返す） → Recovering → Preparing → GoingToDungeon
```

### 入口接触位置の計算

`FacilityEntranceNavigationTargetCalculator.Calculate(layer, facility)` は以下を返す。

```
entranceCenter = layer.GetCellCenter(facility.EntrancePosition)
target = entranceCenter + FacingDirectionVector(facility.FacingDirection) * (layer.CellSizeMeters * 0.5f)
```

`FacingDirectionVector` は North=(0,+1)、South=(0,-1)、East=(+1,0)、West=(-1,0)。

この target は「建物に入るための入口接触位置」であり、入口タイル中心そのものではない。施設利用判定と ActorView 非表示判定は、この target または入口中心への距離しきい値で判定する。

**理想設計**

AI は施設座標を計算しない。AI は `FacilityNeed` を選び、`GroundFacilityNavigationPlanner` が `FacilityInteractionPoint` を選ぶ。

```text
AI: ResolveFacilityNeed(UpgradeWeapon)
NavigationPlanner: EquipmentShop の最適 InteractionPoint を選択
Lifecycle: MoveTo(InteractionPoint.Position, InteractionPoint.Radius)
InteractionOrchestrator: 接触後に UpgradeWeapon Interaction を実行
PresenceService: Interacting 中は ActorView を非表示
```

この流れにすると、施設が移動しても、入口が複数になっても、施設が増えても AI の意思決定は変えずに済む。

### AI への施設位置の渡し方

`AdventurerAiPolicy` が施設入口位置を参照できるよう `ActorAiContext` を拡張する。

```csharp
// ActorAiContext に追加
public AdventurerGuild Guild { get; }
public GroundMap GroundMap { get; }
```

`AdvanceActorAiOrchestrator.ExecuteAsync()` に `IGameWorldState` を渡してコンテキストを生成する。

### Inn 予約の入口到達後処理

`WorldSimulationOrchestrator.AdvanceScheduleSystemsAsync()` では同スケジュールティック内で以下の順序で呼ばれる。

```
AdvanceScheduleSystemsAsync() 内:
  1. AdvanceScheduledActorLifecycleAsync()  // MoveTo 実行
  2. AdvanceGroundFacilityTaskOrchestrator  // 入口到達・満室待機・入室判定
  3. FacilityInteractionOrchestrator.UseInn()  // 到達後の予約処理
```

Inn の予約はスケジュール処理では作らない。Actor が施設入口の interaction point へ到達した後、
`FacilityInteractionOrchestrator.UseInn()` が空室・待機列・料金支払いをまとめて判定する。

- 変更前: `actor.Position.LayerId.Equals(MapLayerId.Ground)` のみチェック
- 変更後: 加えて `FacilityEntranceInteractionService.IsTouchingEntrance(groundMap.Layer, inn, actor.Position)` をチェック

### 設計上の重要な方針

- Actor の移動には既存の `MoveActorTowardDestinationUseCase` をそのまま使用する。施設ごとに移動用 UseCase を作成しない。
- 施設選択ロジックは `AdventurerAiPolicy` に置く。`AdvanceActorLifecycleOrchestrator` は Inn / Tavern / Shop を区別しない。
- 施設は必須巡回しない。AI は用事がある施設だけを選び、売却済み・購入済み・回復済みなど実データ上の用事が解消された施設は再選択しない。
- `WaitingForInn` 状態の Actor は Inn の入口タイルで待機する（移動不要）。
- `Recovering → Preparing` 遷移は AI が `Prepare` プランを返したタイミングで Lifecycle Orchestrator が行う。

**理想設計**

- `WaitingForInn` は LifecycleState ではなく `FacilityInteractionStatus.WaitingForCapacity` とする。
- `Recovering` も施設利用の大枠状態としては不要で、地上滞在中の Plan 群で表す。
- 施設利用順は固定リストではなく `FacilityNeedSelector` が優先度で選ぶ。
- 売却・購入・回復・装備切替は施設入口で開始した Interaction のトランザクション内で完了する。
- ActorView の非表示は入口座標ではなく `ActorPresence.HiddenInsideFacility` から導出する。

## 地上マップ生成との関係

`InitializeWorldMapUseCase` は `IFacilityBuildingSettingsRepository` から定義を取得してループで建物を配置する。建物矩形全体をまず `Building / Blocked` で塗りつぶした後、入口タイルだけを `FacilityEntrance / Walkable` で上書きする。

```csharp
foreach (var def in facilityBuildingSettingsRepository.GetDefinitions())
{
    // 建物全体を Blocked で塗る
    FillRectangle(cells, layer, def.OriginX, def.OriginZ, def.SizeCells, def.SizeCells,
        GroundCellType.Building, MapCellBlockType.Blocked);

    // 入口タイルを Facing から計算して Walkable に上書き
    var (localX, localZ) = CalculateEntranceLocal(def.SizeCells, def.Facing);
    var entrancePos = new GridPosition(def.OriginX + localX, def.OriginZ + localZ);
    SetCell(cells, layer, entrancePos, GroundCellType.FacilityEntrance, MapCellBlockType.Walkable);
}
```

## 既存バグ: Tavern の未生成

`FacilityType.Tavern` は `FacilityType` enum に定義済みだが、`InitializeGameWorldOrchestrator.CreateInitialGuild()` に Tavern のインスタンス生成が漏れていた。今回の実装で合わせて修正する。合わせて `InitialWorldSettings` と `WorldGameSettingsSO` に Tavern の初期設定（`TavernBasePrice`、`TavernCapacity`）を追加する。

## 変更対象ファイル一覧

| ファイル | 変更種別 | 内容 |
|---|---|---|
| `Domain/Facility/BuildingFacingDirection.cs` | 新規 | `BuildingFacingDirection` enum (North/South/East/West) |
| `Domain/Facility/Facility.cs` | 変更 | `BuildingOrigin`、`BuildingSizeCells`、`FacingDirection`、`EntrancePosition`、`IsAtEntrance()`、`SetBuildingInfo()` を追加 |
| `Domain/Map/GroundMap.cs` | 変更 | `IsAtFacilityEntrance(GridPosition)` メソッドを追加 |
| `Application/World/ActorViewData.cs` | 変更 | `IsInsideFacility` フラグを追加 |
| `Application/World/ActorViewDataStore.cs` | 変更 | `SyncActor()` で `IsInsideFacility` を更新（Guild 参照を追加） |
| `Application/World/GroundMapGenerationSettings.cs` | 変更 | 旧建物オフセット設定（3 フィールド）を削除 |
| `Application/Facilities/IFacilityBuildingSettingsRepository.cs` | 新規 | 施設建物設定のリポジトリインターフェース |
| `GameSession/Settings/FacilityBuildingSettingsSO.cs` | 新規 | `FacilityBuildingDefinition[]` を保持する専用 ScriptableObject |
| `GameSession/Settings/WorldGameSettingsSO.cs` | 変更 | 旧建物オフセット設定（3 フィールド）を削除。Tavern 初期設定フィールド（`TavernBasePrice`、`TavernCapacity`）を追加 |
| `Application/World/InitialWorldSettings.cs` | 変更 | `TavernBasePrice`、`TavernCapacity` を追加 |
| `Application/Dungeons/InitializeWorldMapUseCase.cs` | 変更 | `IFacilityBuildingSettingsRepository` を注入、ループ配置＋`Facing` から入口計算に変更 |
| `Application/World/InitializeGameWorldOrchestrator.cs` | 変更 | 施設生成後に `SetBuildingInfo()` で位置情報を設定。**Tavern を追加**（既存バグ修正） |
| `Application/Actors/Ai/ActorAiContext.cs` | 変更 | `AdventurerGuild`・`GroundMap` を追加 |
| `Application/Actors/Ai/AdvanceActorAiOrchestrator.cs` | 変更 | `IGameWorldState` を受け取りコンテキストに渡す |
| `Application/Actors/Ai/AdventurerAiPolicy.cs` | 変更 | `EvaluateMidTerm`・`EvaluateShortTerm` に Recovering 状態向けの施設利用判断を追加 |
| `Application/Actors/Lifecycle/AdvanceActorLifecycleOrchestrator.cs` | 変更 | `Recovering` / `WaitingForInn` 状態で `MoveTo` アクションを汎用実行。施設固有ロジックは持たない |
| `Application/Actors/Lifecycle/AdvanceInnRecoveryOrchestrator.cs` | 変更 | 予約作成・満室待機責務を廃止し、既存宿泊者の回復進行だけを扱う |
| `Application/Facilities/AdvanceGroundFacilityTaskOrchestrator.cs` | 追加 | 施設入口までの移動、満室時の入口外待機、空室後の入室再開を扱う |
| `Application/Facilities/FacilityInteractionOrchestrator.cs` | 追加 | 入口到達後の Inn 予約、GeneralStore 売買、EquipmentShop 売買と装備更新を扱う |
| View 層（ActorView 関連） | 変更 | `ActorViewData.IsInsideFacility` を参照して `ActorView.SetVisible()` を切り替え |

## 拡張方針

- 施設を追加するときは `FacilityType` に追加し、`FacilityBuildingSettingsSO` の配列に定義を足すだけでよい。
- 入口を複数持たせたい場合は `Facing` を配列にする拡張が可能。
- 建物の形状を非正方形にしたい場合は `SizeCells` を `WidthCells / DepthCells` に分離できる。
- 将来的に「施設内の別のインタラクション位置」が必要になった場合、同様のパターンで拡張できる。
