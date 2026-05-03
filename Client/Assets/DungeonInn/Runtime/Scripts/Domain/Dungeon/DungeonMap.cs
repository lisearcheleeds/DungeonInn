using System;
using System.Collections.Generic;

namespace DungeonInn.Domain.Dungeon
{
    public class DungeonMap
    {
        public IReadOnlyList<DungeonFloor> Floors { get; }

        public DungeonMap(IReadOnlyList<DungeonFloor> floors)
        {
            Floors = floors ?? throw new ArgumentNullException(nameof(floors));
        }

        public DungeonFloor GetFloor(int floorIndex)
        {
            if (floorIndex < 0 || floorIndex >= Floors.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(floorIndex));
            }

            return Floors[floorIndex];
        }
    }
}
