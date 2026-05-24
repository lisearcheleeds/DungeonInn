using UnityEngine;
using Lighthouse.Scene;
using DungeonInn.LighthouseGenerated;
using DungeonInn.View.Base;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD
{
    public sealed class GameHUDModuleScene : ProductCanvasModuleSceneBase
    {
        [SerializeField] Canvas hudCanvas;

        public override ModuleSceneId ModuleSceneId => DungeonInnModuleSceneId.GameHUD;
        public Canvas HUDCanvas => hudCanvas;

        void Awake()
        {
            if (hudCanvas != null)
            {
                GameHUDRenderingLayer.ApplyToHierarchy(hudCanvas.gameObject);
            }
        }
    }
}
