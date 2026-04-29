using DungeonInn.Runtime.Scripts.View.HUD;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DungeonInn.Runtime.Scripts.View.Scene.ModuleScene.UI
{
    public class UIModuleLifetimeScope : LifetimeScope
    {
        [SerializeField] UIModuleScene uiModuleScene;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(uiModuleScene);
        }
    }
}
