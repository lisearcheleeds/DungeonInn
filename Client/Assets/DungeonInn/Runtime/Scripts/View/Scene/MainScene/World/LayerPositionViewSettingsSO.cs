using UnityEngine;

namespace DungeonInn.View.Scene.MainScene.World
{
    [CreateAssetMenu(menuName = "DungeonInn/Visual/LayerPositionViewSettings")]
    public sealed class LayerPositionViewSettingsSO : ScriptableObject
    {
        const float DefaultLayerHeightOffset = -240f;
        const float DefaultActorHeightOffset = 0f;

        [SerializeField] float layerHeightOffset = DefaultLayerHeightOffset;
        [SerializeField] float actorHeightOffset = DefaultActorHeightOffset;

        public LayerPositionViewSettings ToSettings()
        {
            return new LayerPositionViewSettings(layerHeightOffset, actorHeightOffset);
        }

        internal static LayerPositionViewSettings CreateFallbackSettings()
        {
            return new LayerPositionViewSettings(DefaultLayerHeightOffset, DefaultActorHeightOffset);
        }
    }
}
