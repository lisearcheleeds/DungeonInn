using DungeonInn.LighthouseGenerated;
using Lighthouse.Scene;
using Lighthouse.Scene.SceneBase;
using UnityEngine;

namespace DungeonInn.View.Scene.ModuleScene.GameHUD
{
    public sealed class GameHUDModuleScene : ModuleSceneBase
    {
        [SerializeField] Transform hudRoot;

        public override ModuleSceneId ModuleSceneId => DungeonInnModuleSceneId.GameHUD;
        public Transform HudRoot => hudRoot != null ? hudRoot : transform;
    }
}
