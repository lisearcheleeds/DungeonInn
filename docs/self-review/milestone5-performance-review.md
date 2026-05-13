# パフォーマンスレビュー報告（Milestone 5 Phase 1）

## 概要

DungeonInnプロジェクトはClean Architectureを採用した良く設計されたシミュレーションゲームです。Domain/Application層がUnityEngineから独立している点は優れています。ただし、現在の実装にはいくつかのパフォーマンス上の問題があります。特に **ゲームループ毎フレームの処理量**、**Update内でのList複製**、**LINQの連鎖**、**O(n²)の線形走査** が主要な改善対象です。

---

## 問題一覧

### [パフォーマンス-1] DetectCombatEncounterUseCase 内の O(n²) 二重走査

**重要度**: 高  
**場所**: `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/DetectCombatEncounterUseCase.cs:40-84`

**問題**:
```csharp
foreach (var actor in actors)  // O(n)
{
    var nearest = FindNearestHostile(worldState.Dungeon, actor, actors);  // O(n)
}
// 結果: O(n²)
```
外側ループで全Actor、内側で `FindNearestHostile` 内で再度全Actorを走査。Actor数100の場合、毎フレーム10,000の比較操作。

**原因**: Encounter 検出の度に全候補を線形走査している。

**解決案**:
- Spatial partitioning（空間分割）を導入し、自Actorの周辺セルのみを検索対象に限定。
- LayerId ごとに Actor リストをプリフィルタリングして処理量を削減。
- 戦闘中のActor のみを検出ロジック対象にする。

---

### [パフォーマンス-2] AttackAreaTargetResolver の毎フレーム全Actor走査（O(m×n)）

**重要度**: 高  
**場所**: `Client/Assets/DungeonInn/Runtime/Scripts/Application/Combat/AttackAreaTargetResolver.cs:24-36`

**問題**:
```csharp
foreach (var actor in worldState.Actors)  // O(n)
{
    if (/* 複数条件 */ !Contains(areaEffect, actor))
        continue;
    targets.Add(actor);
}
```
Area Effect 毎回、全 Actor リストを走査（O(n)）。複数 Area Effect が同時に存在すると O(m×n)（m=AreaEffect数、n=Actor数）。

**原因**: Area Effect の中心座標周辺だけでなく全Actorを走査している。

**解決案**:
- Spatial Partitioning 導入（DetectCombatEncounter 同様）。
- Area Effect の中心座標周辺グリッドのみを検索対象に限定。

---

### [パフォーマンス-3] WorldMapView の GameObject.CreatePrimitive() による大量生成

**重要度**: 高  
**場所**: `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldMapView.cs:111`

**問題**:
```csharp
var tile = GameObject.CreatePrimitive(PrimitiveType.Plane);
// ...
RemoveCollider(tile);  // 生成後すぐ削除する無駄な処理
```
- Ground: 100 × 100 = 10,000 個の Plane オブジェクトが生成される可能性がある。
- 各 Plane は Collider, Renderer, MeshFilter を持ち、初回構築時に数秒かかる可能性がある。
- Milestone 5 Roadmap では「タイルごとに GameObject を生成しない、chunk mesh を使う」と明記されているのに逆の実装。

**原因**: Roadmap Phase 4「Map Chunk Mesh 生成」が未完了のため、CreatePrimitive による workaround が固定化された。

**解決案**:
- Milestone 5 Phase 4 の chunk mesh 実装を進める（設計済み）。
- 現段階ではテストデータセットを小規模マップ（例: 20×20）に限定する。
- tile 生成にかかる時間を Unity Profiler で測定し、frame budget 内に収まるか確認。

---

### [パフォーマンス-4] AdvanceCombatUseCase 内での毎フレーム List 複製

**重要度**: 高  
**場所**: `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/AdvanceCombatUseCase.cs:41`

**問題**:
```csharp
var actors = new List<Actor>(worldState.Actors);  // 毎フレーム複製
foreach (var actor in actors)
```
List 複製は O(n) のメモリアロケーション。防御的コピーだが、現在のコード構造ではループ内で Actors リストの削除は発生しない。

**原因**: ループ中に actors が変更される可能性を懸念した防御的コピー。

**解決案**:
- 複製の必要性を確認し、不要であれば `foreach (var actor in worldState.Actors)` で直接走査する。
- 必要な場合は、戦闘中の Actor だけを別途リスト管理して処理量を削減。

---

### [パフォーマンス-5] WorldActorPresenter の毎フレーム HashSet 生成

**重要度**: 中  
**場所**: `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldActorPresenter.cs:33`

**問題**:
```csharp
var activeActorIds = new HashSet<Guid>();  // 毎フレーム新規アロケーション
foreach (var actor in gameWorldState.Actors)
{
    activeActorIds.Add(actor.Id);
}
```
毎フレーム HashSet を生成。60fps では 60回/秒 のアロケーション圧力。

**原因**: フィールドとして保持する設計になっていない。

**解決案**:
```csharp
readonly HashSet<Guid> activeActorIds = new();

public void UpdateVisuals()
{
    activeActorIds.Clear();  // 再利用
    foreach (var actor in gameWorldState.Actors)
    {
        activeActorIds.Add(actor.Id);
    }
    // 以下同じ
}
```

---

### [パフォーマンス-6] AdvanceProjectileUseCase の毎フレーム List 複製

**重要度**: 中  
**場所**: `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/AdvanceProjectileUseCase.cs:29`

**問題**:
```csharp
var projectiles = new List<ProjectileInstance>(worldState.Projectiles);
foreach (var projectile in projectiles)
{
    if (!AdvanceProjectile(worldState, projectile, deltaGameSeconds))
    {
        worldState.RemoveProjectile(projectile.Id);  // リスト変更
    }
}
```
ループ内での `RemoveProjectile()` を安全に行うための複製だが、毎フレーム O(m) アロケーション（m=projectile数）。

**解決案**:
```csharp
readonly List<Guid> toRemove = new();

public UniTask ExecuteAsync(IGameWorldState worldState, float deltaGameSeconds)
{
    toRemove.Clear();
    foreach (var projectile in worldState.Projectiles)
    {
        if (!AdvanceProjectile(worldState, projectile, deltaGameSeconds))
            toRemove.Add(projectile.Id);
    }
    foreach (var id in toRemove)
        worldState.RemoveProjectile(id);
    return UniTask.CompletedTask;
}
```

---

### [パフォーマンス-7] AdvanceAreaEffectUseCase も同様に毎フレーム List 複製

**重要度**: 中  
**場所**: `Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/AdvanceAreaEffectUseCase.cs:32`

**問題**: AdvanceProjectileUseCase と同じパターン。`worldState.AreaEffects` を毎フレーム List 複製している。

**解決案**: [パフォーマンス-6] と同様の遅延削除パターンに統一する。

---

### [パフォーマンス-8] WorldMapView の毎フレーム Dungeon.Floors 全走査

**重要度**: 中  
**場所**: `Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldMapView.cs:46-65`

**問題**:
```csharp
void BuildMissingLayerTiles()
{
    foreach (var pair in gameWorldState.Dungeon.Floors)  // 毎フレーム全フロア確認
    {
        var layerId = MapLayerId.DungeonFloor(pair.Key).Value;
        if (builtLayerIds.Contains(layerId))
            continue;
        // ...
    }
}
```
既に構築済みのレイヤーも毎フレーム反復している。ダンジョン深度が増えるほどスキャンコスト増加。

**原因**: 「フロアが追加されたか」をイベントでなくポーリングで検出している。

**解決案**:
- `BuildMissingLayerTiles()` を「フロアが追加された」GameEvent の購読に変更。
- または最後に構築したフロアインデックスを保持して差分のみ確認する。

---

### [パフォーマンス-9] AdvanceActorAiOrchestrator の LINQ FirstOrDefault 使用

**重要度**: 中  
**場所**: `Client/Assets/DungeonInn/Runtime/Scripts/Application/Orchestration/AdvanceActorAiOrchestrator.cs:94`

**問題**:
```csharp
var policy = policies.FirstOrDefault(x => x.CanHandle(actor));
```
ポリシー解決時に毎 AI 評価ごとにリスト線形走査。現在は policies が 4件のため問題ないが、将来 policy が増えると影響大。

**解決案**:
- Actor.Behavior の型をキーにした `Dictionary<Type, IActorAiPolicy>` を構築する。
- `policies[actor.Behavior.GetType()]` で O(1) 参照にする。

---

### [パフォーマンス-10] GameWorldState.RemoveActor() での List.FindIndex() 二重検索

**重要度**: 低  
**場所**: `Client/Assets/DungeonInn/Runtime/Scripts/Application/GameLoop/GameWorldState.cs:63-78`

**問題**:
```csharp
public bool RemoveActor(Guid actorId)
{
    if (!actorById.ContainsKey(actorId))
        return false;

    actorById.Remove(actorId);   // O(1)
    var index = actors.FindIndex(x => x.Id.Equals(actorId));  // O(n) 再検索
    if (index >= 0)
        actors.RemoveAt(index);
    return true;
}
```
Dictionary で O(1) 削除した後、List から同じ ID を O(n) で再検索している。

**解決案**:
- Swap-remove パターン（末尾要素と交換後 RemoveAt(last)）で O(1) 削除。
- または Tombstone パターン（削除マークのみ付けて後で整理）。

---

### [パフォーマンス-11] GameEventHistoryService の ToArray() によるアロケーション

**重要度**: 低  
**場所**: `Client/Assets/DungeonInn/Runtime/Scripts/Application/Event/GameEventHistoryService.cs:45-78`

**問題**:
```csharp
var source = entries.ToArray();  // 毎回複製（容量4096）
```
UI が頻繁にイベント履歴を取得する場合、毎回 4096 件のキューを複製。

**原因**: Queue の内部構造をコピーしてスライス取得しているため、アロケーションが発生。

**解決案**:
- イテレータパターン導入（yield return）で実際に使うエントリだけ列挙。
- または、`ReadOnlySpan<GameEventHistoryEntry>` でビューを返す。

---

## 総評

### 優れた設計

1. **AI Dirty Flag システム**: 毎フレーム評価でなく状態変化時のみ再評価する効率的な設計。
2. **Dictionary 活用**: `GameWorldState.FindActor()` など O(1) 参照が適切に使われている。
3. **Domain/Application が UnityEngine 非依存**: テスト可能性と再利用性が高い。
4. **LayerPosition.DistanceSquaredTo()**: 距離比較時に平方根を回避する正しい実装。

### 主要改善対象（優先順位順）

| 優先度 | 問題 | 影響規模 | 推定改善効果 |
|---|---|---|---|
| 最高 | DetectCombatEncounter O(n²) → Spatial Partitioning | Actor 50人超で深刻 | O(n²)→O(n log n) |
| 最高 | AttackAreaTargetResolver O(m×n) → Spatial Partitioning | AreaEffect 増加時 | O(m×n)→O(m log n) |
| 高 | WorldMapView の CreatePrimitive → Chunk Mesh | 初回ロード時 | 数秒→数ミリ秒 |
| 高 | List 複製の除去（Combat/Projectile/AreaEffect） | 毎フレーム | GC圧力軽減 |
| 中 | WorldActorPresenter HashSet の再利用 | 毎フレーム | GC圧力軽減 |
| 中 | BuildMissingLayerTiles → イベント駆動化 | 毎フレーム | 不要な走査除去 |

### プロファイリング推奨

```
現在の推定ボトルネック:
1. DetectCombatEncounter: O(n²) - 多数Actor時に数ms
2. AttackAreaTargetResolver: O(m×n) - AreaEffect増加時
3. WorldMapView 初回構築: GameObject大量生成
4. AI評価チェーン: O(n) per frame（DirtyFlagのおかげで限定的）
```

Unity Profiler + Burst Inspector で実測後に優先順位を再検討することを推奨する。
