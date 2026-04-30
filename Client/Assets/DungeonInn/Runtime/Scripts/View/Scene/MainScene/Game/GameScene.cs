using DungeonInn.LighthouseGenerated;
using DungeonInn.Runtime.Scripts.View.Base;
using CameraModuleNS = DungeonInn.Runtime.Scripts.View.Scene.ModuleScene.Camera;
using Lighthouse.Scene;
using Lighthouse.Scene.SceneCamera;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DungeonInn.Runtime.Scripts.View.Scene.MainScene.Game
{
    public class GameScene : ProductMainSceneBase<GameScene.GameTransitionData>
    {
        public override MainSceneId MainSceneId => DungeonInnMainSceneId.GameScene;

        public override ISceneCamera[] GetSceneCameraList()
        {
            var cameraModule = Object.FindFirstObjectByType<CameraModuleNS.CameraModuleScene>(FindObjectsInactive.Include);
            if (cameraModule == null) return System.Array.Empty<ISceneCamera>();
            var cameras = cameraModule.GetSceneCameraList();
            var wrapped = new ISceneCamera[cameras.Length];
            for (var i = 0; i < cameras.Length; i++)
                wrapped[i] = new Camera3DDepthWrapper(cameras[i]);
            return wrapped;
        }

        // SceneCameraManager hardcodes Base camera depth=0, but URP requires depth>=1 here
        // to prevent the Base camera from rendering below the default depth threshold.
        sealed class Camera3DDepthWrapper : ISceneCamera
        {
            readonly ISceneCamera inner;
            internal Camera3DDepthWrapper(ISceneCamera inner) => this.inner = inner;
            SceneCameraType ISceneCamera.SceneCameraType => inner.SceneCameraType;
            float ISceneCamera.CameraDefaultDepth => inner.CameraDefaultDepth;
            void ISceneCamera.SetupCamera(CameraRenderType t, float _) => inner.SetupCamera(t, 1f);
            void ISceneCamera.AddStackCamera(ISceneCamera cam) => inner.AddStackCamera(cam);
            void ISceneCamera.ClearStackCamera() => inner.ClearStackCamera();
            UnityEngine.Camera ISceneCamera.GetCamera() => inner.GetCamera();
        }

        public class GameTransitionData : ProductTransitionDataBase
        {
            public override MainSceneId MainSceneId => DungeonInnMainSceneId.GameScene;
        }
    }
}
