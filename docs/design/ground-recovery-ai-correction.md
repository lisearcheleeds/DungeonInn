# Ground 地上帰還後 AI 施設利用設計 — 緊急修正プラン

## 問題

`ground-facility-placement-design.md` の設計では `AdvanceActorLifecycleOrchestrator.Recovering` に「Inn のみを対象としたハードコード移動ロジック」を追加していた。これは以下の理由で設計として誤り。

1. `ActorPlanType.Recover` / `ActorPlanType.UseFacility` / `ActorAction.MoveTo()` が既にドメインに存在しており、AI が施設利用を制御する設計になっている。
2. Inn だけでなく、Tavern・GeneralStore・EquipmentShop への移動も Actor の状態・目的に応じて AI が必要な場合だけ判断すべき。
3. `AdvanceActorLifecycleOrchestrator.GoingToDungeon` や `Exploring` と同様、Lifecycle Orchestrator はあくまで「AI が設定したアクションを実行するだけ」であるべき。

## 正しい設計方針

ダンジョン内でルーム・階段に向かう処理と完全に同じパターンを使う。

1. **AI** が Actor の状態を評価し、`ActorPlanType.Recover` / `ActorPlanType.UseFacility` などのプランを設定する。
2. **AI** が `ActorAction.MoveTo(facilityEntranceTouchPosition)` を設定することで移動先を決める。
3. **AdvanceActorLifecycleOrchestrator.Recovering** は `actor.CurrentAction` が `MoveTo` であれば `MoveActorTowardDestinationUseCase` で実行するだけ。Inn 固有のロジックは持たない。
4. Actor が施設入口接触位置に到着したら、各施設ハンドラー（`AdvanceInnRecoveryOrchestrator` 等）が入室を検知して処理する。

## 理想設計

互換性維持を考慮しない場合、`Recovering` 中に AI が施設を順に選ぶ設計ではなく、「地上滞在中の未解決 Need を解決する Plan システム」として再設計する。

### 実装時の優先順位

このドキュメントは当初の緊急修正案を含むため、`Recovering` 分岐、`ShouldUse...()`、`AdvanceInnRecoveryOrchestrator`、`AdventurerGuild` / `GroundMap` を直接持つ `ActorAiContext` の記述が残っている。実装時に迷った場合は、以下を確定方針とする。

- `Recovering` / `WaitingForInn` を施設利用の中心状態にしない。
- AI は `FacilityNeed` を生成・選択する。施設別 `ShouldUse...()` を Policy に並べない。
- `ActorAiContext` は読み取り専用 Snapshot を持つ。
- 入口接触判定は `FacilityInteractionPoint.Contains()` に統一する。
- 施設利用は `FacilityInteractionOrchestrator` と `IFacilityInteractionHandler` が処理する。
- `PrepareNextAdventure` は `FacilityNeed` がなくなった後の Plan として扱う。

理想的な全体像:

```text
ActorLifecycleState.OnGround
  ActorPlan.ResolveFacilityNeed(RecoverAtInn)
  ActorPlan.ResolveFacilityNeed(SellGeneralGoods)
  ActorPlan.ResolveFacilityNeed(RefillPotions)
  ActorPlan.ResolveFacilityNeed(SellUnusedEquipment)
  ActorPlan.ResolveFacilityNeed(UpgradeWeapon)
  ActorPlan.PrepareNextAdventure
  ActorPlan.EnterDungeon
```

- Lifecycle は「地上にいる / ダンジョンにいる / 戦闘中」など大枠だけを表す。
- 施設利用の進行は `ActorTaskState` が持つ。
- AI は訪問履歴ではなく `FacilityNeed` の有無を評価する。
- Navigation は `FacilityInteractionPoint` までの移動だけを担当する。
- Interaction は入口接触後に施設別 Handler が実行する。
- View の入退室表現は `ActorPresence` から導出する。

この構造にすると、Inn / GeneralStore / EquipmentShop / Tavern の差異は `FacilityNeedEvaluator` と `IFacilityInteractionHandler` に閉じる。Lifecycle Orchestrator、Navigation、View は施設種別に依存しない。

## AI が施設を選ぶフロー

```
[Returning → Ground 到着] → Recovering 状態
    ↓
AdvanceActorAiOrchestrator（AI 評価）
    AdventurerAiPolicy.EvaluateMidTerm()
        HP が低い            → ActorPlanType.Recover    (targetId = inn.Id)
        雑貨屋で売却/補充が必要 → ActorPlanType.UseFacility (targetId = generalStore.Id)
        装備屋で売却/武器更新が必要 → ActorPlanType.UseFacility (targetId = equipmentShop.Id)
        Tavern を使う明確な理由がある → ActorPlanType.UseFacility (targetId = tavern.Id)
        何も必要ない          → ActorPlanType.Prepare（ダンジョンへ）
    ↓
    AdventurerAiPolicy.EvaluateShortTerm()
        現在プランに対応する施設の入口接触位置を取得
        → ActorAction.MoveTo(facilityEntranceTouchPosition) を設定
    ↓
AdvanceActorLifecycleOrchestrator.Recovering（移動実行）
    actor.CurrentAction が MoveTo → MoveActorTowardDestinationUseCase で実行
    到着 → Action を Complete にする（次フレームで AI が次のプランを決定）
    ↓
施設ハンドラーが入口接触を検知
    Inn   → AdvanceInnRecoveryOrchestrator（予約・回復）
    GeneralStore → 不要な非装備アイテム売却、ポーションを2個まで購入
    EquipmentShop → 不要な未装備装備売却、より強い武器を購入可能なら購入
    Tavern → 明確な利用理由が仕様化されている場合のみ処理
    ↓
必要な施設利用がなくなる（AI が Prepare プランを返す）
    → AdvanceActorLifecycleOrchestrator が Prepare を検知
    → GoingToDungeon 状態へ遷移
```

## 修正が必要なコンポーネント

### 1. ActorAiContext の拡張

AI が施設の入口位置を参照できるよう `AdventurerGuild` を追加する。

```csharp
public sealed class ActorAiContext
{
    public Actor Actor { get; }
    public float CurrentTimeSeconds { get; }
    public ActorAiRuntimeState RuntimeState { get; }
    public AdventurerGuild Guild { get; }       // 追加: 施設位置参照に使用
    public GroundMap GroundMap { get; }         // 追加: LayerPosition 変換に使用
}
```

`AdvanceActorAiOrchestrator.ExecuteAsync()` に `IGameWorldState` を渡してコンテキストを生成する。

**理想設計**

`ActorAiContext` は `AdventurerGuild` と `GroundMap` の可変参照を持たず、AI 判断用の読み取り専用 Snapshot を持つ。

```csharp
public sealed class ActorAiContext
{
    public Actor Actor { get; }
    public FacilityCatalogSnapshot Facilities { get; }
    public GroundNavigationSnapshot Navigation { get; }
    public ActorAdventureIntent NextAdventureIntent { get; }
    public int CurrentTick { get; }
}
```

AI は Snapshot から `FacilityNeed` を生成し、状態変更は Interaction 実行時まで行わない。

### 2. AdventurerAiPolicy の拡張

`EvaluateMidTerm()` に地上帰還後の施設利用判断を追加する。

```csharp
public ActorAiDecision EvaluateMidTerm(ActorAiContext context)
{
    var behavior = actor.Behavior as AdventurerBehavior;
    if (behavior == null || behavior.LifecycleState != AdventurerLifecycleState.Recovering)
    {
        // 従来の Prepare プラン設定（非 Recovering 状態）
        ...
    }

    // Recovering 状態: 必要な施設だけを選ぶ
    if (actor.HpRatio < RecoverHpThreshold)
        return new ActorAiDecision(null, new ActorPlan(ActorPlanType.Recover, innId, 0), null);

    if (ShouldUseGeneralStore(actor, context.Guild))
        return new ActorAiDecision(null, new ActorPlan(ActorPlanType.UseFacility, generalStoreId, 0), null);

    if (ShouldUseEquipmentShop(actor, context.Guild))
        return new ActorAiDecision(null, new ActorPlan(ActorPlanType.UseFacility, equipmentShopId, 0), null);

    if (ShouldUseTavern(actor, context.Guild))
        return new ActorAiDecision(null, new ActorPlan(ActorPlanType.UseFacility, tavernId, 0), null);

    // 必要な施設利用なし → Prepare
    return new ActorAiDecision(null, new ActorPlan(ActorPlanType.Prepare, 0, 0), null);
}
```

`EvaluateShortTerm()` では現在プランに対応する施設の入口接触位置を取得して `MoveTo` アクションを設定する。

```csharp
var facility = context.Guild.GetFacility(actor.CurrentPlan.TargetId);
return new ActorAiDecision(
    null, null,
    ActorAction.MoveTo(FacilityEntranceNavigationTargetCalculator.Calculate(
        context.GroundMap.Layer,
        facility)));
```

`FacilityEntranceNavigationTargetCalculator.Calculate()` は入口タイル中心から `BuildingFacingDirection` 方向へ `CellSizeMeters * 0.5f` だけずらした位置を返す。NavMesh が建物メッシュに阻まれて入口タイル中心に入れない場合でも、入口に触れて ActorView が消える見た目を作るため。

「訪問済みフラグ」を主条件にした再訪抑止は行わない。GeneralStore は売却対象がなくなり、ポーションが2個まで補充されれば `ShouldUseGeneralStore()` が false になる。EquipmentShop は売却対象がなくなり、より強い武器を購入・装備できなくなれば `ShouldUseEquipmentShop()` が false になる。Tavern は利用理由が仕様化されている場合だけ選ぶ。

**理想設計**

施設別の `ShouldUse...()` を `AdventurerAiPolicy` に置かない。`GroundFacilityNeedEvaluator` が Need を列挙し、`FacilityNeedSelector` が優先度・距離・期待効果から 1 つ選ぶ。

```csharp
groundFacilityNeedEvaluator.CollectNeeds(context, needs);
var selected = facilityNeedSelector.SelectBest(context, needs);
return selected.HasValue
    ? ActorAiDecision.Plan(ActorPlan.ResolveFacilityNeed(selected.Value))
    : ActorAiDecision.Plan(ActorPlan.PrepareNextAdventure());
```

優先度の初期方針:

- 次回冒険に必要な HP/MP が足りない場合は Inn を最優先。
- ポーションが 2 個未満で購入可能なら GeneralStore。
- 現在武器より明確に強い武器を買えるなら EquipmentShop。
- 不要品売却は購入資金を作るため、購入 Need と同じ施設なら先に実行する。
- Tavern は仕様上の効果が明確になった時だけ Need を生成する。

### 3. AdvanceActorLifecycleOrchestrator の Recovering ハンドラー

Inn 固有ロジックを排除し、`MoveTo` アクションを実行するだけにする。

```csharp
case AdventurerLifecycleState.Recovering:
case AdventurerLifecycleState.WaitingForInn:
    await AdvanceGroundMovementAsync(actor, behavior, worldState, deltaGameSeconds);
    break;
```

```csharp
async UniTask AdvanceGroundMovementAsync(Actor actor, AdventurerBehavior behavior, IGameWorldState worldState, float deltaGameSeconds)
{
    if (!actor.Position.LayerId.Equals(MapLayerId.Ground)) return;
    if (actor.CurrentAction?.Type != ActorActionType.Move) return;
    if (!actor.CurrentAction.HasTargetPosition) return;

    var groundMap = worldState.GroundMap;
    var arrived = moveActorTowardDestinationUseCase.Execute(
        actor,
        actor.CurrentAction.TargetPosition,
        groundMap.Layer,
        groundMap,
        worldGameSettingsRepository.GetActorSimulationSettings().MoveSpeedMetersPerSecond,
        deltaGameSeconds);

    if (arrived)
    {
        actor.CurrentAction.Complete();
        navigationService.InvalidatePath(actor.Id);
    }
}
```

**理想設計**

`Recovering` と `WaitingForInn` を lifecycle enum から外し、`ActorTaskStateMachine` に移す。

```text
OnGround
  MovingToInteractionPoint
  Interacting
  WaitingForInteraction
  Completed
```

Lifecycle Orchestrator は現在 Action の進行だけを扱い、施設種別や予約処理を知らない。施設利用の開始条件は `ActorTaskStateMachine` が `FacilityEntranceDetector` で判定する。

### 4. 施設ハンドラーの入口接触チェック

変更方針は `ground-facility-placement-design.md` に記載のとおり維持。AI が施設入口接触位置に向けて `MoveTo` を設定 → 到着 → アクション完了 → 次フレームで各施設ハンドラーが入口接触チェックで処理する。

- Inn: `AdvanceInnRecoveryOrchestrator` が予約・回復を行う。
- GeneralStore: 不要な非装備アイテムを売却し、購入できるならポーションを2個まで補充する。
- EquipmentShop: 不要な未装備装備を売却し、購入できるなら現在装備より強い武器へ置き換える。
- Tavern: 利用理由が仕様化されている場合のみ処理する。

入口接触判定は `GridPosition == EntrancePosition` のみにしない。入口タイル中心、または入口タイル中心から施設の向きへ半タイルずらした接触位置への距離で判定する。

**理想設計**

施設ハンドラーは入口判定を持たない。入口判定は `FacilityEntranceDetector` が行い、接触済みの Actor だけが `FacilityInteractionOrchestrator` に渡される。

```csharp
var entrance = entranceDetector.FindTouchedInteractionPoint(actor, facilityCatalog);
if (entrance.HasValue && actor.CurrentTask.CanStartInteraction(entrance.Value.FacilityId))
{
    facilityInteractionOrchestrator.StartOrAdvance(actor, entrance.Value, worldState);
}
```

Handler は施設ごとの処理だけに集中する。

- Inn Handler: 予約・待機・回復。
- GeneralStore Handler: 非装備不要品売却、ポーション補充。
- EquipmentShop Handler: 未装備装備売却、強武器購入、装備確定。
- Tavern Handler: 仕様化済みの食事・情報収集・バフなど。

### 5. Recovering → Preparing の遷移

AI が `ActorPlanType.Prepare` を返したタイミングで `AdvanceActorLifecycleOrchestrator` が `Recovering → Preparing` 遷移を行う。

```csharp
if (behavior.LifecycleState == AdventurerLifecycleState.Recovering
    && actor.CurrentPlan.Type == ActorPlanType.Prepare)
{
    behavior.ChangeLifecycleState(AdventurerLifecycleState.Preparing);
    candidateService.SyncActor(actor);
}
```

**理想設計**

`Recovering → Preparing` という固定遷移ではなく、AI が `FacilityNeed` なしと判断した時点で `PrepareNextAdventure` Plan を発行する。

```text
ResolveFacilityNeed Completed
  → AI reevaluate
  → no FacilityNeed
  → PrepareNextAdventure
  → EnterDungeon
```

準備完了後に `GoingToDungeon` へ遷移するため、施設利用が 0 回でも複数回でも同じ流れになる。

## ground-facility-placement-design.md への影響

以下のセクションを修正する。

- **「Actor の地上移動と施設利用フロー」**: Inn 固定フローを削除し、AI 主導の必要時施設利用フローに差し替える。
- **「変更対象ファイル一覧」**: `AdvanceActorLifecycleOrchestrator` の変更内容を「Recovering 状態で MoveTo アクションを実行（Inn 固定ではない）」に修正。`AdvanceActorAiOrchestrator`・`ActorAiContext`・`AdventurerAiPolicy` を変更対象に追加。

## 変更対象ファイル（このプランで追加・修正）

| ファイル | 変更種別 | 内容 |
|---|---|---|
| `Application/Actors/Ai/ActorAiContext.cs` | 変更 | `AdventurerGuild`・`GroundMap` を追加 |
| `Application/Actors/Ai/AdvanceActorAiOrchestrator.cs` | 変更 | `IGameWorldState` を受け取り Context に渡す |
| `Application/Actors/Ai/AdventurerAiPolicy.cs` | 変更 | `EvaluateMidTerm`・`EvaluateShortTerm` に Recovering 状態向けの施設利用判断を追加 |
| `Application/Actors/Lifecycle/AdvanceActorLifecycleOrchestrator.cs` | 変更 | `Recovering` / `WaitingForInn` で MoveTo アクションを汎用実行。Inn 固有ロジックを持たない |
