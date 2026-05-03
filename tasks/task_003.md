# task_003: Phase 2 — コンフィグ基盤完全実装

## 目的
マスタデータ（ScriptableObject）を Addressables に登録し、
Repository 経由で DI コンテナに流し込む。
Launcher 起動時に全コンフィグを一括ロードしてキャッシュする。

## 必読ドキュメント（作業前に必ず読む）
- docs/spec.md
- docs/master-data.md
- docs/domain-design.md
- .claude/CLAUDE.md（禁止事項）

---

## 作業 1: Repository を IAssetManager ベースに修正（5 ファイル）

現在の実装は IAssetScope をコンストラクタ注入しているが、
IAssetManager から自前でスコープを作成・破棄するパターンに変更する。
ロード後は結果をキャッシュし、2 回目以降は即値を返す。

格納先: `Client/Assets/DungeonInn/Runtime/Scripts/Infrastructure/Repository/`

### 修正パターン（WorldConfigRepository を例に）

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

同パターンで残り 4 つを修正する:
- InnConfigRepository        → "Config/InnConfig"        / InnConfigSO / InnConfigData
- AdventurerConfigRepository → "Config/AdventurerConfig" / AdventurerConfigSO / AdventurerConfigData
- MonsterConfigRepository    → "Config/MonsterConfig"    / MonsterConfigSO / MonsterConfigData
- DungeonConfigRepository    → "Config/DungeonConfig"    / DungeonConfigSO / DungeonConfigData

---

## 作業 2: ProductLifetimeScope への登録

`Client/Assets/DungeonInn/Runtime/Scripts/Core/ProductLifetimeScope.cs` の
`Configure(IContainerBuilder builder)` に以下を追加する。
（既存の `// YourProduct` ブロック内に追記）

```csharp
// using の追加
using DungeonInn.Domain.World;
using DungeonInn.Domain.Inn;
using DungeonInn.Domain.Character;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Infrastructure.Repository;
using LighthouseExtends.Addressable;

// builder への追加
builder.Register<AssetManager>(Lifetime.Singleton).As<IAssetManager>();
builder.Register<WorldConfigRepository>(Lifetime.Singleton).As<IWorldConfigRepository>();
builder.Register<InnConfigRepository>(Lifetime.Singleton).As<IInnConfigRepository>();
builder.Register<AdventurerConfigRepository>(Lifetime.Singleton).As<IAdventurerConfigRepository>();
builder.Register<MonsterConfigRepository>(Lifetime.Singleton).As<IMonsterConfigRepository>();
builder.Register<DungeonConfigRepository>(Lifetime.Singleton).As<IDungeonConfigRepository>();
```

---

## 作業 3: Launcher のコンフィグ eager load

`Client/Assets/DungeonInn/Runtime/Scripts/Core/Launcher.cs` を修正する。

- コンストラクタに全 5 リポジトリを追加注入
- `LaunchProcess()` で全コンフィグを並列ロード（キャッシュが温まる）
- `using` に必要な名前空間を追加

```csharp
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Character;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Inn;
using DungeonInn.Domain.World;
using DungeonInn.Runtime.Scripts.View.Scene.MainScene.World;
using Lighthouse.Scene;
using VContainer;

namespace DungeonInn.Runtime.Scripts.Core
{
    public sealed class Launcher : ILauncher
    {
        static readonly string LauncherSceneName = "Launcher";

        readonly ISceneManager sceneManager;
        readonly IWorldConfigRepository worldConfigRepository;
        readonly IInnConfigRepository innConfigRepository;
        readonly IAdventurerConfigRepository adventurerConfigRepository;
        readonly IMonsterConfigRepository monsterConfigRepository;
        readonly IDungeonConfigRepository dungeonConfigRepository;

        [Inject]
        public Launcher(
            ISceneManager sceneManager,
            IWorldConfigRepository worldConfigRepository,
            IInnConfigRepository innConfigRepository,
            IAdventurerConfigRepository adventurerConfigRepository,
            IMonsterConfigRepository monsterConfigRepository,
            IDungeonConfigRepository dungeonConfigRepository)
        {
            this.sceneManager = sceneManager;
            this.worldConfigRepository = worldConfigRepository;
            this.innConfigRepository = innConfigRepository;
            this.adventurerConfigRepository = adventurerConfigRepository;
            this.monsterConfigRepository = monsterConfigRepository;
            this.dungeonConfigRepository = dungeonConfigRepository;
        }

        void ILauncher.Reboot()
        {
            RebootProcess().Forget();

            async UniTask RebootProcess()
            {
                await sceneManager.PreReboot();
                await UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(
                    LauncherSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
                await LaunchProcess();
                TransitionNextScene();
            }
        }

        async UniTask ILauncher.Launch()
        {
            await FirstLaunchProcess();
            await LaunchProcess();
            TransitionNextScene();
        }

        UniTask FirstLaunchProcess()
        {
            return UniTask.CompletedTask;
        }

        async UniTask LaunchProcess()
        {
            await UniTask.WhenAll(
                worldConfigRepository.LoadAsync(),
                innConfigRepository.LoadAsync(),
                adventurerConfigRepository.LoadAsync(),
                monsterConfigRepository.LoadAsync(),
                dungeonConfigRepository.LoadAsync()
            );
        }

        void TransitionNextScene()
        {
            UniTask.Void(async () =>
            {
                await sceneManager.TransitionScene(new WorldScene.WorldTransitionData());

                if (!string.IsNullOrEmpty(
                    UnityEngine.SceneManagement.SceneManager.GetSceneByName(LauncherSceneName).name))
                {
                    await UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(LauncherSceneName);
                }
            });
        }
    }
}
```

---

## 作業 4: コンフィグ ScriptableObject アセットの作成

`uloop execute-dynamic-code` を使い Unity Editor 上で SO アセットを作成する。

格納先: `Assets/DungeonInn/Runtime/StaticResources/Config/`
（フォルダが存在しない場合は AssetDatabase.CreateFolder で作成してから実行する）

以下の C# コードを `uloop execute-dynamic-code` で実行する:

```csharp
using UnityEditor;
using UnityEngine;
using DungeonInn.Infrastructure.Repository;

// フォルダ作成
var folder = "Assets/DungeonInn/Runtime/StaticResources/Config";
if (!AssetDatabase.IsValidFolder(folder))
{
    var parent = "Assets/DungeonInn/Runtime/StaticResources";
    if (!AssetDatabase.IsValidFolder(parent))
    {
        AssetDatabase.CreateFolder("Assets/DungeonInn/Runtime", "StaticResources");
    }
    AssetDatabase.CreateFolder(parent, "Config");
}

// WorldConfig
var worldConfig = ScriptableObject.CreateInstance<WorldConfigSO>();
AssetDatabase.CreateAsset(worldConfig, folder + "/WorldConfig.asset");

// InnConfig
var innConfig = ScriptableObject.CreateInstance<InnConfigSO>();
AssetDatabase.CreateAsset(innConfig, folder + "/InnConfig.asset");

// AdventurerConfig
var adventurerConfig = ScriptableObject.CreateInstance<AdventurerConfigSO>();
AssetDatabase.CreateAsset(adventurerConfig, folder + "/AdventurerConfig.asset");

// MonsterConfig
var monsterConfig = ScriptableObject.CreateInstance<MonsterConfigSO>();
AssetDatabase.CreateAsset(monsterConfig, folder + "/MonsterConfig.asset");

// DungeonConfig
var dungeonConfig = ScriptableObject.CreateInstance<DungeonConfigSO>();
AssetDatabase.CreateAsset(dungeonConfig, folder + "/DungeonConfig.asset");

AssetDatabase.SaveAssets();
AssetDatabase.Refresh();
Debug.Log("All config assets created.");
```

---

## 作業 5: Addressables への登録

以下の C# コードを `uloop execute-dynamic-code` で実行し、
作成した SO アセットを Default Local Group に登録する:

```csharp
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

var settings = AddressableAssetSettingsDefaultObject.Settings;
var group = settings.DefaultGroup;

var entries = new[]
{
    ("Assets/DungeonInn/Runtime/StaticResources/Config/WorldConfig.asset",     "Config/WorldConfig"),
    ("Assets/DungeonInn/Runtime/StaticResources/Config/InnConfig.asset",       "Config/InnConfig"),
    ("Assets/DungeonInn/Runtime/StaticResources/Config/AdventurerConfig.asset","Config/AdventurerConfig"),
    ("Assets/DungeonInn/Runtime/StaticResources/Config/MonsterConfig.asset",   "Config/MonsterConfig"),
    ("Assets/DungeonInn/Runtime/StaticResources/Config/DungeonConfig.asset",   "Config/DungeonConfig"),
};

foreach (var (path, address) in entries)
{
    var guid = AssetDatabase.AssetPathToGUID(path);
    var entry = settings.CreateOrMoveEntry(guid, group, false, false);
    entry.address = address;
}

settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true, true);
AssetDatabase.SaveAssets();
Debug.Log("Addressables entries registered.");
```

---

## 完了条件

1. `uloop.cmd compile --project-path Client` でエラーゼロ
2. SO アセット 5 つが `Assets/DungeonInn/Runtime/StaticResources/Config/` に存在する
3. Addressables の Default Local Group に 5 エントリが登録されている
4. `review/task_003_done.md` に完了報告を記載
