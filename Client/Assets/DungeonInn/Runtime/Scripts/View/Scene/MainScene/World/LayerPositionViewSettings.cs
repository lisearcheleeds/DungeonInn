namespace DungeonInn.View.Scene.MainScene.World
{
    public sealed class LayerPositionViewSettings
    {
        public float LayerHeightOffset { get; }
        public float ActorHeightOffset { get; }

        public LayerPositionViewSettings()
            : this(
                layerHeightOffset: -240f,
                actorHeightOffset: 0f)
        {
        }

        public LayerPositionViewSettings(float layerHeightOffset, float actorHeightOffset)
        {
            LayerHeightOffset = layerHeightOffset;
            ActorHeightOffset = actorHeightOffset;
        }
    }
}
