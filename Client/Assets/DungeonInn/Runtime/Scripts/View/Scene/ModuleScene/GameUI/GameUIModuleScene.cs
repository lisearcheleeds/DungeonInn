using UnityEngine;
using Lighthouse.Scene;
using DungeonInn.LighthouseGenerated;
using DungeonInn.View.Base;

namespace DungeonInn.View.Scene.ModuleScene.GameUI
{
    public sealed class GameUIModuleScene : ProductCanvasModuleSceneBase
    {
        [SerializeField] Canvas uiCanvas;

        public override ModuleSceneId ModuleSceneId => DungeonInnModuleSceneId.GameUI;
        public Canvas UICanvas => uiCanvas;

        void Awake()
        {
            if (uiCanvas != null)
            {
                GameUIRenderingLayer.ApplyToHierarchy(uiCanvas.gameObject);
            }
        }
    }
}

