# task_012: Application AI 実装

## 概要

Application/AI/ 配下の `DefaultAdventurerAI` メソッド本体と、
4 つのステートクラスを新規作成する。

---

## AGENTS.md を必ず読むこと

このリポジトリの AGENTS.md に Lighthouse / UniTask / Addressables の禁止事項と正しいパターンが書いてある。
作業前に読むこと。特に **Application 層は Unity 禁止・NavMeshAgent 禁止** のルールに注意する。

---

## 設計方針

冒険者 AI はステートマシン。各ステートは `IAdventurerAIState`（internal interface）を実装する。

```
AdventurerState.Resting          → RestingState
AdventurerState.TravelingToDungeon → TravelingState
AdventurerState.ExploringDungeon  → ExploringState
AdventurerState.Returning         → ReturningState
```

### Presenter との分担

- `IAdventurerAI.Tick()` は次の目標 `GridPosition?` を返すだけ
- NavMeshAgent.SetDestination() は View 層（AdventurerPresenter）が呼ぶ → AI 側は不要
- 位置到達の検知も View 層が行い、到達時に `character.TransitionState()` を呼ぶ
- タイマーベースのステート遷移は AI ステートの `Tick()` 内で `character.TransitionState()` を直接呼ぶ

---

## 対象ファイル

### 既存（本体実装のみ）

```
Client/Assets/DungeonInn/Runtime/Scripts/Application/AI/DefaultAdventurerAI.cs
```

現在の stub:
```csharp
public class DefaultAdventurerAI : IAdventurerAI
{
    private readonly IReadOnlyList<IAdventurerAIState> states;
    private IAdventurerAIState current;

    public DefaultAdventurerAI(IReadOnlyList<IAdventurerAIState> states) { ... }

    public void OnStateEnter(AdventurerCharacter character, AdventurerState state) { throw ... }
    public GridPosition? Tick(AdventurerCharacter character, float deltaTime) { throw ... }
    public void OnStateExit(AdventurerCharacter character, AdventurerState state) { throw ... }
}
```

### 新規作成（4 ファイル）

```
Client/Assets/DungeonInn/Runtime/Scripts/Application/AI/RestingState.cs
Client/Assets/DungeonInn/Runtime/Scripts/Application/AI/TravelingState.cs
Client/Assets/DungeonInn/Runtime/Scripts/Application/AI/ExploringState.cs
Client/Assets/DungeonInn/Runtime/Scripts/Application/AI/ReturningState.cs
```

---

## 実装仕様

### DefaultAdventurerAI

```csharp
// OnStateExit: current?.Exit(character)  ← 現ステートを終了
// OnStateEnter:
//   current = states.FirstOrDefault(s => s.StateType == state)
//   current?.Enter(character)
// Tick:
//   return current?.Tick(character, deltaTime)  ← null の場合もある（移動目標なし）
```

using が必要なもの:
- `System.Linq` (FirstOrDefault)
- `DungeonInn.Domain.Character`
- `DungeonInn.Domain.World`

### RestingState

```csharp
// namespace: DungeonInn.Application.AI
// internal class
//
// コンストラクタ: RestingState(float restDuration)
// フィールド: float restDuration, float elapsed
//
// StateType → AdventurerState.Resting
//
// Enter: elapsed = 0
//
// Tick:
//   elapsed += deltaTime
//   if (elapsed >= restDuration)
//     character.TransitionState(AdventurerState.TravelingToDungeon)
//   return null  ← 宿屋ベッドで静止。移動目標なし
//
// Exit: 何もしない
```

### TravelingState（宿屋 → ダンジョン入口）

```csharp
// namespace: DungeonInn.Application.AI
// internal class
//
// コンストラクタ: TravelingState(GridPosition dungeonEntrance)
// フィールド: GridPosition dungeonEntrance
//
// StateType → AdventurerState.TravelingToDungeon
//
// Enter: 何もしない
//
// Tick: return dungeonEntrance
//   ← View 層 Presenter が NavMesh 到達を検知して TransitionState(ExploringDungeon) を呼ぶ
//
// Exit: 何もしない
```

### ExploringState（ダンジョン探索）

```csharp
// namespace: DungeonInn.Application.AI
// internal class
//
// コンストラクタ: ExploringState(IReadOnlyList<GridPosition> waypoints, float exploreDuration, float waypointSwitchInterval)
// フィールド: waypoints, exploreDuration, waypointSwitchInterval, float elapsed, float waypointElapsed, int waypointIndex
//
// StateType → AdventurerState.ExploringDungeon
//
// Enter: elapsed = 0; waypointElapsed = 0; waypointIndex = 0
//
// Tick:
//   elapsed += deltaTime
//   if (elapsed >= exploreDuration)
//     character.TransitionState(AdventurerState.Returning)
//     return null
//   if (waypoints.Count == 0) return null
//   waypointElapsed += deltaTime
//   if (waypointElapsed >= waypointSwitchInterval)
//     waypointIndex = (waypointIndex + 1) % waypoints.Count
//     waypointElapsed = 0f
//   return waypoints[waypointIndex]
//
// Exit: 何もしない
```

### ReturningState（ダンジョン → 宿屋入口）

```csharp
// namespace: DungeonInn.Application.AI
// internal class
//
// コンストラクタ: ReturningState(GridPosition innEntrance)
// フィールド: GridPosition innEntrance
//
// StateType → AdventurerState.Returning
//
// Enter: 何もしない
//
// Tick: return innEntrance
//   ← View 層 Presenter が NavMesh 到達を検知して TransitionState(Resting) を呼び、CheckIn UseCase を実行
//
// Exit: 何もしない
```

---

## 制約

- Application 層: Unity 依存禁止（UnityEngine.* using 不可）
- Lighthouse 依存禁止（LighthouseExtends.* using 不可）
- NavMeshAgent 禁止 → AI は GridPosition? を返すだけ
- Task / ValueTask 禁止
- コメントは WHY が非自明な場合のみ（WHAT を説明するコメント禁止）
- namespace は `DungeonInn.Application.AI`
- ステートクラスは `internal class`（外部公開不要）

---

## 完了条件

- [ ] DefaultAdventurerAI の 3 メソッドが実装済み
- [ ] RestingState, TravelingState, ExploringState, ReturningState の 4 ファイルが新規作成済み
- [ ] `uloop.cmd compile --project-path Client` エラーゼロ
- [ ] `review/task_012_done.md` に完了報告（問題があれば記載）
