# Milestone 4 Roadmap

このドキュメントは Milestone 4 の実装計画をまとめる。

対象ループ:

```text
冒険者が来訪する
-> ダンジョンへ向かう（NavMesh による経路探索）
-> モンスターと戦闘（Projectile / Area 攻撃を含む）
-> 帰還・成長・経済ループ（Milestone 3 継続）
```

## 到達目標

- 戦闘に Projectile（飛び道具）と Area 攻撃（範囲攻撃）が加わり、戦術的な多様性が生まれる
- 移動が NavMesh による経路探索になり、ダンジョンの地形を正しく回避して移動する
- 上記2点によって Milestone 2〜3 のシミュレーションが視覚的にも説得力ある動きを見せる

## 前提

- Milestone 3 のゲームループ（成長・アイテム経済）が安定して動作していること
- NavMesh の Bake がダンジョン生成後に動的に実行できること
- Milestone 4 に入る前に、回復薬・バフ・デバフ共通基盤として `docs/design/actor-effect-status-effect-design.md` の ActorEffect / StatusEffect を実装する

---

## Phase 1: Projectile 攻撃

遠距離攻撃を持つモンスター・冒険者が飛び道具を発射する。

作るもの:

- `ProjectileInstance`（位置, 速度, 発射元, ターゲット, ダメージ）
- `GameWorldState.Projectiles`
- `AdvanceProjectileUseCase`（フレームごとに飛翔体を移動・命中判定）
- `ActorAttackType`（Direct / Projectile / Area）
- `AttackMaster` の Projectile パラメータ（射程, 速度, 弾道）

初期仕様:

- 直線飛翔のみ
- ターゲットに命中 or 最大射程で消滅
- Direct 攻撃との共存（攻撃タイプはモンスター/冒険者ごとに設定）

完了条件:

```text
[Combat] Goblin Archer fired arrow at Adventurer A
[Combat] Arrow hit Adventurer A for 12
```

---

## Phase 2: Area 攻撃

爆発や広域魔法など、範囲内の複数ターゲットに効果を与える攻撃を実装する。

作るもの:

- `AreaEffectInstance`（中心位置, 半径, 持続時間, ダメージ/効果）
- `GameWorldState.AreaEffects`
- `AdvanceAreaEffectUseCase`（範囲内アクターへの効果適用）
- 発動条件（即時 or 着弾後 or 時間差）

初期仕様:

- 即時爆発と一定時間持続する床置き効果の2種類
- ダメージのみ（バフ/デバフは Milestone 5 以降）
- Friendly Fire は考慮しない（Faction 判定のみ）

完了条件:

```text
[Combat] Mage cast Fireball at (15.0, 8.0) radius 4m
[Combat] Fireball hit Adventurer A for 20, Adventurer B for 15
```

---

## Phase 3: 高度な NavMesh 連携

移動を直線移動から NavMesh 経路探索に切り替える。

作るもの:

- `NavMeshBakeService`（ダンジョン生成後に NavMesh を動的に Bake）
- `IActorNavigationService` の NavMesh 実装（`NavMeshActorNavigationService`）
- `MoveActorTowardDestinationUseCase` の NavMesh パス追従モード

初期仕様:

- ダンジョン生成完了後に NavMesh を Bake
- フロアを跨いだ移動は NavMesh を切り替える
- 経路が取れない場合は直線移動にフォールバック

完了条件:

- 冒険者・モンスターが壁を通り抜けず、廊下や部屋を通って目標へ到達すること
- ダンジョン再生成後に NavMesh が再 Bake されること

---

## 推奨実装順

1. Phase 1: Projectile 攻撃
2. Phase 2: Area 攻撃
3. Phase 3: 高度な NavMesh 連携

NavMesh 連携は他フェーズと独立しているため、Phase 1/2 と並行して着手可能。
ただし View 表示が整ってから着手するほうが動作確認がしやすい。
