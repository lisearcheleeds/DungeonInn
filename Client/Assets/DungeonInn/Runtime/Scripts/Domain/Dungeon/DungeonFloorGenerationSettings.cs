using System;

namespace DungeonInn.Domain.Dungeon
{
    public sealed class DungeonFloorGenerationSettings
    {
        public int ThemeId { get; }

        public DungeonFloorGenerationSettings(int themeId)
        {
            ThemeId = Math.Max(0, themeId);
        }
    }
}
