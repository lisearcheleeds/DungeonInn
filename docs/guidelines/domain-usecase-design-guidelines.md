# Domain / UseCase Design Guidelines

このドキュメントは、Clean Architecture で Domain / Application UseCase を設計・レビューするときの汎用的な判断基準をまとめる。
特定プロジェクト専用の実装手順ではなく、別プロジェクトでも同じ設計判断を再現するための指針とする。

DungeonInn での Actor / Behavior / Equipment / WeaponCalculator の設計は、この指針を適用した具体例の1つとして扱う。

## 基本方針

Domain は、業務・ゲーム・プロダクト固有の不変条件、状態、状態遷移を表現する。
Application UseCase は、Domain を組み合わせてユースケースの手順、入力の適用、外部境界との受け渡し、意思決定の流れを実行する。

Domain は View、Infrastructure、Framework、外部SDKに依存しない。
UseCase も原則として Domain の組み合わせに留め、表示都合や永続化都合を Domain Entity に持ち込まない。

## 命名規約: Master / Spec / Params / State

マスタデータと、それを組み合わせて生成される実行時用の値は名前で役割を分ける。
特に MasterMemory のようなマスタ管理基盤を使う場合、テーブルの行と1対1で対応する型と、複数のマスタや文脈から構築される型を混同しない。

- `Master`: マスタデータの1レコードと1対1で対応する型。保存・ロード・参照の単位であり、原則として不変に扱う。
- `Spec`: Master、固定値、文脈を束ねて作る不変の仕様値。グラフ、攻撃効果、生成設定など、実行時に参照する構造化済みデータに使う。
- `Params`: Stats、Equipment、Behavior、Buff などから計算された現在値。キャッシュしてよいが、更新経路を明確にする。
- `State`: HP、位置、予約状態、AI Dirty など、時間経過や行動で変化する実行時状態。
- `Policy` / `Calculator`: 種類や文脈ごとの判断・計算式。Entity に式や分岐を集めないために使う。

### Before

```csharp
public sealed class ItemDefinition
{
}

public sealed class WeaponAttackDefinition
{
}
```

`Definition` は意味が広く、マスタデータそのものなのか、複数のマスタから構築された実行時仕様なのかが名前から判断しにくい。

### After

```csharp
public sealed class ItemMaster
{
}

public sealed class WeaponAttackSpec
{
}
```

`ItemMaster` はアイテムマスタの1行、`WeaponAttackSpec` は武器や攻撃効果ノードから構築された攻撃仕様を表す。
この境界を名前で固定すると、MasterMemory への移行時もデータロード層と Domain の責務を分けやすい。

### DungeonInn Example

DungeonInn では `ItemMaster`、`EquipmentMaster`、`WeaponMaster`、`ActorArchetypeMaster`、`SpeciesMaster`、`AdventurerSpawnMaster` をマスタデータ相当として扱う。
一方、`WeaponAttackSpec`、`CombatEffectNodeSpec`、`CombatEffectLinkSpec`、`DamageSpec`、`AttackAreaSpec`、`ProjectileSpec` は戦闘処理で参照する不変仕様として扱う。
将来的に `ConsumableMaster` などを追加する場合も、マスタ1行に対応する型は `Master`、複数マスタを束ねた実行時仕様は `Spec` に寄せる。
DungeonInn のようにマスタが Domain Entity ではなく参照データである場合、`Domain/` ではなく `Master/` などの専用フォルダ・名前空間に置き、Repository から UseCase / オーケストレーションへ供給する。
MasterMemory 導入前は、`HardcodedMasterRepository` のような仮 Repository で同じ読み取り契約を満たす。

## マスタ参照値を Runtime Instance に複製しない

Runtime Instance / State / Entity がマスタ行を参照する場合、原則として保持するのは `XxxMasterId` のみとする。
`Name`、`DisplayName`、`ReapplyPolicy`、固定 Duration、固定 Amount、Category など、マスタから O(1) で参照できる不変値を Instance 側へコピーしてはならない。

マスタリポジトリは `Id -> Master` の辞書参照を提供できる前提で設計する。
したがって「参照のために毎回マスタを引くのが面倒」「表示名をすぐ使いたい」という理由で、マスタ由来値を Runtime Instance に重複保持しない。

### Before

```csharp
public sealed class ActorEffectInstance
{
    public int ActorEffectMasterId { get; }
    public string DisplayName { get; }                 // NG: ActorEffectMaster.Name から引ける
    public ActorEffectReapplyPolicy ReapplyPolicy { get; } // NG: ActorEffectMaster.ReapplyPolicy から引ける
}
```

### After

```csharp
public sealed class ActorEffectInstance
{
    public int ActorEffectMasterId { get; }
    public float ElapsedSeconds { get; private set; }  // OK: 実行時に変化する状態
}
```

表示や再付与判断が必要な UseCase / Presenter は、`ActorEffectMasterId` から `ActorEffectMaster` を引いて `Name` や `ReapplyPolicy` を参照する。

### 例外

以下の場合のみ、マスタ由来値のスナップショット保持を検討してよい。

- 履歴・ログ・リプレイ・セーブ互換のため、後でマスタが変わっても当時の値を固定したい
- 生成時に複数のマスタや乱数から確定した、個体固有の値として扱う
- マスタではなくプレイヤー操作で変更される現在値として扱う

例外として保持する場合は、フィールド名やコメントで「スナップショット」「現在値」「個体固有値」であることを明示する。
単なるマスタ参照値のキャッシュとして追加してはならない。

## 1. Entity は分類ではなく状態と振る舞いを持つ

同じ個体が時間経過や操作によって役割を変える可能性がある場合、継承で役割を固定しない。
Entity は個体の継続性を表し、役割や振る舞いは Behavior、State、Policy などの合成で表す。

### Before

```csharp
public sealed class Customer : Person
{
}

public sealed class Staff : Person
{
}
```

この形では、同じ人物が顧客からスタッフになる、スタッフが一時的に別ロールを持つ、といった変化で型そのものが変わる。
ID、履歴、所持品、設定値などを維持したまま役割だけを変えにくい。

### After

```csharp
public sealed class Actor
{
    public IActorBehavior Behavior { get; private set; }

    public void ChangeBehavior(IActorBehavior behavior)
    {
        Behavior = behavior;
        RefreshParams();
    }
}

public sealed class CustomerBehavior : IActorBehavior
{
}

public sealed class StaffBehavior : IActorBehavior
{
}
```

Entity は個体そのものを表し、Behavior は現在の振る舞いを表す。
役割変更は Entity を作り直さず、Behavior の差し替えで表現する。

### DungeonInn Example

DungeonInn では `Adventurer` / `Monster` / `GuildStaff` を継承クラスとして分けず、`Actor + IActorBehavior` で表現する。
これにより、冒険者をギルドスタッフ化する、モンスターを Pet 化する、といった変化で同じ Actor を維持できる。

## 2. 導出できる識別情報を二重に持たない

実体型や保持データから導ける情報を、bool や enum として重複保持しない。
重複した識別情報は差異が発生し、設計と実装の整合性を壊す。

enum 自体は、保存データ、外部入力、Factory 入力など境界の識別値として使ってよい。
ただし Runtime Object の中で、実体型と同じ意味の enum を重複保持しない。

### Before

```csharp
public interface IBehavior
{
    BehaviorType Type { get; }
}

public sealed class CustomerBehavior : IBehavior
{
    public BehaviorType Type => BehaviorType.Customer;
}
```

`CustomerBehavior` という実体型と `BehaviorType.Customer` が同じ意味を二重に表している。

### After

```csharp
public interface IBehavior
{
}

if (actor.Behavior is CustomerBehavior)
{
    ...
}
```

実体型を見ればよい場面では実体型を見る。
`BehaviorType` はセーブデータや外部入力から Behavior を生成するために残す。

### DungeonInn Example

`IActorBehavior` に `ActorBehaviorType Type` を持たせない。
`ActorBehaviorType` は保存・生成用の識別値として残し、実行時の Behavior には重複保持しない。

## 3. Entity に計算式を集めすぎない

Entity は状態の集約点であり、すべての計算式の置き場ではない。
複数の入力から導出される値や、種類ごとに式が変わる値は Calculator / Policy に分離する。

### Before

```csharp
public sealed class Actor
{
    public int CalculateMaxHp()
    {
        return Stats.Constitution * 10 + Stats.Strength * 2 + Level * 5;
    }

    public int CalculateSwordAttack()
    {
        return Stats.Strength * 3 + Stats.Dexterity + Level;
    }
}
```

Entity が基礎能力、装備、武器種、バフ、状態などの計算責務を抱え込む。

### After

```csharp
public sealed class ActorParamCalculator
{
    public ActorParams Calculate(
        ActorStats stats,
        IReadOnlyList<EquipmentMaster> equipmentMasters,
        IActorBehavior behavior,
        int level)
    {
        ...
    }
}

public interface IWeaponCalculator
{
    int CalculateAttack(
        ActorStats stats,
        WeaponMaster weaponMaster,
        EquipmentMaster weaponEquipmentMaster,
        IActorBehavior behavior,
        int level);
}
```

Entity は計算結果を保持してもよいが、式そのものは専用クラスに置く。
種類ごとに式が変わるものは、種類別 Calculator に分ける。

### DungeonInn Example

`Actor` から `CalculateMaxHp()` や `SwordAttack` / `BowAttack` を外し、共通パラメータは `ActorParamCalculator`、武器攻撃力は `IWeaponCalculator` 実装へ分離する。

## 4. キャッシュを持つなら更新経路を絞る

Entity が `Params`、`Score`、`ComputedState` のようなキャッシュを持つ場合、キャッシュに影響する状態変更は Entity 経由に集約する。
外部から内部状態を直接変更できる設計は避ける。

### Before

```csharp
actor.Stats.Increase(1, 0, 0, 0, 0, 0);
actor.Equipment.Equip(sword);
```

Stats や Equipment が直接変更されると、Actor 側の `Params` や `AttackPower` が古くなる。

### After

```csharp
actor.IncreaseStats(1, 0, 0, 0, 0, 0);
actor.Equip(sword);
```

Entity 経由で変更し、変更後にキャッシュ再計算や Calculator 再選択を行う。

### DungeonInn Example

`ActorStats` は不変値として扱い、能力値変更は `Actor.IncreaseStats()` 経由で行う。
装備変更は `Actor.Equip()` / `Actor.Unequip()` 経由で行い、`ActorParams` と `WeaponAttack` を同期させる。

## 5. AI判断・業務判断と Domain 不変条件を分ける

Domain は「不正な状態を作れないこと」を守る。
UseCase、AI、Application Service は「いつ、どれを選ぶか」を決める。

Domain に判断を入れすぎると、将来のゲームデザイン変更や業務ルール変更で表現が狭くなる。
ただし、構造上あり得ない状態は Domain で禁止する。

### Before

```csharp
public void Equip(EquipmentMaster equipment)
{
    if (equipment.ItemId == RareSwordId && Level < 10)
    {
        throw new InvalidOperationException();
    }
}
```

推奨レベル未満の装備を本当に禁止するかはプロダクトルール次第。
AI が避けるだけでよい場合、Domain Validation に入れると表現が狭くなる。

### After

```csharp
public EquipmentMaster(
    int itemId,
    EquipmentSlot slot)
{
    if (slot == EquipmentSlot.None)
    {
        throw new ArgumentException("Equipment slot is required.", nameof(slot));
    }
}
```

装備なのに装備スロットがない、という構造上の不正は Domain で禁止する。
どの武器を選ぶか、推奨レベルを守るかは UseCase / AI 側で判断する。
武器種や攻撃力のような武器固有情報は `WeaponMaster` に分離する。

### DungeonInn Example

冒険者がダンジョン探索前にどの装備を Equip するかは AI / UseCase の責務。
モンスターがスポーン時にどの武器を Equip するかも、種族情報とスポーン文脈を見た UseCase / Policy の責務。
Actor Factory に判断を寄せすぎない。

## 6. 種類ごとの式は Calculator / Policy に分ける

武器、料金、報酬、探索目的、購買プリセット、施設ポイントなど、種類ごとに式や判断が変わるものは Entity の `switch` に集めすぎない。
専用の Calculator / Policy として分ける。

### Before

```csharp
public int CalculateAttack()
{
    switch (WeaponType)
    {
        case WeaponType.Sword:
            return Stats.Strength * 3 + Stats.Dexterity;
        case WeaponType.Bow:
            return Stats.Dexterity * 3 + Stats.Strength;
        default:
            return Stats.Strength;
    }
}
```

Entity が種類ごとの式を知りすぎている。

### After

```csharp
public sealed class SwordWeaponCalculator : IWeaponCalculator
{
    public int CalculateAttack(
        ActorStats stats,
        WeaponMaster weaponMaster,
        EquipmentMaster weaponEquipmentMaster,
        IActorBehavior behavior,
        int level)
    {
        return stats.Strength * 3 + stats.Dexterity + level + weaponMaster.Attack;
    }
}
```

Entity は現在使う Calculator / Policy を保持するか、必要な場面で UseCase から適用する。

### DungeonInn Example

`SwordWeaponCalculator`、`BowWeaponCalculator`、`AxeWeaponCalculator`、`ClawsWeaponCalculator` など、武器種ごとに攻撃力Calculatorを分ける。

## 7. UseCase は Domain を組み合わせる

UseCase は Domain Entity を操作してよいが、Domain 不変条件を壊さない経路を使う。
UseCase が担うのは、入力の組み合わせ、処理順序、取引記録、AI判断の適用、境界との受け渡しである。

### Before

```csharp
candidate.Role = Role.Staff;
candidate.ScoutCost = null;
```

Role や ScoutCost のような状態を直接持たせると、現在の役割と一時的な計算結果が混ざる。

### After

```csharp
var cost = await calculateCostUseCase.ExecuteAsync(candidate);
organization.Inventory.RemoveRange(cost);
candidate.ChangeBehavior(new StaffBehavior(salary));
organization.RecordTransaction(...);
```

一時的な費用は現在値から逐次計算する。
採用結果は Behavior の変更として表す。
取引履歴は UseCase が記録する。

### DungeonInn Example

スカウト費用は `ScoutCost` として Actor に保持しない。
`CalculateScoutCostUseCase` で逐次計算し、`RecruitStaffUseCase` が支払い、Behavior変更、取引記録を行う。

## 8. Factory に判断を抱え込ませすぎない

Factory は生成の整合性を担うが、AI判断、ユーザー操作、スポーン文脈、業務フローまで抱え込ませすぎない。
生成後に UseCase / Policy が文脈を見て状態を整える方が自然な場合がある。

### Before

```csharp
public static Actor CreateEnemy(EnemySpecies species)
{
    var actor = new Actor(...);
    actor.Equip(species.DefaultWeapon);
    actor.ChangeBehavior(new AggressiveBehavior());
    return actor;
}
```

Factory が生成だけでなく、スポーン文脈やAI判断まで抱える。

### After

```csharp
var actor = actorFactory.Create(species.BaseProfile);
var weapon = equipmentPolicy.SelectWeapon(species, spawnContext);
actor.Equip(weapon);
actor.ChangeBehavior(behaviorPolicy.SelectBehavior(species, spawnContext));
```

Factory は最低限の生成に留め、文脈依存の判断は UseCase / Policy が担う。

## 9. 将来の拡張点を名前や継承で固定しない

将来、種別追加、状態追加、役割変更、外部連携追加が想定される概念は、クラス名や継承で固定しすぎない。
Behavior、Faction、Master、Spec、Policy、Calculator などへ分解し、差し替え可能にする。

### Before

```csharp
public sealed class Monster : Actor
{
}
```

Monster が将来、種族別クラス、Pet、NPC、別勢力などへ分かれると型の意味が曖昧になる。

### After

```csharp
public sealed class MonsterBehavior : IActorBehavior
{
    public int SpeciesId { get; }
}

public sealed class Faction
{
    public IReadOnlyList<FactionRelation> Relations { get; }
}
```

種族、勢力、振る舞いを分けて表現する。
Entity 本体は個体として維持する。

## 10. UseCase はトランザクション境界であり、イベントは事後通知である

1つの UseCase 実行は1つのトランザクションである。
「読み取る → ルールを適用する → 状態を変える → 通知する」の一連を完結させる責務を持つ。

状態変化をイベントで連鎖させない。
「攻撃イベントを受け取ってダメージを計算し、ダメージイベントを発行してHP減算する」という設計は
処理の順序や整合性がイベント購読順に依存してしまう。

### Before（イベント連鎖）

```text
AttackUseCase → Publish(AttackEvent)
  ↓ subscribe
DamageUseCase → Publish(DamageEvent)
  ↓ subscribe
HpReduceUseCase → HP減算
```

購読順がゲームロジックの正しさを決める。整合性の保証がない。

### After（UseCase内で完結）

```text
AdvanceCombatUseCase
  ├─ ダメージ計算
  ├─ HP減算                ← 状態変化はここで完結
  ├─ Publish(AttackOccurred)  ← 事後通知
  └─ Publish(ActorDefeated)   ← 事後通知
```

イベントバスへの Publish は UseCase の処理が完了した後の通知専用とする。
購読者はゲームの状態を変更しない。

### DungeonInn Example

`AdvanceCombatUseCase` がダメージ計算・HP減算・死亡判定をすべて完結させ、
完了後に `CombatAttackOccurred` / `ActorDefeated` を `IGameEventBus` に Publish する。
`AdventurerBattleRecord`（戦績記録）や `CombatLogPresenter`（UI表示）はこれを購読するが、
どちらもゲームの状態（Actor の HP 等）を変更しない。

## レビュー用チェックリスト

- Entity に `IsXxx` のような分類 bool が増えていないか
- 実体型から導ける enum / Type が Runtime Object に重複保持されていないか
- Entity に種類別の計算式が集まりすぎていないか
- キャッシュを持つ場合、キャッシュに影響する変更経路が Entity 経由に集約されているか
- Domain Validation が「不変条件」ではなく「AI判断」や「業務判断」まで禁止していないか
- UseCase が Domain の内部状態を迂回して変更していないか
- イベントバスへの Publish が UseCase の処理完了後に行われているか（事前・途中でないか）
- 購読者がゲームの状態を変更していないか
- Factory が文脈依存の判断まで抱え込みすぎていないか
- 将来の Behavior / Faction / Species / Policy 追加で既存クラス名が破綻しないか
- View / Infrastructure / Framework の都合が Domain に入り込んでいないか
