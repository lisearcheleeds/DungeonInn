# ActorActionPhase システム設計

## 目的

Actor のアクション実行を「フェーズのシーケンス」として定義し、以下を実現する。

- アクション中の移動・次行動判断をブロックする（硬直）
- フェーズ移行タイミングでゲーム効果を発生させる（攻撃ダメージ、Projectile 発射など）
- フェーズ移行イベントを View 層に通知してアニメーション切り替えに使う
- アクション種別 × サブタイプ（スペルなど）ごとに個別のフェーズ定義を持てる

---

## 要件

| # | 要件 |
|---|---|
| R1 | 攻撃・ルーム到達・アイテム拾得の後に 1 秒間 Actor を硬直させる |
| R2 | 硬直中は移動・AI 再評価を行わない |
| R3 | アクション種別 × サブタイプ（スペル ID など）ごとにフェーズ定義を持てる |
| R4 | 1 フェーズは「フェーズ名」と「継続時間（秒）」で定義する |
| R5 | フェーズ移行時にイベントを発行し、View のアニメーション切り替えに使える |
| R6 | 将来の Magic アクションで「詠唱 → モーション開始 → Projectile 発射 → モーション終了」などの多段フェーズを定義できる |
| R7 | Effect フェーズ開始時にゲームロジック（ダメージ、Projectile 生成など）が実行される |
| R8 | フェーズ定義を持たないアクションは現行通り即時完了として扱う（後方互換） |

---

## 用語

| 用語 | 意味 |
|---|---|
| フェーズシーケンス | 1 つのアクションを構成する ordered な `ActorActionPhaseDef` のリスト |
| アクティブシーケンス | Actor が現在実行中のフェーズシーケンス |
| Effect フェーズ | ゲーム効果（ダメージ判定・Projectile 発射など）を発生させるフェーズ |
| サブタイプ ID | ActionType をさらに細分化する識別子（スペル ID など）。null = 基本アクション |

---

## 硬直の分類方針

硬直（次の行動判断をブロックすること）には 2 つのパターンがある。それぞれ適切な層で実装する。

### パターン A：AI クールダウン硬直

「何かをした後の間」であり、特定のタイミングでゲーム効果を発生させる必要がないケース。

実装：AI ポリシーが次の行動を決定する際に `MarkEvaluated(time, frame, cooldownSeconds: 1f)` を渡す。

対象：
- ルーム到達後の硬直
- アイテム拾得後の硬直
- （将来）UseStair 後の硬直など、効果タイミングを制御しないアクション全般

### パターン B：Phase システム硬直

アクション実行の途中に「ゲーム効果が発生するタイミング」があり、その前後で Actor を拘束するケース。

実装：本ドキュメントで定義する ActorActionPhase システムを使う。

対象：
- 通常攻撃（WindUp → Effect[ダメージ判定] → Recovery）
- 魔法（Casting → Effect[Projectile 発射] → Recovery）
- （将来）スキル・特殊能力で「発動タイミング」が意味を持つアクション全般

---

## 設計方針

- フェーズ定義は Master データとして外部化する。コードにフェーズ秒数をハードコードしない
- フェーズ実行の「タイミング計算」は Application 層のステートストアが担う
- ゲーム効果（ダメージ・Projectile）は UseCase が担う。イベントで UseCase を起動しない（game-event-design.md 方針遵守）
- View へのフェーズ通知はイベントバスで行う。View は Application の実装詳細を知らない
- Domain の `ActorAction` にフェーズ秒数は持たせない。Domain はアクションの「意図」を持ち、実行タイミングは Application が管理する
- Phase システムは「ゲーム効果のタイミングを制御したいアクション」にのみ適用する。単純な硬直は AI クールダウンで十分

---

## 型定義

### Master 層

```
ActorActionPhaseName (enum)
  Casting      // 魔法詠唱など：モーション・移動ロック
  WindUp       // 攻撃の振りかぶり
  Effect       // 効果発生タイミング（ダメージ判定・Projectile 生成など）
  Recovery     // モーション後の硬直
  Stagger      // 汎用硬直（ルーム到達・アイテム拾得など）

ActorActionPhaseKey (value object)
  ActionType : ActorActionType
  SubTypeId  : Guid?   // null = そのアクション種別のデフォルト定義

ActorActionPhaseDef (value object)
  PhaseName         : ActorActionPhaseName
  DurationSeconds   : float   // 0 = 即時通過（イベント発行のみ）

IActorActionPhaseMasterRepository
  bool TryGetPhaseSequence(ActorActionPhaseKey key,
                           out IReadOnlyList<ActorActionPhaseDef> phases)
```

`ActorActionPhaseKey` の複合キーで Dictionary を構築し O(1) で取得する（M2 と同パターン）。

---

### Domain 層（`ActorAction` への追加）

```
ActorAction (既存 + 変更)
  追加フィールド:
    SubTypeId : Guid?  // スペル ID など。null = 基本アクション

  追加ファクトリメソッド:
    static ActorAction Attack(int targetId, Guid? subTypeId = null)
    // 既存 Attack() を置き換え or オーバーロード追加
```

`SubTypeId` はアクションの「種類の細分化」を意味し、フェーズ定義の検索キーに使う。Domain にフェーズ秒数は持たせない。

---

### Application 層

#### ランタイムステート

```
ActorActionPhaseRuntimeState (Application 層の mutable state)
  ActorId          : Guid
  PhaseKey         : ActorActionPhaseKey   // 実行中シーケンスのキー
  Phases           : IReadOnlyList<ActorActionPhaseDef>
  CurrentPhaseIndex: int
  PhaseStartTime   : float
  // 派生プロパティ
  CurrentPhaseDef  : ActorActionPhaseDef
  IsCompleted      : bool  (= CurrentPhaseIndex >= Phases.Count)
  CurrentPhaseElapsed(float now) : float
```

#### ステートストア

```
IActorActionPhaseStateStore
  // シーケンス開始（AI が新アクションを決定した直後に呼ぶ）
  bool TryStart(Guid actorId, ActorActionPhaseKey key, float currentTime)
    → フェーズ定義が存在すれば開始して true を返す
    → 存在しなければ false（後方互換：既存の即時完了アクションはそのまま動く）

  // シーケンス実行中かどうか
  bool IsActive(Guid actorId)

  // フレームごとのフェーズ進行
  // 返り値: このフレームで各 Actor が新しいフェーズに入ったか・完了したか
  ActorActionPhaseTickResult TickAll(float currentTime)

  // アクション中断（戦闘離脱・死亡など）
  void Interrupt(Guid actorId)

ActorActionPhaseTickResult
  IReadOnlyList<ActorActionPhaseTransition> Transitions
    ActorActionPhaseTransition
      ActorId       : Guid
      Key           : ActorActionPhaseKey
      PhaseDef      : ActorActionPhaseDef   // 新しく入ったフェーズ
      IsFirstTick   : bool   // このフレームで初めてそのフェーズに入った
  IReadOnlyList<Guid> CompletedActorIds
```

`IActorActionPhaseStateStore` の実装は `GameSession` スコープに登録する。

---

### AI ブロック方針

アクティブなフェーズシーケンスが存在する Actor は AI 再評価をスキップする。

実装: `AdvanceActorAiOrchestrator.ExecuteAsync()` の対象 Actor 選別時に `phaseStateStore.IsActive(actorId)` を確認し、true なら処理をスキップする。`ActorAiRuntimeState.CooldownUntilTimeSeconds` は変更しない（フェーズ完了後に AI dirty が立てば自然に再評価される）。

---

### Events

```
ActorActionPhaseStartedEvent : IGameEvent
  ActorId    : Guid
  ActionType : ActorActionType
  SubTypeId  : Guid?
  PhaseName  : ActorActionPhaseName

ActorActionSequenceCompletedEvent : IGameEvent
  ActorId    : Guid
  ActionType : ActorActionType
  SubTypeId  : Guid?
```

イベントは game-event-design.md の方針に従い「状態変化の通知」として使う。
View のアニメーション切り替えには `ActorActionPhaseStartedEvent` を使う。
ゲームロジック（ダメージ、Projectile 生成）はイベント購読ではなく UseCase 内で実行する（下記「既存システムへの変更」参照）。

---

## 既存システムへの変更

### WorldSimulationOrchestrator

`AdvanceFrameAsync` のフレーム進行に `phaseStateStore.TickAll(currentTime)` を追加する。

呼び出し位置: AI 評価の直前（フェーズ進行 → 完了 Actor 確定 → AI 評価の順）。

```
変更後の順序（概略）:
1. phaseStateStore.TickAll(currentTime) → tickResult 取得
2. advanceActorAiOrchestrator.ExecuteAsync()
   └─ phaseStateStore.IsActive(actorId) == true の Actor はスキップ
3. advanceCombatUseCase.ExecuteAsync() (下記参照)
4. (Projectile, Pickup, Inn 回復 ... 以降は変更なし)
```

### AdvanceCombatUseCase

現行: 射程内かつ攻撃可能なら即時 `combatEffectExecutor.ExecuteAttack()` を呼ぶ。

変更後:
- 射程内かつ攻撃者に **アクティブシーケンスがない** 場合 → `phaseStateStore.TryStart(actorId, key, time)` でシーケンス開始のみ行い、このフレームはダメージを与えない
- `TickAll` の結果から「このフレームで Effect フェーズに入った攻撃者」を受け取り → `combatEffectExecutor.ExecuteAttack()` を呼ぶ

このため `AdvanceCombatUseCase` は `IActorActionPhaseStateStore` を注入し、`TickAll` の結果（または別途フェーズ判定 API）を使って Effect フェーズ到達を判定する。

> **注意**: `TickAll` の戻り値（`ActorActionPhaseTickResult`）を `AdvanceCombatUseCase` に渡すのではなく、`AdvanceCombatUseCase` 自身が `phaseStateStore` を介して現在のフェーズ状態を確認する。`WorldSimulationOrchestrator` は `TickAll` の結果をログ・デバッグ目的にのみ使う設計でよい。

### ApplyActorAiDecisionUseCase

Actor の Action が変更されたとき（`decision.NextAction != null`）、`phaseStateStore.TryStart()` を呼んでシーケンスを開始する。

既存のフェーズ定義を持たないアクション（Move, UseStair など）は `TryStart()` が false を返すため、現行と同じ即時完了として動作する。

### ActorCombatAnimationPresenter（View 層）

既存の `CombatAttackOccurred` に加えて `ActorActionPhaseStartedEvent` を購読し、フェーズ名に応じたアニメーション状態へ遷移する。

```
購読追加:
  ActorActionPhaseStartedEvent
    → PhaseName に応じて SetAnimationState を呼ぶ
      Casting   → ActorAnimationState.Casting
      WindUp    → ActorAnimationState.WindUp
      Effect    → ActorAnimationState.Attack  (既存の Attack アニメに対応)
      Recovery  → ActorAnimationState.Idle    (またはデフォルト)
      Stagger   → ActorAnimationState.Idle

  ActorActionSequenceCompletedEvent
    → アニメーション状態をデフォルトに戻す（必要に応じて）
```

`ActorAnimationState` に `Casting` / `WindUp` を追加する。

---

## フェーズ定義の例（HardcodedMasterRepository）

Phase システム（パターン B）を使うアクションのみ定義する。

```
Attack（基本攻撃、SubTypeId = null）:
  [WindUp,   0.2s]
  [Effect,   0s  ]  ← ダメージ判定
  [Recovery, 0.8s]
  合計 1.0s

MagicSpell（SubTypeId = <スペル ID A>）例:
  [Casting,        1.5s]
  [WindUp,         0.2s]
  [Effect,         0s  ]  ← Projectile 発射
  [Recovery,       1.3s]
  合計 3.0s
```

PickUpItem とルーム到達は AI クールダウン（パターン A）で対応するため、Phase システムにはフェーズ定義を持たせない。

---

## AI クールダウン硬直の実装方針（パターン A）

ルーム到達後とアイテム拾得後の硬直は、AI ポリシーが次の行動を決定するタイミングで `MarkEvaluated(time, frame, cooldownSeconds: 1f)` を渡すことで実現する。Phase システムは使わない。

### ルーム到達後

- `AdventurerAiPolicy` が ShortTerm 評価で「直前にルームへ到達した」ことを検知する
- 到達判定は `AdventurerBehavior.ExplorationRoomArrivalCount` の増分または専用 dirty flag で行う
- 到達検知時、次の行動（Wait または次の移動目標）を決定しつつ `cooldownSeconds: 1f` を返す

### アイテム拾得後

- `AdventurerAiPolicy` が ShortTerm 評価で「直前の Action が PickUpItem かつ State == Completed」を検知する
- 検知時、次の行動を決定しつつ `cooldownSeconds: 1f` を返す
- `PickUpItemUseCase` 側は変更しない

どちらも AI ポリシー内の判断として閉じており、Phase システムとの連携は不要。

---

## DI 登録

| 型 | スコープ | 登録場所 |
|---|---|---|
| `IActorActionPhaseStateStore` | GameSession | `GameSessionLifetimeScope` |
| `IActorActionPhaseMasterRepository` | GameSession | `GameSessionLifetimeScope` |

`AdvanceCombatUseCase` / `ApplyActorAiDecisionUseCase` は既に GameSession スコープにあるため追加登録は不要。

---

## スコープ外（このマイルストーン対象外）

- Magic アクション本体の実装（`ActorActionType.Magic` の追加と SpellMaster 設計）
- Projectile 発射の実装（フェーズシステムと Projectile システムの接続は Magic 実装時に行う）
- `ActorAnimationState.Casting` / `WindUp` に対応する実際のスプライト・アニメーション
- ルーム到達硬直の `Wait` フェーズ正式化（今回は `MarkEvaluated cooldown` で代替）
- フェーズシーケンスの割り込み処理の詳細（死亡時のクリーンアップは Interrupt を呼べば十分）
- フェーズ定義の ScriptableObject / Addressables 化（現在 HardcodedMasterRepository で十分）

---

## 実装タスク案

### グループ A：AI クールダウン硬直（パターン A）

既存 AI ポリシーの変更のみ。Phase システムとは独立して実装できる。

| # | タスク | 変更対象 |
|---|---|---|
| A1 | `AdventurerAiPolicy` にルーム到達検知 → `cooldownSeconds: 1f` を追加 | Application/Actors/Ai |
| A2 | `AdventurerAiPolicy` にアイテム拾得完了検知 → `cooldownSeconds: 1f` を追加 | Application/Actors/Ai |
| A3 | EditMode テスト：ルーム到達後・PickUpItem 完了後に 1 秒 AI 評価がスキップされる | Tests |

### グループ B：Phase システム（パターン B）

| # | タスク | 変更対象 |
|---|---|---|
| B1 | `ActorActionPhaseName` enum / `ActorActionPhaseDef` / `ActorActionPhaseKey` を追加 | 新規 Application/Actors/Phase |
| B2 | `ActorAction` に `SubTypeId: Guid?` を追加 | Domain/Actor/ActorAction.cs |
| B3 | `IActorActionPhaseMasterRepository` と HardcodedMasterRepository への Attack 定義追加 | Master |
| B4 | `ActorActionPhaseRuntimeState` と `IActorActionPhaseStateStore` 実装 | Application/Actors/Phase |
| B5 | `ApplyActorAiDecisionUseCase` に `TryStart` 呼び出しを追加 | Application |
| B6 | `AdvanceActorAiOrchestrator` にフェーズアクティブ Actor のスキップを追加 | Application |
| B7 | `AdvanceCombatUseCase` を Effect フェーズ駆動に変更 | Application/Combat |
| B8 | `WorldSimulationOrchestrator` に `TickAll` 呼び出しを追加 | Application/World |
| B9 | `ActorCombatAnimationPresenter` に `ActorActionPhaseStartedEvent` 購読を追加 | View |
| B10 | EditMode テスト：フェーズ進行・Effect 発火・AI ブロック・完了後 AI 再評価 | Tests |

B1〜B4 は並行可能。B5〜B8 は B4 完了後。B9 は B5〜B8 完了後。
グループ A と グループ B は独立しており、並行実装できる。
