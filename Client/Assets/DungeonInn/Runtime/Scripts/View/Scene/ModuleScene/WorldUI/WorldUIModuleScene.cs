using DungeonInn.LighthouseGenerated;
using DungeonInn.View.Base;
using Lighthouse.Scene;
using UnityEngine;

namespace DungeonInn.View.Scene.ModuleScene.WorldUI
{
    public sealed class WorldUIModuleScene : ProductCanvasModuleSceneBase
    {
        [SerializeField] Canvas hudCanvas;

        public override ModuleSceneId ModuleSceneId => DungeonInnModuleSceneId.WorldUI;
        public Canvas HUDCanvas => hudCanvas;
    }
}
