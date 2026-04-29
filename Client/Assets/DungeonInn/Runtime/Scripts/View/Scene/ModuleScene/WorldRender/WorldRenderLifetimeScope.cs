using DungeonInn.Runtime.Scripts.View.World;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DungeonInn.Runtime.Scripts.View.Scene.ModuleScene.WorldRender
{
    public class WorldRenderLifetimeScope : LifetimeScope
    {
        [SerializeField] WorldRenderModuleScene worldRenderModuleScene;
        [SerializeField] WorldRenderer worldRenderer;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(worldRenderModuleScene);
            builder.RegisterComponent(worldRenderer).AsImplementedInterfaces();
        }
    }
}
