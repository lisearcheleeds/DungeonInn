using DungeonInn.LighthouseGenerated;
using DungeonInn.Runtime.Scripts.View.Base;
using Lighthouse.Scene;
using Lighthouse.Scene.SceneCamera;
using UnityEngine;

namespace DungeonInn.Runtime.Scripts.View.Scene.ModuleScene.Camera
{
    public class CameraModuleScene : ProductModuleSceneBase
    {
        [SerializeField] SceneCamera sceneCamera;

        public override ModuleSceneId ModuleSceneId => DungeonInnModuleSceneId.CameraModule;

        public override ISceneCamera[] GetSceneCameraList()
        {
            return new ISceneCamera[] { sceneCamera };
        }
    }
}
