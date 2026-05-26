namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class VisualConfigSettings
    {
        public VisualConfigSettings(
            MapMaterialSetSO mapMaterialSetSO,
            ActorSpriteVisualConfigSO actorSpriteVisualConfigSO)
        {
            MapMaterialSetSO = mapMaterialSetSO;
            ActorSpriteVisualConfigSO = actorSpriteVisualConfigSO;
        }

        public MapMaterialSetSO MapMaterialSetSO { get; }
        public ActorSpriteVisualConfigSO ActorSpriteVisualConfigSO { get; }
    }
}
