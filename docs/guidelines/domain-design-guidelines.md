# Domain 設計ガイドライン

このドキュメントは、Domain 層の Entity・値型・Calculator・Factory・DTO を設計・レビューするときの判断基準をまとめる。
特定プロジェクト専用の実装手順ではなく、別プロジェクトでも同じ設計判断を再現するための指針とする。

Domain は、業務・ゲーム・プロダクト固有の不変条件、状態、状態遷移を表現する。
Domain は View、Infrastructure、Framework、外部 SDK に依存しない。

---

## ハードゲート

この節は即時停止・修正が必要な禁止事項を列挙する。
この節に載っていない実装が自動的に許可されるわけではなく、本文の設計方針・判断基準に反する場合もレビュー指摘または作業停止対象とする。

- [ ] Domain 層から View / Infrastructure / Framework / 外部 SDK に依存していない
- [ ] Domain 層に DI フレームワーク依存を持ち込んでいない
- [ ] Runtime Instance / State / Entity に、マスタから O(1) で引ける不変値をコピー保持していない
- [ ] Runtime Object に、実体型から導ける enum / Type / bool 分類を重複保持していない
- [ ] Domain Validation で AI 判断・業務判断・ゲームデザイン上の選択まで禁止していない
- [ ] 既存の値型・構造体で表現できる同一概念を、別フィールド・別 DTO として重複定義していない
- [ ] Factory に AI 判断・ユーザー操作・スポーン文脈・業務フローを抱え込ませていない
- [ ] Domain Entity から static Catalog / Registry / Locator に依存していない
- [ ] 状態を持たない Calculator / Policy をメソッド呼び出しごとに `new` していない
- [ ] Domain 層の Calculator / Policy を DI 注入対象にしていない

## 完了前チェックリスト

このチェックリストは本文の設計方針を省略するためのものではない。
実装・レビュー時は本文を確認したうえで、最後に確認漏れを防ぐ目的で使用する。

- [ ] `Master` / `Spec` / `Params` / `State` / `Policy` / `Calculator` の命名が責務と一致している
- [ ] Entity は分類ではなく個体の状態と振る舞いを表している
- [ ] キャッシュ値を持つ場合、更新経路が Entity 経由に集約されている
- [ ] 種類ごとに変わる式や判断が Calculator / Policy に分離されている
- [ ] 新しいフィールドが「本質的な属性」か「文脈依存の属性」か確認した
- [ ] DTO は現在値・履歴・集計途中の責務で分かれている
- [ ] 並列 Factory / Request 構造を作る前に共通基盤で表現できないか確認した
- [ ] 純粋計算クラスの static 化・共有インスタンス化・DI 注入の選択理由が本文の優先順位に沿っている

---

## 命名規約: Master / Spec / Params / State

マスタデータと、それを組み合わせて生成される実行時用の値は名前で役割を分ける。

- `EntityId`: 主要 Entity の ID 型はプロジェクト単位で統一する（`Guid` 統一、または `int` + Value Object 等）。型が混在すると Entity をまたぐ変換ロジックが各境界に発生する。
- `Master`: マスタデータの1レコードと1対1で対応する型。保存・ロード・参照の単位であり、原則として不変に扱う。
- `Spec`: Master、固定値、文脈を束ねて作る不変の仕様値。グラフ、攻撃効果、生成設定など、実行時に参照する構造化済みデータに使う。
- `Params`: Stats、Equipment、Behavior、Buff などから計算された現在値。キャッシュしてよいが、更新経路を明確にする。
- `State`: HP、位置、予約状態、AI Dirty など、時間経過や行動で変化する実行時状態。
- `Policy` / `Calculator`: 種類や文脈ごとの判断・計算式。Entity に式や分岐を集めないために使う。

### Before

```csharp
public sealed class ItemDefinition { }
public sealed class WeaponAttackDefinition { }
```

`Definition` は意味が広く、マスタデータそのものなのか、複数のマスタから構築された実行時仕様なのかが名前から判断しにくい。

### After

```csharp
public sealed class ItemMaster { }
public sealed class WeaponAttackSpec { }
```

`ItemMaster` はアイテムマスタの1行、`WeaponAttackSpec` は武器や攻撃効果ノードから構築された攻撃仕様を表す。
この境界を名前で固定すると、マスタ管理基盤への移行時もデータロード層と Domain の責務を分けやすい。

### DungeonInn Example

`ItemMaster`、`EquipmentMaster`、`WeaponMaster`、`ActorArchetypeMaster`、`SpeciesMaster` をマスタデータ相当として扱う。
`WeaponAttackSpec`、`CombatEffectNodeSpec`、`DamageSpec` は戦闘処理で参照する不変仕様として扱う。
マスタが Domain Entity ではなく参照データである場合、`Domain/` ではなく `Master/` などの専用名前空間に置き、Repository から UseCase へ供給する。
MasterMemory 導入前は、`HardcodedMasterRepository` のような仮 Repository で同じ読み取り契約を満たす。

---

## マスタ参照値を Runtime Instance に複製しない

Runtime Instance / State / Entity がマスタ行を参照する場合、原則として保持するのは `XxxMasterId` のみとする。
`Name`、`DisplayName`、`ReapplyPolicy`、固定 Duration、固定 Amount、Category など、マスタから O(1) で参照できる不変値を Instance 側へコピーしない。

マスタリポジトリは `Id -> Master` の辞書参照を提供できる前提で設計する。

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

表示や再付与判断が必要な UseCase / Presenter は、`ActorEffectMasterId` から Master を引いて値を参照する。

### 例外

以下の場合のみ、マスタ由来値のスナップショット保持を検討してよい。

- 履歴・ログ・リプレイ・セーブ互換のため、後でマスタが変わっても当時の値を固定したい
- 生成時に複数のマスタや乱数から確定した、個体固有の値として扱う
- マスタではなくプレイヤー操作で変更される現在値として扱う

例外として保持する場合は、フィールド名やコメントで「スナップショット」「現在値」「個体固有値」であることを明示する。

---

## 1. Entity は分類ではなく状態と振る舞いを持つ

同じ個体が時間経過や操作によって役割を変える可能性がある場合、継承で役割を固定しない。
Entity は個体の継続性を表し、役割や振る舞いは Behavior、State、Policy などの合成で表す。

### Before

```csharp
public sealed class Customer : Person { }
public sealed class Staff : Person { }
```

同じ人物が顧客からスタッフになる、スタッフが一時的に別ロールを持つ、といった変化で型そのものが変わる。

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

public sealed class CustomerBehavior : IActorBehavior { }
public sealed class StaffBehavior : IActorBehavior { }
```

Entity は個体そのものを表し、Behavior は現在の振る舞いを表す。役割変更は Behavior の差し替えで表現する。

### DungeonInn Example

`Adventurer` / `Monster` / `GuildStaff` を継承クラスとして分けず、`Actor + IActorBehavior` で表現する。
これにより、冒険者をギルドスタッフ化する、モンスターを Pet 化する、といった変化で同じ Actor を維持できる。

---

## 2. 導出できる識別情報を二重に持たない

実体型や保持データから導ける情報を、bool や enum として重複保持しない。

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
public interface IBehavior { }

if (actor.Behavior is CustomerBehavior) { ... }
```

実体型を見ればよい場面では実体型を見る。
`BehaviorType` はセーブデータや外部入力から Behavior を生成するために残す。

### DungeonInn Example

`IActorBehavior` に `ActorBehaviorType Type` を持たせない。
`ActorBehaviorType` は保存・生成用の識別値として残し、実行時の Behavior には重複保持しない。
Behavior ごとの固有処理は `ActorBehaviorType` による分岐ではなく、`IActorBehavior` の polymorphic hook で扱う。例えば回復時に冒険者だけがストレスを減らす場合、`Actor` は `Behavior.OnRecovered(...)` だけを呼び、`AdventurerBehavior` 側でストレス処理を実装する。

---

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

### After

```csharp
public sealed class ActorParamCalculator
{
    public ActorParams Calculate(
        ActorStats stats,
        IReadOnlyList<EquipmentMaster> equipmentMasters,
        IActorBehavior behavior,
        int level) { ... }
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

### DungeonInn Example

`Actor` から `CalculateMaxHp()` や武器攻撃力計算を外し、共通パラメータは `ActorParamCalculator`、武器攻撃力は `IWeaponCalculator` 実装へ分離する。

---

## 4. キャッシュを持つなら更新経路を絞る

Entity が `Params`、`Score`、`ComputedState` のようなキャッシュを持つ場合、キャッシュに影響する状態変更は Entity 経由に集約する。
外部から内部状態を直接変更できる設計は避ける。

### Before

```csharp
actor.Stats.Increase(1, 0, 0, 0, 0, 0);
actor.Equipment.Equip(sword);
// Actor 側の Params や AttackPower が古くなる
```

### After

```csharp
actor.IncreaseStats(1, 0, 0, 0, 0, 0);
actor.Equip(sword);
// Entity 経由で変更し、変更後にキャッシュ再計算
```

### DungeonInn Example

`ActorStats` は不変値として扱い、能力値変更は `Actor.IncreaseStats()` 経由で行う。
装備変更は `Actor.Equip()` / `Actor.Unequip()` 経由で行い、`ActorParams` と `WeaponAttack` を同期させる。

---

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
        throw new InvalidOperationException();
}
```

推奨レベル未満の装備を本当に禁止するかはプロダクトルール次第。AI が避けるだけでよい場合、Domain Validation に入れると表現が狭くなる。

### After

```csharp
public EquipmentMaster(int itemId, EquipmentSlot slot)
{
    if (slot == EquipmentSlot.None)
        throw new ArgumentException("Equipment slot is required.", nameof(slot));
}
```

装備なのに装備スロットがない、という構造上の不正は Domain で禁止する。
どの武器を選ぶか、推奨レベルを守るかは UseCase / AI 側で判断する。

### DungeonInn Example

冒険者がダンジョン探索前にどの装備を Equip するかは AI / UseCase の責務。
モンスターがスポーン時にどの武器を Equip するかも、種族情報とスポーン文脈を見た UseCase / Policy の責務。

---

## 6. 種類ごとの式は Calculator / Policy に分ける

武器、料金、報酬、探索目的、購買プリセット、施設ポイントなど、種類ごとに式や判断が変わるものは Entity の `switch` に集めすぎない。

### Before

```csharp
public int CalculateAttack()
{
    switch (WeaponType)
    {
        case WeaponType.Sword: return Stats.Strength * 3 + Stats.Dexterity;
        case WeaponType.Bow:   return Stats.Dexterity * 3 + Stats.Strength;
        default:               return Stats.Strength;
    }
}
```

### After

```csharp
public sealed class SwordWeaponCalculator : IWeaponCalculator
{
    public int CalculateAttack(
        ActorStats stats, WeaponMaster weaponMaster,
        EquipmentMaster weaponEquipmentMaster, IActorBehavior behavior, int level)
    {
        return stats.Strength * 3 + stats.Dexterity + level + weaponMaster.Attack;
    }
}
```

Entity は現在使う Calculator / Policy を保持するか、必要な場面で UseCase から適用する。

### DungeonInn Example

`SwordWeaponCalculator`、`BowWeaponCalculator`、`AxeWeaponCalculator` など、武器種ごとに攻撃力 Calculator を分ける。

---

## 7. Factory に判断を抱え込ませすぎない

Factory は生成の整合性を担うが、AI判断、ユーザー操作、スポーン文脈、業務フローまで抱え込ませすぎない。

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

### After

```csharp
var actor = actorFactory.Create(species.BaseProfile);
var weapon = equipmentPolicy.SelectWeapon(species, spawnContext);
actor.Equip(weapon);
actor.ChangeBehavior(behaviorPolicy.SelectBehavior(species, spawnContext));
```

Factory は最低限の生成に留め、文脈依存の判断は UseCase / Policy が担う。

---

## 8. 将来の拡張点を名前や継承で固定しない

将来、種別追加、状態追加、役割変更、外部連携追加が想定される概念は、クラス名や継承で固定しすぎない。

### Before

```csharp
public sealed class Monster : Actor { }
```

### After

```csharp
public sealed class MonsterBehavior : IActorBehavior { public int SpeciesId { get; } }
public sealed class Faction { public IReadOnlyList<FactionRelation> Relations { get; } }
```

Entity 本体は個体として維持し、種族・勢力・振る舞いを分けて表現する。

---

## 9. 概念が重複する型を作らない。既存の値型を再利用する

同じ概念（アイテムID ＋ 個数など）が複数の型に分散すると、フィールド名のゆれや変換ロジックが生まれる。
既存の値型で表現できるものは新しいフィールドを作らず、その型を組み合わせる。

### Before

```csharp
public sealed class ItemInstance
{
    public Guid InstanceId { get; }
    public int ItemId { get; }     // ItemStack.ItemId と同じ概念
    public int Amount { get; }     // ItemStack.Count と同じ概念（命名もゆれている）
    public LayerPosition Position { get; }
}
```

### After

```csharp
public sealed class ItemInstance
{
    public Guid InstanceId { get; }
    public ItemStack Stack { get; }     // 既存の値型をそのまま使う
    public LayerPosition Position { get; }
}
```

`ItemInstance` は「ワールドに置かれた ItemStack のエンティティ」として意味が明確になる。

### 適用基準

- 新しいフィールドを追加する前に「同じ概念を表す値型・構造体が既にないか」を確認する
- 値型（struct）は Identity を持たない「量・仕様・状態のスナップショット」として再利用しやすい

---

## 10. DTO は「現在値・履歴・集計途中」の責務で分ける

似たフィールドを持つ DTO が複数存在すると、どれが正典か分かりにくくなる。
詰め替えコードが増え、片方だけ更新されるリスクもある。

DTO は責務で分類し、名前でその役割を明示する。

| 種別 | 役割 | 命名目安 |
|---|---|---|
| Current Status | 現在表示する状態 | `XxxStatus` |
| Report / History | 過去の記録として保存する不変データ | `XxxReport` / `XxxRecord` |
| Statistics / Accumulator | 集計途中の内部状態 | `XxxStatistics` / `XxxAccumulator` |
| Summary | 複数 DTO の共通フィールドをまとめた値型 | `XxxSummary` |

```csharp
// Before: 同じ意味のフィールドが2つの DTO に重複している
public readonly struct EconomyStatus { public int GuestsToday; public int SalesToday; }
public readonly struct DailyReport   { public int Guests;       public int Sales; }

// After: 共通フィールドを Summary に切り出す
public readonly struct EconomySummary { public int Guests; public int Sales; }
public readonly struct EconomyStatus  { public EconomySummary Today; public int TrendDelta; }
public readonly struct DailyReport    { public EconomySummary Result; public DateOnly Date; }
```

### 適用基準

- 同じ意味のフィールドが複数 DTO に重複していないか確認する
- 現在値・履歴・集計途中の責務が名前と構造から分かるようにする

### DungeonInn Example

`InnEconomyStatus`（現在表示）・`InnDailyReport`（履歴記録）・`InnEconomyStatistics`（集計途中）が
類似フィールドを持っており、`InnEconomySummary` のような共通値型を切り出すことで重複を減らせる。

---

## 11. データ設計の疑問は「概念レベル」から問う

コードの正しさを問う前に、データモデルの概念的な正しさを問う。

```
この属性は...
  ├─ この型の本質的な属性か？ → その型に持たせる
  └─ 特定の文脈でのみ意味を持つ属性か？ → 文脈を持つ別の型に持たせる
```

### 例：アイテムと座標

```
「Item は座標を持つか？」
  → Item それ自体は座標を持たない（インベントリにある Item もショップにある Item も同じ Item）
  → 座標は「ワールドにドロップされている」という文脈に固有の属性

結論：座標は ItemInstance（ワールドに存在するアイテムのエンティティ）が持つ
```

### 例：ItemStack と ItemInstance の概念整理

```
「ItemInstance に ItemId と Amount を持たせているが、ItemStack と概念が重複していないか？」
  → ItemStack =「アイテムID ＋ 個数」という純粋な値（文脈なし）
  → ItemInstance =「その ItemStack がワールドに存在している状態」（文脈あり）

結論：ItemInstance は ItemStack を内包する構造にする
```

### 適用基準

- 新しいフィールドを追加する前に「この属性は、この型の本質か、それとも文脈か」を自問する
- 「文脈依存の属性を本質に入れた型」は、文脈が変わるたびにフィールドが増え肥大化する
- 逆に「本質的な属性を別の型に切り出した型」は、再利用性が高まり命名も明確になる
- 設計に迷いがある場合は「この型を別の文脈でも使うとしたら、このフィールドは自然か」で確認する

---

## 12. 並列 Factory / Request 構造は共通基盤を先に確認する

Entity 種別ごとに Factory / Request を並列に作ると、差分が少ない場合に重複構造になる。

型別 Factory / Request を作る前に、共通基盤と差分フィールドで表現できないか確認する。

| 状況 | 対応 |
|---|---|
| 2つのクラスが全フィールド同一 | 1つに統合する |
| 上層 interface が別で実装が共通 | 共通 interface に統一し、差分は optional field で表す |
| 差異が1フィールドのみ | 共通 request に optional field を持たせ、Factory 内の型判断を不要にする |
| 仕様上の意味が明確に異なる | 分離を維持し、コメントで理由を残す |

```csharp
// Before: 差分が DisplayName のみなのに型が2本立て
public sealed class FooCreateRequest { public int ArchetypeId { get; init; } public string DisplayName { get; init; } }
public sealed class BarCreateRequest { public int ArchetypeId { get; init; } }

// After: 共通 request に optional field を持たせる
public sealed class EntityCreateRequest
{
    public int ArchetypeId { get; init; }
    public string DisplayNameOverride { get; init; }  // null ならデフォルト名を使う
}

public sealed class EntityFactory
{
    public Entity Create(EntityCreateRequest request)
    {
        return core.Create(request.ArchetypeId, request.DisplayNameOverride);
    }
}
```

### レビュー観点

- `XxxFactory` / `YyyFactory` の差分が責務差か、単なる request フィールド差か
- 共通の core factory があるのに上層 interface が増えていないか

### DungeonInn Example

旧 DungeonInn 実装では `AdventurerCreateRequest` / `MonsterCreateRequest` と
`IAdventurerFactory` / `IMonsterFactory` が並列に存在していた。
差分が `DisplayName` と必須 Behavior 種別だけだったため、`ActorFactoryRequest` / `IActorFactory` に統合した。

---

## 13. 純粋計算クラスはインスタンスを毎回生成しない

副作用を持たない純粋計算クラス（Calculator / Policy 等）のインスタンスをメソッド呼び出しのたびに `new` するのは禁止する。

解決手段の優先順位は以下の通りとする。

1. **`static` クラスまたは `static` メソッド化する（推奨）** — 状態を持たない計算クラスは `static` が最も明確。Domain 層のクラスは DI フレームワークへの依存を避けるためこちらを選ぶ。
2. **呼び出し側が共有インスタンスを所有する** — `static` にできない事情がある場合（例: インターフェースを要求される）、呼び出し側クラスのフィールドとして `readonly` インスタンスを保持して再利用する。
3. **DI でシングルトンとしてインジェクトする** — Application 層以上のクラスで、テスト差し替えや将来の拡張が見込まれる場合に限り採用する。Domain 層のクラスに DI を導入すると Domain 層がフレームワークに依存する問題が生じるため禁止する。

### Before

```csharp
public sealed class Actor
{
    public void RefreshParams()
    {
        // NG: 呼び出しのたびに新しいインスタンスを生成する
        Params = new ActorParamCalculator().Calculate(Stats, equipment, Behavior, Level);
    }
}
```

### After

```csharp
// 選択肢1: static クラスにする（Domain 層の推奨）
public static class ActorParamCalculator
{
    public static ActorParams Calculate(
        ActorStats stats, IReadOnlyList<EquipmentMaster> equipment,
        IActorBehavior behavior, int level) { ... }
}

// 選択肢2: 呼び出し側がフィールドとして保持する
public sealed class Actor
{
    static readonly ActorParamCalculator calculator = new();

    public void RefreshParams()
    {
        Params = calculator.Calculate(Stats, equipment, Behavior, Level);
    }
}
```

### 適用基準

- メソッドに状態がなく（フィールドを持たず）同じ引数で同じ結果を返す計算クラスは `static` にする
- `new XxxCalculator()` がメソッド本体に出てきたら即座に対象として疑う
- インターフェース（`IWeaponCalculator` 等）を要求される場合は選択肢2でフィールド共有する
- DI による Calculator 注入は Application 層のみ。Domain 層には持ち込まない

### DungeonInn Example

`ActorParamCalculator` は状態を持たないため `static` クラスまたは `Actor` のフィールドとして保持する。
`Actor.RefreshParams()` 内で毎回 `new ActorParamCalculator()` するパターンは禁止。

---

## レビュー用チェックリスト

### 命名・分類

- [ ] プロジェクト内の主要 Entity ID 型が統一されているか（`Guid` 混在・`int` 混在になっていないか）
- [ ] マスタ行に対応する型は `Master`、複数マスタを束ねた実行時仕様は `Spec` になっているか
- [ ] Runtime Instance が `XxxMasterId` を保持し、マスタ由来の不変値をコピーしていないか
- [ ] コピー保持している場合、「スナップショット・現在値・個体固有値」として明示されているか
- [ ] DTO の責務（現在値・履歴・集計途中）が名前から分かるか

### Entity 設計

- [ ] Entity に `IsXxx` のような分類 bool が増えていないか
- [ ] 実体型から導ける enum / Type が Runtime Object に重複保持されていないか
- [ ] Entity に種類別の計算式が集まりすぎていないか
- [ ] キャッシュを持つ場合、変更経路が Entity 経由に集約されているか
- [ ] Domain Validation が「不変条件」ではなく「AI判断」や「業務判断」まで禁止していないか
- [ ] 将来の Behavior / Faction / Policy 追加で既存クラス名が破綻しないか

### 型・概念の重複

- [ ] 同じ概念を表す値型・構造体が既にないか確認したか
- [ ] 新しいフィールドが「本質的な属性か、文脈依存の属性か」を自問したか
- [ ] `Count` / `Amount` / `Quantity` のような命名ゆれが発生していないか
- [ ] 同じ意味のフィールドが複数 DTO に重複していないか
- [ ] 並列 Factory / Request 構造を作る前に共通基盤で表現できないか確認したか

### Factory

- [ ] Factory が文脈依存の判断まで抱え込みすぎていないか
- [ ] View / Infrastructure / Framework の都合が Domain に入り込んでいないか

### 純粋計算クラス

- [ ] 状態を持たない Calculator / Policy クラスがメソッド呼び出しのたびに `new` されていないか
- [ ] Domain 層の Calculator は `static` クラスまたはフィールドとして保持する共有インスタンスになっているか
- [ ] Domain 層の Calculator に DI を導入してフレームワーク依存を持ち込んでいないか
