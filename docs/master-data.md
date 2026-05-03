# DungeonInn マスタデータ設計書

バージョン: 1.0.0
対象仕様: spec.md v1.0.0 / domain-design.md v1.0.0

---

## 1. 概要

マスタデータは **2 層構造** で管理する。

```
[Domain 層]          *ConfigData   純 C# クラス。Unity 非依存。リポジトリ IF の戻り値型。
      ↑ 変換
[Infrastructure 層]  *ConfigSO     ScriptableObject。Addressables でロード。ToData() で変換。
```

- `Resources.Load` 禁止 → すべて `IAssetScope.LoadAsync<T>()` 経由でロードする
- Addressables グループ: **Default Local Group**（全設定ファイル共通）
- SO の `CreateAssetMenu` パスは `DungeonInn/Config/〇〇`

---

## 2. Domain 層 ConfigData クラス

純 C# クラス。Unity / Lighthouse 依存なし。

```csharp
// namespace: DungeonInn.Domain.World
public class WorldConfigData
{
    public int SizeX { get; init; }          // デフォルト 1000
    public int SizeY { get; init; }          // デフォルト 1000
    public int SizeZ { get; init; }          // デフォルト 1000
    public int DungeonFloorCount { get; init; }   // デフォルト 5
    public int DungeonFloorHeight { get; init; }  // 1 フロアの Y 幅 デフォルト 20
    // 地下 n 階の YMin = -(n-1)*FloorHeight - 1
}

// namespace: DungeonInn.Domain.Inn
public class InnConfigData
{
    public int BaseFee { get; init; }            // 宿泊 1 回あたり固定料金
    public int MaxTip { get; init; }             // チップ最大額
    public float TipThreshold { get; init; }     // この満足度を超えるとチップ発生 (0〜1)
    public int InitialLandSizeX { get; init; }   // 初期土地 X（デフォルト 10）
    public int InitialLandSizeZ { get; init; }   // 初期土地 Z（デフォルト 10）
    public int ExpansionStepSize { get; init; }  // 拡張単位（デフォルト 5）
    public int LandPurchaseCost { get; init; }   // 1 区画購入コスト
    public int InitialFunds { get; init; }       // ゲーム開始時の初期資金
    public int InitialBedCount { get; init; }    // ゲーム開始時の所持ベッド数
}

// namespace: DungeonInn.Domain.Character
public class AdventurerConfigData
{
    public int BaseMaxHp { get; init; }
    public int BaseAttackPower { get; init; }
    public int BaseDefense { get; init; }
    public float BaseMoveSpeed { get; init; }
    public float BaseAttackSpeed { get; init; }
    public float SatisfactionThreshold { get; init; }  // チップ判定閾値 (0〜1)
    public float RestDuration { get; init; }            // 宿屋滞在時間（秒）
    public float ExploreDuration { get; init; }         // ダンジョン探索時間（秒）
}

public class MonsterConfigData
{
    public int BaseMaxHp { get; init; }
    public int BaseAttackPower { get; init; }
    public int BaseDefense { get; init; }
    public float PatrolSpeed { get; init; }
    // フロアインデックス n のステータス = Base * (1 + FloorScaleMultiplier * n)
    public float FloorScaleMultiplier { get; init; }
}

// namespace: DungeonInn.Domain.Dungeon
public class DungeonConfigData
{
    public int Seed { get; init; }           // 0 = ランダム
    public int MinStairsCount { get; init; } // 各フロアの最低階段数
    public string GeneratorType { get; init; } // 生成アルゴリズム識別子（例: "Maze"）
    public int FloorSizeX { get; init; }     // 各フロアの X 幅（グリッド）
    public int FloorSizeZ { get; init; }     // 各フロアの Z 幅（グリッド）
}
```

---

## 3. Infrastructure 層 ScriptableObject クラス

```csharp
// namespace: DungeonInn.Infrastructure.Repository
// フォルダ: Assets/DungeonInn/Runtime/Scripts/Infrastructure/Repository/

[CreateAssetMenu(menuName = "DungeonInn/Config/WorldConfig")]
public class WorldConfigSO : ScriptableObject
{
    public int SizeX = 1000;
    public int SizeY = 1000;
    public int SizeZ = 1000;
    public int DungeonFloorCount = 5;
    public int DungeonFloorHeight = 20;

    public WorldConfigData ToData() => new()
    {
        SizeX = SizeX, SizeY = SizeY, SizeZ = SizeZ,
        DungeonFloorCount = DungeonFloorCount,
        DungeonFloorHeight = DungeonFloorHeight,
    };
}

[CreateAssetMenu(menuName = "DungeonInn/Config/InnConfig")]
public class InnConfigSO : ScriptableObject
{
    public int BaseFee = 100;
    public int MaxTip = 50;
    public float TipThreshold = 0.7f;
    public int InitialLandSizeX = 10;
    public int InitialLandSizeZ = 10;
    public int ExpansionStepSize = 5;
    public int LandPurchaseCost = 500;
    public int InitialFunds = 1000;
    public int InitialBedCount = 4;

    public InnConfigData ToData() => new()
    {
        BaseFee = BaseFee, MaxTip = MaxTip, TipThreshold = TipThreshold,
        InitialLandSizeX = InitialLandSizeX, InitialLandSizeZ = InitialLandSizeZ,
        ExpansionStepSize = ExpansionStepSize, LandPurchaseCost = LandPurchaseCost,
        InitialFunds = InitialFunds, InitialBedCount = InitialBedCount,
    };
}

[CreateAssetMenu(menuName = "DungeonInn/Config/AdventurerConfig")]
public class AdventurerConfigSO : ScriptableObject
{
    public int BaseMaxHp = 100;
    public int BaseAttackPower = 10;
    public int BaseDefense = 5;
    public float BaseMoveSpeed = 3.0f;
    public float BaseAttackSpeed = 1.0f;
    public float SatisfactionThreshold = 0.7f;
    public float RestDuration = 30.0f;
    public float ExploreDuration = 60.0f;

    public AdventurerConfigData ToData() => new()
    {
        BaseMaxHp = BaseMaxHp, BaseAttackPower = BaseAttackPower,
        BaseDefense = BaseDefense, BaseMoveSpeed = BaseMoveSpeed,
        BaseAttackSpeed = BaseAttackSpeed,
        SatisfactionThreshold = SatisfactionThreshold,
        RestDuration = RestDuration, ExploreDuration = ExploreDuration,
    };
}

[CreateAssetMenu(menuName = "DungeonInn/Config/MonsterConfig")]
public class MonsterConfigSO : ScriptableObject
{
    public int BaseMaxHp = 50;
    public int BaseAttackPower = 8;
    public int BaseDefense = 3;
    public float PatrolSpeed = 2.0f;
    public float FloorScaleMultiplier = 0.3f;  // 地下 5F は Base * 2.2 倍

    public MonsterConfigData ToData() => new()
    {
        BaseMaxHp = BaseMaxHp, BaseAttackPower = BaseAttackPower,
        BaseDefense = BaseDefense, PatrolSpeed = PatrolSpeed,
        FloorScaleMultiplier = FloorScaleMultiplier,
    };
}

[CreateAssetMenu(menuName = "DungeonInn/Config/DungeonConfig")]
public class DungeonConfigSO : ScriptableObject
{
    public int Seed = 0;
    public int MinStairsCount = 1;
    public string GeneratorType = "Maze";
    public int FloorSizeX = 50;
    public int FloorSizeZ = 50;

    public DungeonConfigData ToData() => new()
    {
        Seed = Seed, MinStairsCount = MinStairsCount,
        GeneratorType = GeneratorType,
        FloorSizeX = FloorSizeX, FloorSizeZ = FloorSizeZ,
    };
}
```

---

## 4. Addressables 設定

| Addressables アドレス | SO ファイル名 | グループ |
|----------------------|--------------|---------|
| `Config/WorldConfig` | WorldConfig.asset | Default Local Group |
| `Config/InnConfig` | InnConfig.asset | Default Local Group |
| `Config/AdventurerConfig` | AdventurerConfig.asset | Default Local Group |
| `Config/MonsterConfig` | MonsterConfig.asset | Default Local Group |
| `Config/DungeonConfig` | DungeonConfig.asset | Default Local Group |

SO アセットの格納先: `Assets/DungeonInn/Runtime/StaticResources/Config/`

---

## 5. リポジトリ実装パターン

全リポジトリ共通パターン（`WorldConfigRepository` を例示）。

```csharp
// namespace: DungeonInn.Infrastructure.Repository

public class WorldConfigRepository : IWorldConfigRepository
{
    private readonly IAssetScope _assetScope;

    public WorldConfigRepository(IAssetScope assetScope)
    {
        _assetScope = assetScope;
    }

    public async UniTask<WorldConfigData> LoadAsync()
    {
        var so = await _assetScope.LoadAsync<WorldConfigSO>("Config/WorldConfig");
        return so.ToData();
    }
}
```

VContainer でのバインド（`ProductLifetimeScope` に追加）:
```csharp
builder.Register<WorldConfigRepository>(Lifetime.Singleton).As<IWorldConfigRepository>();
builder.Register<InnConfigRepository>(Lifetime.Singleton).As<IInnConfigRepository>();
builder.Register<AdventurerConfigRepository>(Lifetime.Singleton).As<IAdventurerConfigRepository>();
builder.Register<MonsterConfigRepository>(Lifetime.Singleton).As<IMonsterConfigRepository>();
builder.Register<DungeonConfigRepository>(Lifetime.Singleton).As<IDungeonConfigRepository>();
```

---

## 6. デフォルト値一覧

| 設定 | パラメータ | デフォルト値 | 根拠 |
|------|----------|------------|------|
| WorldConfig | SizeX/Y/Z | 1000 | spec §2.1 |
| WorldConfig | DungeonFloorCount | 5 | spec §2.2 |
| WorldConfig | DungeonFloorHeight | 20 | spec §2.2（-1〜-20 = 20段） |
| InnConfig | InitialLandSizeX/Z | 10 | spec §3.1 |
| InnConfig | ExpansionStepSize | 5 | spec §3.1 |
| InnConfig | InitialBedCount | 4 | 初期 10×10 = 4 部屋分の想定 |
| InnConfig | TipThreshold | 0.7 | 調整可能（仮値） |
| AdventurerConfig | RestDuration | 30 秒 | 調整可能（仮値） |
| AdventurerConfig | ExploreDuration | 60 秒 | 調整可能（仮値） |
| MonsterConfig | FloorScaleMultiplier | 0.3 | 地下 5F = 基礎値 × 2.2 |
| DungeonConfig | FloorSizeX/Z | 50 | 調整可能（仮値） |
