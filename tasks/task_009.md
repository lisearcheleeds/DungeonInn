# task_009: WorldScene.GetSceneCameraList() をオーバーライドする

## 問題

CLAUDE.md のルール:
> GetSceneCameraList() 未オーバーライドの MainScene → カメラ未登録

`WorldScene` は `ProductCanvasMainSceneBase` を継承する MainScene だが、
`GetSceneCameraList()` をオーバーライドしていない。

## 修正対象

```
Client/Assets/DungeonInn/Runtime/Scripts/View/Scene/MainScene/World/WorldScene.cs
```

## 修正内容

現在の WorldScene は Canvas/UI のみのシーンで 3D カメラを持っていない。
以下のように空配列を返すオーバーライドを追加する（将来 3D カメラを追加した際にここに登録する）。

```csharp
using Cysharp.Threading.Tasks;
using DungeonInn.LighthouseGenerated;
using DungeonInn.Runtime.Scripts.View.Base;
using Lighthouse.Scene;
using Lighthouse.Scene.SceneCamera;
using UnityEngine;

namespace DungeonInn.Runtime.Scripts.View.Scene.MainScene.World
{
    public class WorldScene : ProductCanvasMainSceneBase<WorldScene.WorldTransitionData>
    {
        public override MainSceneId MainSceneId => DungeonInnMainSceneId.World;

        public class WorldTransitionData : ProductTransitionDataBase
        {
            public override MainSceneId MainSceneId => DungeonInnMainSceneId.World;
        }

        public override ISceneCamera[] GetSceneCameraList()
        {
            // 現在 WorldScene は UI 専用シーン。3D カメラを追加した際はここに登録する。
            return System.Array.Empty<ISceneCamera>();
        }
    }
}
```

## 完了条件

- [ ] `uloop.cmd compile --project-path Client` エラーゼロ
- [ ] `review/task_009_done.md` に完了報告
