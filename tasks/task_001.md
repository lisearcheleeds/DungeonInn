# task_001: Domain層 + Infrastructure層 基盤実装

## 目的
docs/domain-design.md と docs/master-data.md に基づき、
ゲームの Domain 層と Infrastructure 層を純 C# で実装する。
Unity Editor 操作は不要。コンパイル通過を完了条件とする。

## 必読ドキュメント（作業前に必ず読む）
- docs/domain-design.md
- docs/master-data.md
- .claude/CLAUDE.md（禁止事項）

---

## 作業 1: フォルダ構造の作成

以下のパスに空フォルダを作成する（.gitkeep 不要）:

```
Client/Assets/DungeonInn/Runtime/Scripts/Domain/World/
Client/Assets/DungeonInn/Runtime/Scripts/Domain/Inn/
Client/Assets/DungeonInn/Runtime/Scripts/Domain/Character/
Client/Assets/DungeonInn/Runtime/Scripts/Domain/Dungeon/
Client/Assets/DungeonInn/Runtime/Scripts/Application/UseCase/
Client/Assets/DungeonInn/Runtime/Scripts/Application/AI/
Client/Assets/DungeonInn/Runtime/Scripts/Infrastructure/Repository/
```

---

## 作業 2: Domain 層 — 値オブジェクト（3 ファイル）

domain-design.md セクション 3「値オブジェクト」を参照。
**Domain 層は using UnityEngine / Lighthouse を書かないこと。**

### Domain/World/GridPosition.cs
```csharp
namespace DungeonInn.Domain.World

public readonly struct GridPosition : IEquatable<GridPosition>
```
- フィールド: X, Y, Z (int)
- Equals / GetHashCode / == / != を実装する

### Domain/Inn/Money.cs
```csharp
namespace DungeonInn.Domain.Inn

public readonly struct Money : IEquatable<Money>, IComparable<Money>
```
- フィールド: Value (int, コンストラクタで 0 以上に保証)
- +, -, >= 演算子を実装する

### Domain/Character/Satisfaction.cs
```csharp
namespace DungeonInn.Domain.Character

public readonly struct Satisfaction : IEquatable<Satisfaction>
```
- フィールド: Value (float, 0.0〜1.0 にクランプ)
- IsAboveThreshold(float threshold) を実装する

---

## 作業 3: Domain 層 — ConfigData クラス（5 ファイル）

master-data.md セクション 2「Domain 層 ConfigData クラス」を参照。
**Unity 依存なし。init プロパティを使う。**

| ファイル | 名前空間 |
|---------|---------|
| Domain/World/WorldConfigData.cs | DungeonInn.Domain.World |
| Domain/Inn/InnConfigData.cs | DungeonInn.Domain.Inn |
| Domain/Character/AdventurerConfigData.cs | DungeonInn.Domain.Character |
| Domain/Character/MonsterConfigData.cs | DungeonInn.Domain.Character |
| Domain/Dungeon/DungeonConfigData.cs | DungeonInn.Domain.Dungeon |

---

## 作業 4: Domain 層 — リポジトリインターフェース（5 ファイル）

domain-design.md セクション 6「リポジトリインターフェース」を参照。
**戻り値は必ず UniTask。Task / ValueTask 禁止。**

| ファイル | 名前空間 |
|---------|---------|
| Domain/World/IWorldConfigRepository.cs | DungeonInn.Domain.World |
| Domain/Inn/IInnConfigRepository.cs | DungeonInn.Domain.Inn |
| Domain/Character/IAdventurerConfigRepository.cs | DungeonInn.Domain.Character |
| Domain/Character/IMonsterConfigRepository.cs | DungeonInn.Domain.Character |
| Domain/Dungeon/IDungeonConfigRepository.cs | DungeonInn.Domain.Dungeon |

---

## 作業 5: Domain 層 — ドメインエンティティ（7 ファイル）

domain-design.md セクション 4「ドメインエンティティ」を参照。
**Unity 依存なし。**

| ファイル | クラス | 名前空間 |
|---------|-------|---------|
| Domain/World/WorldGrid.cs | WorldGrid | DungeonInn.Domain.World |
| Domain/Inn/InnLand.cs | InnLand | DungeonInn.Domain.Inn |
| Domain/Inn/Room.cs | Room | DungeonInn.Domain.Inn |
| Domain/Inn/Bed.cs | Bed | DungeonInn.Domain.Inn |
| Domain/Character/CharacterBase.cs | CharacterBase (abstract) | DungeonInn.Domain.Character |
| Domain/Character/InnkeeperCharacter.cs | InnkeeperCharacter | DungeonInn.Domain.Character |
| Domain/Character/AdventurerCharacter.cs | AdventurerCharacter + AdventurerState | DungeonInn.Domain.Character |
| Domain/Character/MonsterCharacter.cs | MonsterCharacter | DungeonInn.Domain.Character |
| Domain/Dungeon/DungeonMap.cs | DungeonMap | DungeonInn.Domain.Dungeon |
| Domain/Dungeon/DungeonFloor.cs | DungeonFloor | DungeonInn.Domain.Dungeon |

InnLand のドア開放ロジックの補足:
- PurchaseParcel(origin, cost) は 5×5 の Room を生成し、4 辺を走査して
  既存 Room と接する辺の中央（オフセット 2）に双方向で OpenDoor() を呼ぶ
- 辺の中央座標の計算例（North 辺: z = origin.Z + 4, x = origin.X + 2）:
  - North: (origin.X + 2, 0, origin.Z + 4)
  - South: (origin.X + 2, 0, origin.Z)
  - East:  (origin.X + 4, 0, origin.Z + 2)
  - West:  (origin.X,     0, origin.Z + 2)

---

## 作業 6: Domain 層 — ドメインサービス（2 ファイル）

domain-design.md セクション 5「ドメインサービス」を参照。

| ファイル | 名前空間 |
|---------|---------|
| Domain/Inn/InnFeeCalculator.cs | DungeonInn.Domain.Inn |
| Domain/Inn/SatisfactionCalculator.cs | DungeonInn.Domain.Inn |
| Domain/Dungeon/IDungeonGenerator.cs | DungeonInn.Domain.Dungeon |

---

## 作業 7: Infrastructure 層 — ScriptableObject（5 ファイル）

master-data.md セクション 3「Infrastructure 層 ScriptableObject クラス」を参照。

格納先: `Client/Assets/DungeonInn/Runtime/Scripts/Infrastructure/Repository/`
名前空間: `DungeonInn.Infrastructure.Repository`

各クラスに `ToData()` メソッドを実装すること。
デフォルト値は master-data.md セクション 6「デフォルト値一覧」を参照。

---

## 作業 8: Infrastructure 層 — Repository 実装（5 ファイル）

格納先: `Client/Assets/DungeonInn/Runtime/Scripts/Infrastructure/Repository/`
名前空間: `DungeonInn.Infrastructure.Repository`

**実装パターン（WorldConfigRepository を例に）:**

```csharp
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.World;
using LighthouseExtends.Addressable;
using VContainer;

namespace DungeonInn.Infrastructure.Repository
{
    public class WorldConfigRepository : IWorldConfigRepository
    {
        readonly IAssetScope assetScope;

        [Inject]
        public WorldConfigRepository(IAssetScope assetScope)
        {
            this.assetScope = assetScope;
        }

        public async UniTask<WorldConfigData> LoadAsync()
        {
            var so = await assetScope.LoadAsync<WorldConfigSO>("Config/WorldConfig");
            return so.ToData();
        }
    }
}
```

同パターンで残り 4 つ（InnConfig / AdventurerConfig / MonsterConfig / DungeonConfig）を実装する。

---

## 完了条件

1. `uloop compile --project-path Client` でエラーゼロ（警告は許容）
2. `review/task_001_done.md` に以下を記載して完了報告:
   - 作成したファイル一覧
   - コンパイル結果
   - 実装中に気づいた懸念点（あれば）
