namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class LayerPositionViewSettings
    {
        public float LayerHeightOffset { get; }
        public float ActorHeightOffset { get; }

        public LayerPositionViewSettings(float layerHeightOffset, float actorHeightOffset)
        {
            LayerHeightOffset = layerHeightOffset;
            ActorHeightOffset = actorHeightOffset;
        }
    }
}
