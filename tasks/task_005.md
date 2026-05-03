# task_005: Config Repository の Addressables 違反修正

## 問題

以下の5つの Config Repository が CLAUDE.md の禁止事項に違反している。

```
Client/Assets/DungeonInn/Runtime/Scripts/Infrastructure/Repository/WorldConfigRepository.cs
Client/Assets/DungeonInn/Runtime/Scripts/Infrastructure/Repository/InnConfigRepository.cs
Client/Assets/DungeonInn/Runtime/Scripts/Infrastructure/Repository/AdventurerConfigRepository.cs
Client/Assets/DungeonInn/Runtime/Scripts/Infrastructure/Repository/MonsterConfigRepository.cs
Client/Assets/DungeonInn/Runtime/Scripts/Infrastructure/Repository/DungeonConfigRepository.cs
```

### 違反内容

```csharp
// NG: Addressables 直接呼び出し + WaitForCompletion（メインスレッドブロッキング）
var handle = Addressables.LoadAssetAsync<WorldConfigSO>("Config/WorldConfig");
var asset = handle.WaitForCompletion();
```

CLAUDE.md 禁止事項:
- `Addressables.LoadAssetAsync の直接呼び出し禁止 → IAssetScope 経由のみ`
- `Resources.Load 禁止`

## 正しい実装パターン

```csharp
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.World;
using LighthouseExtends.Addressable;
using VContainer;

namespace DungeonInn.Infrastructure.Repository
{
    public class WorldConfigRepository : IWorldConfigRepository
    {
        readonly IAssetManager assetManager;
        WorldConfigData cached;

        [Inject]
        public WorldConfigRepository(IAssetManager assetManager)
        {
            this.assetManager = assetManager;
        }

        public async UniTask<WorldConfigData> LoadAsync()
        {
            if (cached != null) return cached;
            using var scope = assetManager.CreateScope();
            var handle = await scope.LoadAsync<WorldConfigSO>("Config/WorldConfig");
            cached = handle.Asset.ToData();
            return cached;
        }
    }
}
```

## 作業内容

全5つのリポジトリを上記パターンで修正する。

- `IAssetManager` はコンストラクタ `[Inject]` で受け取る
- `assetManager.CreateScope()` → `scope.LoadAsync<TSO>(address)` → `handle.Asset.ToData()` → `cached` に保存
- `using var scope` で自動解放
- メソッドは `async UniTask<T>` に戻す（`WaitForCompletion` を除去）

各リポジトリのアドレス:
- WorldConfig    → `"Config/WorldConfig"`
- InnConfig      → `"Config/InnConfig"`
- AdventurerConfig → `"Config/AdventurerConfig"`
- MonsterConfig  → `"Config/MonsterConfig"`
- DungeonConfig  → `"Config/DungeonConfig"`

## 完了条件

1. `uloop.cmd compile --project-path Client` エラーゼロ
2. プレイモード起動後 `uloop get-logs` エラーゼロ
3. `review/task_005_done.md` に完了報告
