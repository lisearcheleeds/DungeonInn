using System;

namespace DungeonInn.View.Scene.MainScene.World
{
    public enum ActorVisualSizeTier
    {
        AdventurerS = 0,
        MonsterS = 1,
        MonsterL = 2
    }

    public static class ActorVisualSizeTierCatalog
    {
        public static float GetCanvasHeightMeters(ActorVisualSizeTier sizeTier)
        {
            return sizeTier switch
            {
                ActorVisualSizeTier.AdventurerS => 1.5f,
                ActorVisualSizeTier.MonsterS => 1.2f,
                ActorVisualSizeTier.MonsterL => 5f,
                _ => throw new ArgumentOutOfRangeException(nameof(sizeTier), sizeTier, null)
            };
        }

        public static float GetGroundAnchorOffsetMeters(ActorVisualSizeTier sizeTier)
        {
            return GetCanvasHeightMeters(sizeTier) * 0.5f;
        }
    }
}
