using DungeonInn.LighthouseGenerated;
using DungeonInn.Runtime.Scripts.View.Base;
using Lighthouse.Scene;

namespace DungeonInn.Runtime.Scripts.View.Scene.MainScene.Game
{
    public class GameScene : ProductMainSceneBase<GameScene.GameTransitionData>
    {
        public override MainSceneId MainSceneId => DungeonInnMainSceneId.GameScene;

        public class GameTransitionData : ProductTransitionDataBase
        {
            public override MainSceneId MainSceneId => DungeonInnMainSceneId.GameScene;
        }
    }
}
