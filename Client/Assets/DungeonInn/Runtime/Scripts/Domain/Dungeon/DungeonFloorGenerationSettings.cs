using System;

namespace DungeonInn.Domain.Dungeon
{
    public sealed class DungeonFloorGenerationSettings
    {
        public int RoomCount { get; }
        public int MinCorridorLength { get; }
        public int MaxCorridorLength { get; }
        public int ThemeId { get; }

        public DungeonFloorGenerationSettings(
            int roomCount,
            int minCorridorLength,
            int maxCorridorLength,
            int themeId)
        {
            if (roomCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(roomCount));
            }

            if (minCorridorLength < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(minCorridorLength));
            }

            if (maxCorridorLength < minCorridorLength)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCorridorLength));
            }

            RoomCount = roomCount;
            MinCorridorLength = minCorridorLength;
            MaxCorridorLength = maxCorridorLength;
            ThemeId = Math.Max(0, themeId);
        }
    }
}
