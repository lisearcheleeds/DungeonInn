using DungeonInn.Runtime.Scripts.View.Camera;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DungeonInn.Runtime.Scripts.View.Scene.ModuleScene.Camera
{
    public class CameraModuleLifetimeScope : LifetimeScope
    {
        [SerializeField] CameraModuleScene cameraModuleScene;
        [SerializeField] FreeCamera freeCamera;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(cameraModuleScene);
            builder.RegisterComponent(freeCamera);
        }
    }
}
