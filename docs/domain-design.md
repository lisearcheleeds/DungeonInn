# DungeonInn ドメイン設計書

バージョン: 1.0.0
対象仕様: spec.md v1.0.0

---

## 1. アーキテクチャ方針

Clean Architecture を採用する。依存の方向は外から内（Presentation → Application → Domain）のみ。

```
[Presentation / View]   Lighthouse シーン・MonoBehaviour・Presenter
        ↓ DI (VContainer)
[Application]           ユースケース・AI サービス
        ↓
[Domain]                エンティティ・値オブジェクト・ドメインサービス・リポジトリ IF
        ↑ implements
[Infrastructure]        リポジトリ実装・ScriptableObject・Addressables ロード
```

### 依存ルール

| 層 | Unity 依存 | Lighthouse 依存 | UniTask |
|----|-----------|----------------|---------|
| Domain | **禁止** | **禁止** | 禁止 |
| Application | 禁止 | 禁止 | **必須**（async メソッド） |
| Infrastructure | 許可 | IAssetScope のみ | 許可 |
| View | 許可 | 許可 | 許可 |

- `MonoBehaviour` は **View 層のみ**に存在する
- `NavMeshAgent` は **View 層のみ**に存在する（AI は目標座標を返すだけ）
- `Task` / `ValueTask` は全層で禁止。非同期は `UniTask` 統一

---

## 2. フォルダ・名前空間設計

```
Assets/DungeonInn/Runtime/Scripts/
├── Domain/
│   ├── World/           DungeonInn.Domain.World
│   ├── Inn/             DungeonInn.Domain.Inn
│   ├── Character/       DungeonInn.Domain.Character
│   └── Dungeon/         DungeonInn.Domain.Dungeon
├── Application/
│   ├── UseCase/         DungeonInn.Application.UseCase
│   └── AI/              DungeonInn.Application.AI
├── Infrastructure/
│   └── Repository/      DungeonInn.Infrastructure.Repository
├── Core/                (既存・Lighthouse 起動基盤)
├── Input/               (既存)
├── View/                (既存・Presentation 層)
└── LighthouseGenerated/ (既存・自動生成)
```

---

## 3. 値オブジェクト

### struct / class 使い分け方針

| 条件 | 採用型 |
|------|--------|
| フィールド 1〜3 個かつ合計 ≤ 16 バイト | `readonly struct` |
| フィールドが多い、または参照型を含む | `sealed record class` |
| ポリモーフィズムが必要 | `abstract record class` |

小さい struct はスタック割り当てによる GC プレッシャー軽減と配列格納時のキャッシュ効率が利点。フィールドが増えたら `sealed record class` へ切り替える。

### GridPosition（readonly struct / 12 バイト）
```csharp
// namespace: DungeonInn.Domain.World
public readonly struct GridPosition : IEquatable<GridPosition>
{
    public int X { get; }
    public int Y { get; }
    public int Z { get; }

    public GridPosition(int x, int y, int z);
    public bool Equals(GridPosition other);
    public override int GetHashCode();
}
```
> Vector3 への変換は View/Infrastructure 層の拡張メソッドで行う（Domain を Unity 非依存に保つため）

### Money（readonly struct / 4 バイト）
```csharp
// namespace: DungeonInn.Domain.Inn
public readonly struct Money : IEquatable<Money>, IComparable<Money>
{
    public int Value { get; }  // 0 以上保証

    public Money(int value);
    public static Money operator +(Money a, Money b);
    public static Money operator -(Money a, Money b);
    public static bool operator >=(Money a, Money b);
}
```

### Satisfaction（readonly struct / 4 バイト）
```csharp
// namespace: DungeonInn.Domain.Character
public readonly struct Satisfaction : IEquatable<Satisfaction>
{
    public float Value { get; }  // 0.0 〜 1.0 にクランプ

    public Satisfaction(float value);
    public bool IsAboveThreshold(float threshold);
}
```

---

## 4. ドメインエンティティ

### 4.1 World

```csharp
// namespace: DungeonInn.Domain.World
public class WorldGrid
{
    public int SizeX { get; }
    public int SizeY { get; }
    public int SizeZ { get; }

    public WorldGrid(int sizeX, int sizeY, int sizeZ);
    public bool IsInBounds(GridPosition pos);
    public bool IsOccupied(GridPosition pos);
    public void SetOccupied(GridPosition pos, bool occupied);
}
```

### 4.2 Inn

#### 土地・部屋・ベッドの関係

```
InnLand
  ├── Rooms: List<Room>       5m×5m の土地を購入するたびに Room が 1 つ自動生成される
  │     ├── Beds: List<Bed>   プレイヤーが所持ベッドを任意位置に配置
  │     └── DoorPositions     隣接 Room との共有壁に開く出入り口
  ├── Funds: Money            資金
  └── OwnedBedCount: int      所持ベッド総数（配置済み＋未配置の合計）
```

**ルール:**
- 土地購入（5×5）→ その位置に Room が即時生成。既存 Room と接する辺の壁中央にドアが双方向で開く
- 初期状態: 10×10 = 2×2 配置の Room 4 つが接続済み（ドア計 4 箇所）
- ベッドは `OwnedBedCount - PlacedBedCount > 0` の条件のみで配置可能
- 配置位置の制約: Room 内部かつ壁・ドア・他ベッドと重複しないこと

```csharp
// namespace: DungeonInn.Domain.Inn
public class InnLand
{
    public IReadOnlyList<Room> Rooms { get; }
    public Money Funds { get; private set; }
    public int OwnedBedCount { get; private set; }
    public int PlacedBedCount => Rooms.Sum(r => r.Beds.Count);
    public int UnplacedBedCount => OwnedBedCount - PlacedBedCount;

    public InnLand(IEnumerable<Room> initialRooms, Money initialFunds, int initialBedCount);

    // 隣接チェック・資金チェック → Room 生成 → 隣接 Room との間にドアを開く
    // 戻り値: 生成された Room
    public Room PurchaseParcel(GridPosition origin, Money cost);
    // 未所有・隣接済み・資金充足 の 3 条件を確認
    public bool CanPurchaseAt(GridPosition origin, Money cost);
    public Room? GetRoomContaining(GridPosition worldPos);
    public void AddFunds(Money amount);
    public bool TrySpendFunds(Money amount);
    public void AddBeds(int count);
}

public class Room
{
    public Guid Id { get; }
    // ワールドグリッド上の左下角（Y=0 固定）
    public GridPosition Origin { get; }
    public int Width { get; }   // = 5（固定）
    public int Depth { get; }   // = 5（固定）
    public IReadOnlyList<Bed> Beds { get; }
    // 壁のうち開放されているワールドグリッド座標（隣接 Room 購入時に追加）
    public IReadOnlyList<GridPosition> DoorPositions { get; }

    public Room(Guid id, GridPosition origin, int width, int depth);

    // 境界壁上かつ現在閉じている位置にドアを追加する
    public void OpenDoor(GridPosition wallPos);
    // ベッド配置可否: Room 内部 && 壁でない && ドアでない && 既存ベッドなし
    public bool CanPlaceBedAt(GridPosition worldPos);
    public void PlaceBed(Bed bed);
    public void RemoveBed(Guid bedId);
    // 指定座標が境界かつドアでない → 壁
    public bool IsWall(GridPosition worldPos);
    public bool Contains(GridPosition worldPos);
    // 満足度計算用
    public float Density => (float)Beds.Count / ((Width - 2) * (Depth - 2));  // 内部セルで計算
}

public class Bed
{
    public Guid Id { get; }
    public GridPosition Position { get; }  // ワールドグリッド座標
    public bool IsOccupied { get; private set; }

    public Bed(Guid id, GridPosition position);
    public void CheckIn();
    public void CheckOut();
}
```

#### ドアの開放ロジック（InnLand 内部）

新規 Room の 4 辺を走査し、隣接する既存 Room が見つかった辺の中央ブロックをドアとして双方向に `OpenDoor()` する。

```
例: 5×5 Room（Origin=0,0,0）の North 辺（z=4）に隣接 Room がある場合
  新 Room のドア位置: (2, 0, 4)   ← 中央
  既存 Room のドア位置: (2, 0, 0) ← 既存 Room の South 辺中央
```

### 4.3 Character

```csharp
// namespace: DungeonInn.Domain.Character
public abstract class CharacterBase
{
    public int Hp { get; protected set; }
    public int MaxHp { get; }
    public int AttackPower { get; }
    public int Defense { get; }
    public float MoveSpeed { get; }
    public float AttackSpeed { get; }

    protected CharacterBase(int maxHp, int attackPower, int defense, float moveSpeed, float attackSpeed);
    public bool IsAlive => Hp > 0;
    public void TakeDamage(int amount);
    public void Heal(int amount);
}

// プレイヤーが操作する唯一のキャラクター。戦闘パラメータは保持するが戦闘はスコープ外
public class InnkeeperCharacter : CharacterBase
{
    public InnkeeperCharacter(int maxHp, int attackPower, int defense, float moveSpeed, float attackSpeed)
        : base(maxHp, attackPower, defense, moveSpeed, attackSpeed);
}

// NPC。宿屋 → ダンジョン → 宿屋 をループする自律エージェント
public class AdventurerCharacter : CharacterBase
{
    public Guid Id { get; }
    public Satisfaction CurrentSatisfaction { get; private set; }
    public AdventurerState State { get; private set; }

    public AdventurerCharacter(Guid id, int maxHp, int attackPower, int defense, float moveSpeed, float attackSpeed);
    public void UpdateSatisfaction(Satisfaction satisfaction);
    public void TransitionState(AdventurerState next);
}

public enum AdventurerState
{
    Resting,            // 宿屋で休憩中
    TravelingToDungeon, // ダンジョンへ移動中
    ExploringDungeon,   // ダンジョン探索中
    Returning,          // 宿屋へ帰還中
}

// 各階層に生息するモンスター
public class MonsterCharacter : CharacterBase
{
    public int FloorIndex { get; }  // 0=地下1F 〜 4=地下5F
    public GridPosition PatrolTarget { get; private set; }

    public MonsterCharacter(int floorIndex, int maxHp, int attackPower, int defense, float moveSpeed, float attackSpeed);
    public void SetPatrolTarget(GridPosition target);
}
```

### 4.4 Dungeon

```csharp
// namespace: DungeonInn.Domain.Dungeon
public class DungeonMap
{
    public IReadOnlyList<DungeonFloor> Floors { get; }  // index 0=地下1F, 4=地下5F

    public DungeonMap(IReadOnlyList<DungeonFloor> floors);
    public DungeonFloor GetFloor(int floorIndex);
}

public class DungeonFloor
{
    public int FloorIndex { get; }      // 0-4
    public int YMin { get; }            // 地下1F=-1, 地下2F=-21 ...
    public int YMax { get; }
    public IReadOnlyList<GridPosition> StairsUp { get; }    // 地下1F は地上への出口
    public IReadOnlyList<GridPosition> StairsDown { get; }  // 地下5F は空リスト

    public DungeonFloor(int floorIndex, int yMin, int yMax,
        IReadOnlyList<GridPosition> stairsUp,
        IReadOnlyList<GridPosition> stairsDown,
        bool[,] walkableMap);

    public bool IsWalkable(int localX, int localZ);
    public IReadOnlyList<GridPosition> GetWalkablePositions();
}
```

---

## 5. ドメインサービス

```csharp
// namespace: DungeonInn.Domain.Inn
public class InnFeeCalculator
{
    // 満足度が tipThreshold を超えていればチップを加算して返す
    public Money Calculate(Satisfaction satisfaction, Money baseFee, Money maxTip, float tipThreshold);
}

public class SatisfactionCalculator
{
    // 過密度・プライバシー（個室かどうか）から満足度を算出
    // density が高いほど低下、個室なら補正ボーナス
    public Satisfaction Calculate(float density, bool isPrivateRoom);
}

// namespace: DungeonInn.Domain.Dungeon
public interface IDungeonGenerator
{
    // 差し替え可能な設計（仕様: アルゴリズム変更を許容）
    DungeonMap Generate(int sizeX, int sizeZ, int floorCount, int floorHeight, int seed, int minStairs);
}
```

---

## 6. リポジトリインターフェース（Domain 層定義）

実装は Infrastructure 層に置く。データは Addressables 経由でロードする。

```csharp
// namespace: DungeonInn.Domain.World
public interface IWorldConfigRepository
{
    UniTask<WorldConfigData> LoadAsync();
}

// namespace: DungeonInn.Domain.Inn
public interface IInnConfigRepository
{
    UniTask<InnConfigData> LoadAsync();
}

// namespace: DungeonInn.Domain.Character
public interface IAdventurerConfigRepository
{
    UniTask<AdventurerConfigData> LoadAsync();
}

public interface IMonsterConfigRepository
{
    UniTask<MonsterConfigData> LoadAsync();
}

// namespace: DungeonInn.Domain.Dungeon
public interface IDungeonConfigRepository
{
    UniTask<DungeonConfigData> LoadAsync();
}
```

各 `*ConfigData` は Domain 層のプレーンなデータクラス（Unity 非依存）。
ScriptableObject は Infrastructure 層に定義し、ロード後に `*ConfigData` へ変換する（詳細は master-data.md 参照）。

---

## 7. ユースケース（Application 層）

```csharp
// namespace: DungeonInn.Application.UseCase

// 5m×5m の土地を購入し Room を自動生成する
public class PurchaseInnParcelUseCase
{
    // InnLand.CanPurchaseAt() → InnLand.PurchaseParcel()（ドア開放含む）
    public UniTask<Room> ExecuteAsync(InnLand land, GridPosition origin, Money cost);
}

// 所持ベッドを指定 Room の指定位置に配置する
public class PlaceBedUseCase
{
    // UnplacedBedCount > 0 && Room.CanPlaceBedAt() を確認してから PlaceBed()
    public UniTask ExecuteAsync(InnLand land, Room room, GridPosition worldPos);
}

// 所持ベッドを回収する（配置済みベッドを撤去）
public class RemoveBedUseCase
{
    public UniTask ExecuteAsync(InnLand land, Room room, Guid bedId);
}

// 冒険者が宿屋にチェックインする
public class AdventurerCheckInUseCase
{
    // 全 Room から空きベッドを検索して割り当て。満室なら null を返す
    public UniTask<Bed?> ExecuteAsync(InnLand land, AdventurerCharacter adventurer);
}

// 冒険者がチェックアウトし、料金を受け取る
public class AdventurerCheckOutUseCase
{
    // SatisfactionCalculator → InnFeeCalculator → InnLand.AddFunds → Bed.CheckOut
    public UniTask<Money> ExecuteAsync(
        InnLand land, AdventurerCharacter adventurer, Bed bed, Room room,
        InnFeeCalculator feeCalculator, SatisfactionCalculator satisfactionCalculator,
        InnConfigData config);
}

// ゲーム開始時にダンジョンマップを生成する
public class GenerateDungeonUseCase
{
    public UniTask<DungeonMap> ExecuteAsync(IDungeonGenerator generator, DungeonConfigData config, WorldConfigData worldConfig);
}
```

---

## 8. AI アーキテクチャ（Application 層）

```csharp
// namespace: DungeonInn.Application.AI

// 冒険者 AI の抽象。差し替え可能にする（仕様 6.2）
public interface IAdventurerAI
{
    void OnStateEnter(AdventurerCharacter character, AdventurerState state);
    // 次の目標 GridPosition を返す。目標なしなら null
    GridPosition? Tick(AdventurerCharacter character, float deltaTime);
    void OnStateExit(AdventurerCharacter character, AdventurerState state);
}

// デフォルト実装: ステートマシンベース
public class DefaultAdventurerAI : IAdventurerAI
{
    // 各ステートを IAdventurerAIState に委譲する
}

// ステートの内部インターフェース（DefaultAdventurerAI 内部のみ使用）
internal interface IAdventurerAIState
{
    AdventurerState StateType { get; }
    void Enter(AdventurerCharacter character);
    GridPosition? Tick(AdventurerCharacter character, float deltaTime);
    void Exit(AdventurerCharacter character);
}

// 具体的なステート
// RestingState       : 宿屋のベッドで待機。RestDuration 経過後 → TravelingToDungeon へ
// TravelingState     : ダンジョン入口へ移動。到達で → ExploringDungeon へ
// ExploringState     : ダンジョン内ウェイポイントを巡回。ExploreDuration 経過後 → Returning へ
// ReturningState     : 宿屋入口へ移動。到達でチェックイン → Resting へ
```

**NavMeshAgent との分離方針**

- `IAdventurerAI.Tick()` は次の目標 `GridPosition` を返すだけ
- `NavMeshAgent.SetDestination()` は View 層の `AdventurerPresenter`（MonoBehaviour）が呼ぶ
- これにより AI を NavMesh に非依存に保ち、将来のカスタム実装への差し替えを可能にする

---

## 9. 依存関係サマリー

```
View (WorldScene Presenter / MonoBehaviour)
  ↓ コンストラクタ注入 (VContainer)
Application (UseCase / DefaultAdventurerAI)
  ↓
Domain (Entity / Value / DomainService / Repository IF)
  ↑ implements
Infrastructure (ConfigRepository / ScriptableObject / IAssetScope)
```

| 関心事 | 配置層 |
|--------|--------|
| グリッド座標計算 | Domain.World |
| 料金・満足度計算 | Domain.Inn |
| ダンジョン生成アルゴリズム | Infrastructure（IDungeonGenerator 実装） |
| NavMeshAgent 操作 | View |
| Addressables ロード | Infrastructure |
| シーン遷移 | View（ISceneManager 経由） |
| 入力処理 | View（IInputLayer 経由） |
