# Combat Domain Design

このドキュメントは、Actor同士の戦闘、武器攻撃、攻撃判定、Projectile、Area、AIとの接続方針をまとめる。

## 目的

戦闘システムは、敵対勢力のActor同士が遭遇し、武器や攻撃定義に基づいてダメージや状態変化を発生させる仕組みである。

攻撃は単一の攻撃タイプではなく、複数の Combat Effect Node をイベントでつないだ定義として扱う。
これにより、剣攻撃、弓、貫通弾、グレネード、爆発、毒沼、連鎖攻撃のような複合的な攻撃を同じ枠組みで表現する。

## フォルダ方針

現在、武器攻撃力Calculatorは `Domain/Actor`、武器戦闘性能と攻撃定義は `Domain/Combat` に置く。
武器種別の基礎射程・攻撃速度は `Master/WeaponTypeCombatMaster` が持ち、装備固有の modifier は `Master/WeaponMaster` が持つ。

想定構成:

```text
Domain/
  Combat/
    WeaponCombatParams
    WeaponAttackSpec
    CombatEffectNodeSpec
    CombatEffectNodeType
    CombatEffectTriggerType
    CombatEffectLinkSpec
    DamageSpec
    AttackAreaSpec
    AttackAreaShape
    AttackAreaDurationSpec
    AttackHitIntervalType
    ProjectileSpec
    ProjectileMovementType
    ProjectileHitBehavior
    CombatEffectExecutionId
    CombatEffectGraphValidator
    IWeaponCombatCalculator
    WeaponCombatCalculators

Application/
  Combat/
    IActorCombatService       ← 実装済み
    ActorCombatService        ← 実装済み
    ActorCombatState          ← 実装済み
    AdvanceCombatResult       ← 実装済み
    CombatAttackEvent         ← 実装済み
    CombatDeathEvent          ← 実装済み
    CombatActionSelector
    CombatEffectExecutor
    AttackAreaTargetResolver
    ProjectileSimulator
    CombatTraceRecorder
    TacticalPositionEvaluator ← 遠距離戦闘用、将来実装

Master/
  WeaponTypeCombatMaster      ← 実装済み（武器種別の基礎射程・攻撃速度）
  WeaponTypeCombatMasterCatalog ← 実装済み
```

`IWeaponCalculator` は攻撃力計算、`IWeaponCombatCalculator` は射程・攻撃速度・攻撃定義の取得を担う。
攻撃力と戦闘性能は責務が異なるため、現状では分離したまま扱う。

## 戦闘開始条件

Actor同士は、以下をすべて満たすと戦闘状態に入る。

- 勢力が敵対している
- 距離が20m以内
- 同じLayer / Floor上にいる
- 視線が壁で遮られていない

戦闘開始判定は `DetectCombatEncounterUseCase` が担当する（毎フレーム実行）。
視線判定は同UseCase内の `HasLineOfSight` として実装済み。Map/Dungeonのセル情報で壁を判定する。
将来的に視線判定が複雑化した場合は `Application/Combat/CombatLineOfSightService` として独立させる。

Domainは以下を表す。

- Actorの位置
- Actorの勢力
- 勢力間の敵対関係
- 戦闘状態

Applicationは以下を判定する。

- 距離
- 視線
- 対象候補
- 戦闘参加可否

## 判定方式

Unity Collider は攻撃判定には使わない。
攻撃判定は Domain / Application 層で扱える座標計算で完結させる。

利用する情報:

- ActorのLayerPosition
- Dungeon / Map のセル情報
- 壁や遮蔽情報
- 攻撃範囲の幾何情報

高速Projectileでは、将来的にUnity ColliderのContinuous判定に相当する補間判定が必要になる可能性がある。
初期実装では無視し、必要になった段階で `ProjectileSimulator` に区間判定を追加する。

## 武器戦闘性能

武器は以下の戦闘性能を持つ。

- 攻撃力
- 射程
- 攻撃速度
- 攻撃定義 `WeaponAttackSpec`

攻撃力は既存の `WeaponAttack` 計算を利用する。
射程、攻撃速度、攻撃定義は `IWeaponCombatCalculator` から取得する。

射程と攻撃速度の解決ルール:

- 基礎値は `WeaponTypeCombatMaster`（武器種別ごとの固定値）が持つ
- 装備武器がある場合は `WeaponMaster.RangeModifierMeters` / `AttackIntervalModifierSeconds` を加算する
- 武器未装備の場合は `Actor.NaturalWeaponTypeCombatMaster` の基礎値のみ使う

想定:

```csharp
public sealed class WeaponCombatParams
{
    public int AttackPower { get; }
    public float RangeMeters { get; }
    public float AttackIntervalSeconds { get; }
    public WeaponAttackSpec AttackSpec { get; }
}
```

## Combat Effect Node

攻撃は `WeaponAttackSpec` が持つ Combat Effect Node の木またはDAGとして表現する。

Direct、Area、Projectile は攻撃タイプではなく、攻撃を構成するNodeである。

### WeaponAttackSpec

武器攻撃の定義。

想定情報:

- 攻撃定義ID
- ルートNode ID一覧
- Node一覧
- 最大生成深度

### CombatEffectNodeSpec

攻撃要素の1単位。

想定情報:

- Node ID
- Node Type
- DamageSpec
- AttackAreaSpec
- ProjectileSpec
- Links

### CombatEffectNodeType

想定値:

- DirectDamage
- Area
- Projectile
- ApplyStatus

### CombatEffectTriggerType

Nodeから別Nodeを発火するイベント。

想定値:

- OnStart
- OnHit
- OnExpired
- OnTick
- OnCompleted

### CombatEffectLinkSpec

あるNodeのイベントから別Nodeを発火するリンク。

想定情報:

- TriggerType
- TargetNodeId

例:

```text
Projectile
  OnHit -> Area
  OnExpired -> Area

Area
  OnHit -> DirectDamage
```

## Direct Damage

対象に直接影響を与えるNode。
当たり判定を無視し、選択済みの対象に対して効果を適用する。

用途:

- 確定命中攻撃
- AreaやProjectileの命中結果としてのダメージ
- 状態異常付与の前段

## Area

範囲内の対象へ影響を与えるNode。

形状:

- Rectangle
- Circle
- Fan

判定時間:

- Instant
- Duration

Duration中のヒット頻度:

- OncePerTarget
- EverySecond

Areaは、発生地点、向き、形状、範囲値、持続時間、ヒット頻度に基づいて対象を抽出する。
対象抽出は Application/Combat の `AttackAreaTargetResolver` が担当する。

## Projectile

飛翔物を生成するNode。

移動方式:

- Direction: 一定方向に一定距離進む
- TargetPoint: 指定地点に向かって進む

ヒット時挙動:

- DisappearOnHit: 当たった相手に影響を与えて消える
- Pierce: 当たった相手に影響を与えつつ消えない
- AreaOnly: Projectile自体に当たり判定はなく、周辺Areaで影響を与える

Projectileは、OnHit または OnExpired で別Nodeを発火できる。

例:

```text
GrenadeLauncher
Root:
  Projectile
    movement: TargetPoint
    onHit:
      Area
        shape: Circle
        duration: Instant
        onHit:
          DirectDamage
    onExpired:
      Area
        shape: Circle
        duration: Instant
        onHit:
          DirectDamage
```

## 攻撃例

### Sword

```text
WeaponAttackSpec: SwordSlash
Root:
  Area
    shape: Fan
    duration: Instant
    onHit:
      DirectDamage
```

### Bow

```text
WeaponAttackSpec: ArrowShot
Root:
  Projectile
    movement: Direction
    onHit:
      DirectDamage
```

### Grenade Launcher

```text
WeaponAttackSpec: GrenadeLauncher
Root:
  Projectile
    movement: TargetPoint
    onHit:
      Area
        shape: Circle
        duration: Instant
        onHit:
          DirectDamage
    onExpired:
      Area
        shape: Circle
        duration: Instant
        onHit:
          DirectDamage
```

### Poison Field

```text
WeaponAttackSpec: PoisonField
Root:
  Area
    shape: Circle
    duration: Duration
    hitInterval: EverySecond
    onHit:
      DirectDamage
      ApplyStatus
```

## Graph制約

Combat Effect Node は将来的にはグラフとして扱えるが、初期実装では木またはDAGとして扱う。

初期制約:

- Node ID は定義内で一意
- ルートNodeは定義内に存在する
- Link先Nodeは定義内に存在する
- 循環参照は禁止
- 最大生成深度を持つ
- `OnTick` は初期実装では Duration Area のみ許可

循環参照禁止は `CombatEffectGraphValidator` でValidationし、テストを追加する。

注意:

- ProjectileがAreaを生成し、AreaがProjectileを生成すること自体は将来可能にする
- ただし無限生成を防ぐため、循環禁止と最大生成深度を守る

## CombatEffectExecutionId

Node連鎖はDebugが難しいため、実行単位にTrace用IDを持つ。

想定:

```text
CombatEffectExecutionId
- Guid Value
```

用途:

- どの攻撃から発生した効果か追跡する
- Projectile -> Area -> Damage の連鎖をログで追えるようにする
- テスト時に発生順や原因を検証しやすくする

Application/Combat に `CombatTraceRecorder` を置き、必要に応じて実行ログを記録する。

## 戦闘中の移動方針

### 近接戦闘（接近型）

武器射程が短いActor（剣士、格闘家など）は、ターゲットへ直線接近する。
視線確認済みの近距離での直線移動を `AdvanceCombatUseCase` 内で実行する。
A*は不要（LOSが確認済みの近距離のため）。

### 遠距離戦闘（距離維持型）

弓使いなど射程が長いActorは、ターゲットと距離を保ちながら攻撃する。
単純な後退ではなく、**戦術的ポジション選択**を行う。

#### 戦術ポジションのスコアリング

現在地周辺のグリッドセルを候補として以下をスコアリングし、最高点のセルへA*移動する。

| 評価項目 | 意味 |
|---|---|
| LOSが通る | ターゲットへ攻撃できる |
| 敵との距離が射程に近い | 近すぎず遠すぎない位置 |
| 周囲の歩行可能セル数 | 多いほど逃げ道が広い（コーナー忌避） |

「後ろに壁がある」などで逃げ道が少ない場合は、一時的に敵に近づく方向のセルでも
「逃げ道が多いセル」が選ばれることで、自然に安全な立ち位置へ移動する。

#### ポジション再評価タイミング

以下のタイミングで戦術ポジションを再評価する。キャッシュはA*と同様に再評価まで保持する。

- ターゲット変更時
- 選択ポジションへの移動完了時
- 攻撃実行後
- 戦闘終了時（キャッシュ破棄）

#### 実装上の置き場

戦術ポジション評価は `Application/Combat` に `TacticalPositionEvaluator` として置く。
`AdvanceCombatUseCase` は移動タイプ（近接/遠距離）に応じて直線接近か戦術移動かを切り替える。

## AIとの接続

AIの短期Actionとしては `Attack` を返す。
ただし、どの武器攻撃を使うか、射程内か、Area攻撃を使うか、Projectileを撃つかはAI本体に抱え込みすぎない。

Application/Combat に `CombatActionSelector` を置く。

責務:

- Actorが利用可能な攻撃定義を取得する
- 対象が射程内か判定する
- 複数攻撃候補から現在状況に適したものを選ぶ
- AIへ「攻撃可能か」「どのActionを選ぶべきか」を返す

AIは以下のように扱う。

```text
ShortTerm AI:
  敵がいる
  -> CombatActionSelector に問い合わせる
  -> Attack Action を選ぶ

CombatActionSelector:
  現在装備
  武器戦闘性能
  対象距離
  Area候補
  Projectile候補
  -> 最適な攻撃定義を返す
```

## 責務分離

### Domain/Combat

- 戦闘定義
- 武器戦闘性能
- Effect Node定義
- Area定義
- Projectile定義
- Damage定義
- Graph Validation
- Trace ID

### Application/Combat

- 戦闘開始判定
- 視線判定
- 攻撃候補選択
- Effect Node実行
- 範囲内対象の抽出
- Projectile進行
- Damage適用
- Trace記録

### View

- Projectileの見た目
- 攻撃エフェクト
- アニメーション
- ヒット表示
- UI表示

Viewは戦闘判定を決めない。
Unity Colliderは攻撃判定には使わない。

## 初期実装順

1. `Domain/Combat` の定義型を追加する
2. `CombatEffectGraphValidator` と循環参照テストを追加する
3. `WeaponCombatParams` と `IWeaponCombatCalculator` を追加する
4. 既存 `IWeaponCalculator` から段階的に戦闘性能取得へ移行する
5. 戦闘開始判定を追加する
6. DirectDamageを実行できるようにする
7. Instant Areaを実行できるようにする
8. Projectileを実行できるようにする
9. OnHit / OnExpiredで子Nodeを発火できるようにする
10. Traceログを追加する
11. AIのAttack Actionから `CombatActionSelector` に接続する

## 懸念点と対策

### IWeaponCalculatorの責務拡大

攻撃力だけでなく射程、攻撃速度、攻撃定義まで返す必要が出る。

対策:

- `IWeaponCombatCalculator` へ分離する
- 既存 `IWeaponCalculator` は段階的に移行する

### Domain/Combatの型が増える

複雑な攻撃定義には多くの型が必要になる。

対策:

- Domainには定義とValidationのみ置く
- Runtime実行や対象抽出はApplication/Combatへ置く

### 無限生成

Nodeが相互に生成し合うと無限ループになる。

対策:

- 初期実装ではDAGのみ許可
- 循環参照Validationを行う
- 最大生成深度を持つ

### Debug困難

Projectile、Area、Damageが連鎖すると原因追跡が難しい。

対策:

- `CombatEffectExecutionId` を持つ
- `CombatTraceRecorder` で実行履歴を記録する

### AIが戦闘詳細を抱え込みすぎる

AIが攻撃定義やArea選択まで直接判断すると肥大化する。

対策:

- `CombatActionSelector` をApplication/Combatに置く
- AIは「攻撃するか」を決め、戦闘詳細はCombat側へ委譲する
