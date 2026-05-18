using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class VisualConfigSettings
    {
        public VisualConfigSettings(
            MapMaterialSetSO mapMaterialSetSO,
            ActorSpriteVisualConfigSO actorSpriteVisualConfigSO,
            GameObject propPrefab)
        {
            MapMaterialSetSO = mapMaterialSetSO;
            ActorSpriteVisualConfigSO = actorSpriteVisualConfigSO;
            PropPrefab = propPrefab;
        }

        public MapMaterialSetSO MapMaterialSetSO { get; }
        public ActorSpriteVisualConfigSO ActorSpriteVisualConfigSO { get; }
        public GameObject PropPrefab { get; }
    }
}
