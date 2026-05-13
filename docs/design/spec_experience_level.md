# 経験値・レベルシステム設計

## 概要

アクターは戦闘で敵を倒すことで経験値を獲得し、累計経験値がしきい値を超えるとレベルアップする。
レベルはアクターの種別ごとに異なるテーブルで計算される。

---

## データ構造

### Actor（Domain）

| プロパティ | 型 | 説明 |
|---|---|---|
| `Level` | `int` | 現在のレベル（最小値 1） |
| `Experience` | `int` | 累計獲得経験値（0以上） |
| `ArchetypeId` | `int` | 所属するアーキタイプID（LevelTable の引き当てに使用） |

### ActorArchetypeMaster（Master）

| プロパティ | 型 | 説明 |
|---|---|---|
| `LevelTableId` | `int` | 使用するレベルテーブルのID |
| `InitialLevel` | `int` | スポーン時の初期レベル |

### LevelTable（Master）

累積経験値配列 `cumulativeXp[level]` を保持し、経験値からレベルを逆引きする。

| メソッド | 説明 |
|---|---|
| `GetLevel(xp)` | 経験値からレベルを返す（二分探索） |
| `GetExperienceForLevel(level)` | そのレベルに到達するのに必要な累計経験値を返す |

#### 現在のテーブル定義（HardcodedMasterRepository）

| ID | 対象 | 式 | Lv1→Lv2 累計XP |
|---|---|---|---|
| 1 | 冒険者 | `n*(n+1)/2 * 10` | 30 XP |
| 2 | モンスター | `n*(n+1)/2 * 100` | 300 XP |

---

## 経験値報酬の計算（GameConstants.Combat）

```
報酬XP = max(KillExperienceRewardMinimum, 被撃破者のExperience × Numerator / Denominator)
```

| 定数 | 値 | 意味 |
|---|---|---|
| `KillExperienceRewardNumerator` | 1 | 分子 |
| `KillExperienceRewardDenominator` | 10 | 分母 |
| `KillExperienceRewardMinimum` | 1 | 最低保証報酬 |

**計算例（Lv1ゴブリン撃破時）**：  
ゴブリンの初期 Experience = 100 → 報酬 = max(1, 100 × 1/10) = **10 XP**

---

## ゲームバランスの根拠

- 冒険者 Lv1→Lv2 に必要な累計XP = 30
- Lv1ゴブリンを倒すと 10 XP 獲得
- → **2体倒してレベルアップ**（2体で20 XP + 初期0 XP = 累計20、3体目で30 XP到達）

---

## 処理フロー

```
ActorDefeatOrchestrator（敵HPが0になった時）
  └─ GrantExperienceService.Grant(killer, defeated)
       1. killer.ArchetypeId <= 0 なら早期リターン（テスト用アクター対策）
       2. xpReward を計算
       3. killer.GainExperience(xpReward)
       4. IGameEventBus.Publish(ExperienceGranted)
       5. killer.RecalculateLevel(levelTable)
       6. レベルが上昇していれば IGameEventBus.Publish(ActorLeveledUp)
```

### Actor.RecalculateLevel(LevelTable)

```csharp
Level = Math.Max(1, levelTable.GetLevel(Experience));
RefreshParams();  // Stats / 武器パラメータを再計算
```

`Level` はキャッシュ値。経験値の変化後に必ず `RecalculateLevel` を呼ぶことでキャッシュを更新する。  
`RefreshParams` を内部で呼ぶため、レベルアップに伴うパラメータ変化は自動的に反映される。

---

## イベント

| イベント | 発行タイミング | 主なフィールド |
|---|---|---|
| `ExperienceGranted` | 経験値獲得時 | `ActorId`, `GainedXp`, `TotalXp` |
| `ActorLeveledUp` | レベルが上昇した時 | `ActorId`, `PreviousLevel`, `NewLevel` |

---

## アクター生成時の初期化（ActorFactoryCore）

1. `levelTable.GetExperienceForLevel(archetypeMaster.InitialLevel)` で初期累計XPを算出
2. Actor コンストラクタに渡す
3. `actor.RecalculateLevel(levelTable)` でレベルキャッシュを確定

---

## 設計上の注意

- `Level` は経験値から常に算出可能なキャッシュ。経験値を直接変更した場合は必ず `RecalculateLevel` を呼ぶこと
- `ArchetypeId = 0` のアクター（テスト用途）は `GrantExperienceService` が早期リターンするため経験値処理をスキップする
- `LevelTable` は最大レベルを `cumulativeXp.Length - 1` で表現する。配列は `[0..MaxLevel]` のサイズが必要
