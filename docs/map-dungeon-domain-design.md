# Map / Dungeon Domain Design

## 目的

このドキュメントは、地上マップ、ダンジョン、階層化された座標系、移動判定、将来の NavMesh 連携方針を整理する。

Domain / Application は UnityEngine、GameObject、NavMesh に依存しない。表示中ではない地上やダンジョンフロアでも、ゲーム進行上の移動処理ができることを前提にする。

## 現在の実装状態

2026-05-04 時点で、以下の土台実装がある。

- `Domain/Map`
  - `GridPosition`
  - `MapLayerId`
  - `LayerPosition`
  - `MapLayer`
  - `GroundMap`
  - `GroundCell`
  - `GroundCellType`
  - `MapCellBlockType`
  - `MapWalkability`
- `Domain/Dungeon`
  - `Dungeon`
  - `DungeonFloor`
  - `DungeonCell`
  - `DungeonCellType`
  - `DungeonStair`
  - `DungeonStairType`
  - `DungeonFloorGenerationSettings`
  - `DungeonDepthBandConfig`
- `Application/Navigation`
  - `INavigationPathProvider`
  - `NavigationPath`
  - `NavigationPathRequest`
  - `NavigationPathResult`
- `Application/UseCase`
  - `InitializeWorldMapUseCase`
  - `InitializeDungeonUseCase`
  - `GenerateDungeonFloorUseCase`
  - `EnsureDungeonFloorGeneratedUseCase`
  - `CanMoveOnMapLayerUseCase`
  - `UseDungeonStairUseCase`

ダンジョン生成は、旧 DungeonMaker のコンセプトだけを採用した DungeonInn 向けの再設計実装である。

- フロアを Section に分割する。
- Edge 上の開始 Section と終了 Section を決定的な乱数で選ぶ。
- 開始 Section から終了 Section までの Section 経路を作る。
- Section 経路をランダムに歪ませる。
- 各 Section 内に参照点を置く。
- 参照点同士を L 字通路で接続する。
- 参照点周辺に部屋を配置する。
- 開始参照点を上層階段、終了参照点を下層階段にする。

旧 DungeonMaker のコードは直接移植していない。`UnityEngine.Vector2Int`、`UnityEngine.Random`、`Debug.Log` には依存せず、`GridPosition` と `System.Random` で実装している。

トラップ、宝箱、モンスター、アセットテーマ適用は後続で実装する。

## 空間表現

空間は、平面を階層化した四角形グリッドで表現する。

- 1 マスは `5m x 5m`。
- 地上、地下 1 階、地下 2 階は互換性のない別座標系として扱う。
- 地下フロアは無限に生成される。
- Unity の `Vector3` への変換は View または Infrastructure 側で行う。

## 座標 Domain

### GridPosition

`GridPosition` は構造物、セル、部屋、階段、入口など、グリッド単位の配置に使う。

```csharp
public readonly struct GridPosition
{
    public int X { get; }
    public int Z { get; }
}
```

### MapLayerId

`MapLayerId` は地上や地下フロアなど、互換性のない座標系を識別する。

```csharp
public readonly struct MapLayerId
{
    public int Value { get; }
}
```

ID の割り当ては以下を想定する。

- 地上: `0`
- 地下 1 階: `1`
- 地下 2 階: `2`
- 地下 N 階: `N`

### LayerPosition

`LayerPosition` は、特定レイヤー内の連続座標を表す。

冒険者やモンスターは 5m グリッドの中間にも存在できるため、移動処理には整数グリッドではなく連続座標が必要になる。

```csharp
public readonly struct LayerPosition
{
    public MapLayerId LayerId { get; }
    public float X { get; }
    public float Z { get; }
}
```

`LayerId` が異なる `LayerPosition` 同士は、距離計算、補間、直接移動判定を行わない。

## MapLayer

`MapLayer` は、地上や各ダンジョンフロアの座標系、サイズ、セルサイズを持つ。

```csharp
public sealed class MapLayer
{
    public MapLayerId Id { get; }
    public int Width { get; }
    public int Depth { get; }
    public float CellSizeMeters { get; }
}
```

連続座標からグリッド座標への変換は、`CellSizeMeters` を使って行う。

```text
cellX = floor(x / CellSizeMeters)
cellZ = floor(z / CellSizeMeters)
```

## 地上マップ

地上マップは `100 x 100` タイルとする。

- 1 タイルは 5m。
- 地上全体は `500m x 500m`。
- マップ中央にダンジョン入口がある。
- ダンジョン入口の周囲に以下の施設が建つ。
  - 冒険者ギルド
  - 宿屋
  - アイテム店
  - 装備店

地上表示中は View 側で NavMesh を使った高品質な移動表示を行える。ただし、地上が表示されていない場合は GameObject や NavMesh が存在しないため、Domain/Application のしきい値判定で移動処理を行う。

そのため地上マップにも、Domain 側で移動制限を判定できるセル情報を持たせる。

## ダンジョン

ダンジョンは地下に生成される。

- 1 フロアは `200 x 200` タイル。
- 1 タイルは 5m。
- 1 フロアは `1000m x 1000m`。
- フロアは無限に生成される。
- フロアごとに迷路、階段、生成設定、アセットテーマが異なる。

ダンジョン内の移動制限は Mesh Collider や NavMesh に依存しない。壁や通行可能領域は Domain のセル情報と座標しきい値で判定する。

これにより、表示されていないフロアでも冒険者やモンスターの移動処理を継続できる。

## ダンジョンセル

ダンジョンフロアはセルを持つ。

セル種別は現時点では以下。

```csharp
public enum DungeonCellType
{
    Wall,
    Corridor,
    Room
}
```

- `Wall`: 移動不可。
- `Corridor`: 移動可能な通路。
- `Room`: 移動可能な部屋。

トラップや宝箱は将来追加する。セル種別に混ぜるのではなく、セル上に配置される要素として追加する方針とする。

## 地上セル

地上も未表示時に移動判定が必要なため、地上用のセル情報を持つ。

最低限、移動可能かどうかを判定できればよい。

```csharp
public enum MapCellBlockType
{
    Walkable,
    Blocked
}
```

将来的に用途を持たせる場合は、以下のようなセル種別を追加できる。

```csharp
public enum GroundCellType
{
    Open,
    Building,
    DungeonEntrance,
    FacilityEntrance,
    Road
}
```

## 移動判定

Domain/Application は、表示状態に関係なく移動可能かを判定できる。

基本方針:

- 指定座標が対象レイヤー内か確認する。
- 連続座標からセル座標へ変換する。
- セルが移動可能か確認する。
- キャラクター半径が必要な場合は、中心点だけでなく周辺点も確認する。

キャラクター半径を考慮する場合のチェック点例:

- 中心
- 前後左右
- 斜め 4 点

Domain 側は Unity の当たり判定には依存せず、セルと座標しきい値で移動可否を判断する。

## 階段

各ダンジョンフロアには、上層フロアへの階段と下層フロアへの階段がある。

```csharp
public enum DungeonStairType
{
    Up,
    Down
}
```

```csharp
public sealed class DungeonStair
{
    public DungeonStairType Type { get; }
    public GridPosition Position { get; }
}
```

階段を利用すると、移動先フロアの対応する階段付近へワープする。

階段の XZ 座標は上下階で一致していなくてよい。

例:

- 地下 1 階の下層階段
- 地下 2 階の上層階段

この 2 つの XZ 座標は一致しなくてもよい。

地下 1 階の上層階段は地上のダンジョン入口につながる。

## フロア生成

ゲーム開始時に生成するもの:

- 地上マップ
- 地下 1 階

地下 2 階以降は、冒険者がそのフロアへ到達したタイミングで生成する。

例:

- 冒険者が地下 1 階の下層階段を利用する。
- そのタイミングで地下 2 階の迷路を生成する。

迷路生成に使う乱数シードはゲーム開始時に決定する。

各フロアの生成は、ゲーム開始時シードとフロア階層から決定的に再現できるようにする。

現在の実装では、`dungeon.Seed + floorIndex * GameConstants.DungeonFloorSeedMultiplier` をフロア生成用 seed として使う。

## 階層ごとの設定

フロア階層ごとに以下が変わる。

- モンスタースポーンテーブル
- モンスターレベル
- ダンジョン生成設定
  - 部屋数
  - 通路長
  - 迷路密度
  - 階段配置ルール
- アセットテーマ

想定 Domain:

```csharp
public sealed class DungeonFloorGenerationSettings
{
    public int RoomCount { get; }
    public int MinCorridorLength { get; }
    public int MaxCorridorLength { get; }
    public int ThemeId { get; }
}
```

モンスターやアセットテーマの詳細 Domain は後続で定義する。

## NavMesh 連携方針

将来的に、ユーザーが表示している場所では移動品質を高めるために Unity NavMesh を利用する。

ただし、Domain は NavMesh に依存しない。

NavMesh 経路取得はゲームルールそのものではなく、表示中環境で高品質な経路を得るための外部能力である。そのため、インターフェースは Domain ではなく Application 側に置く。

## Application Navigation Interface

Application 側に、経路取得の抽象インターフェースを定義する。

```csharp
public interface INavigationPathProvider
{
    bool CanProvidePath(MapLayerId layerId);

    NavigationPathResult TryFindPath(NavigationPathRequest request);
}
```

```csharp
public sealed class NavigationPathRequest
{
    public LayerPosition From { get; }
    public LayerPosition To { get; }
    public float AgentRadius { get; }
}
```

```csharp
public sealed class NavigationPathResult
{
    public bool Success { get; }
    public NavigationPath Path { get; }
}
```

```csharp
public sealed class NavigationPath
{
    public IReadOnlyList<LayerPosition> Points { get; }
}
```

View または Infrastructure 側で Unity 実装を持つ。

```csharp
public sealed class UnityNavMeshPathProvider : INavigationPathProvider
{
    public bool CanProvidePath(MapLayerId layerId)
    {
        // 現在表示中で NavMesh が存在する layer のみ true
    }

    public NavigationPathResult TryFindPath(NavigationPathRequest request)
    {
        // LayerPosition を UnityEngine.Vector3 に変換する
        // NavMesh.CalculatePath を呼ぶ
        // Vector3[] を LayerPosition[] に戻す
    }
}
```

## 移動 UseCase 方針

移動 UseCase は以下の優先順位で経路を決定する。

1. `INavigationPathProvider.CanProvidePath(layerId)` が true の場合
   - NavMesh 経路を使う。
2. false の場合
   - Domain のセル情報としきい値判定を使う。

NavMesh は表示中の高品質経路であり、ゲーム進行に必須ではない。

## レイヤー責務

### Domain

- マップサイズ
- セル種別
- 通行可能判定
- 階層 ID
- グリッド座標
- 連続座標
- 階段
- ダンジョンフロア
- フロア生成設定

### Application

- 移動 UseCase
- 経路取得インターフェース
- NavMesh が使える場合と使えない場合の切り替え
- フロア生成 UseCase
- 階段利用 UseCase

### View / Infrastructure

- Unity NavMesh
- GameObject
- `UnityEngine.Vector3` 変換
- 表示中レイヤー判定
- メッシュやアセットの生成

## Domain 候補

- `GridPosition`
- `MapLayerId`
- `LayerPosition`
- `MapLayer`
- `MapCellBlockType`
- `GroundCellType`
- `Dungeon`
- `DungeonFloor`
- `DungeonCell`
- `DungeonCellType`
- `DungeonStair`
- `DungeonStairType`
- `DungeonFloorGenerationSettings`
- `DungeonDepthBandConfig`

## UseCase 候補

- `InitializeWorldMapUseCase`
- `InitializeDungeonUseCase`
- `GenerateDungeonFloorUseCase`
- `EnsureDungeonFloorGeneratedUseCase`
- `CanMoveOnMapLayerUseCase`
- `MoveMapActorUseCase`
- `UseDungeonStairUseCase`
