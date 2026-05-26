using Lighthouse.Scene.SceneCamera;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class WorldSceneCamera : ISceneCamera
    {
        readonly Camera camera;
        readonly UniversalAdditionalCameraData cameraData;

        float? defaultDepth;

        public WorldSceneCamera(Camera camera)
        {
            this.camera = camera;
            cameraData = camera.GetUniversalAdditionalCameraData();
        }

        public SceneCameraType SceneCameraType => SceneCameraType.Camera3D;

        public float CameraDefaultDepth
        {
            get
            {
                defaultDepth ??= camera.depth;
                return defaultDepth.Value;
            }
        }

        public void SetupCamera(CameraRenderType cameraRenderType, float runtimeDepth)
        {
            camera.gameObject.SetActive(true);
            cameraData.renderType = cameraRenderType;
            defaultDepth ??= camera.depth;
            camera.depth = runtimeDepth;
        }

        public void AddStackCamera(ISceneCamera overlaySceneCamera)
        {
            cameraData.cameraStack.Add(overlaySceneCamera.GetCamera());
        }

        public void ClearStackCamera()
        {
            cameraData.cameraStack.Clear();
        }

        public Camera GetCamera()
        {
            return camera;
        }
    }
}
