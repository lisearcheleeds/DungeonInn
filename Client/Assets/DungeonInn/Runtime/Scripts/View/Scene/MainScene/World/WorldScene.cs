using DungeonInn.LighthouseGenerated;
using DungeonInn.Runtime.Scripts.View.Base;
using Lighthouse.Scene;
using Lighthouse.Scene.SceneCamera;

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
