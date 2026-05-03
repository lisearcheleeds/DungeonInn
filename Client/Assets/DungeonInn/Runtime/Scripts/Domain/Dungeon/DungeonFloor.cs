using System;
using System.Collections.Generic;
using DungeonInn.Domain.World;

namespace DungeonInn.Domain.Dungeon
{
    public class DungeonFloor
    {
        private readonly bool[,] walkableMap;

        public int FloorIndex { get; }
        public int YMin { get; }
        public int YMax { get; }
        public IReadOnlyList<GridPosition> StairsUp { get; }
        public IReadOnlyList<GridPosition> StairsDown { get; }

        public DungeonFloor(
            int floorIndex,
            int yMin,
            int yMax,
            IReadOnlyList<GridPosition> stairsUp,
            IReadOnlyList<GridPosition> stairsDown,
            bool[,] walkableMap)
        {
            FloorIndex = floorIndex;
            YMin = yMin;
            YMax = yMax;
            StairsUp = stairsUp ?? throw new ArgumentNullException(nameof(stairsUp));
            StairsDown = stairsDown ?? throw new ArgumentNullException(nameof(stairsDown));
            this.walkableMap = walkableMap ?? throw new ArgumentNullException(nameof(walkableMap));
        }

        public bool IsWalkable(int localX, int localZ)
        {
            return localX >= 0 &&
                   localX < walkableMap.GetLength(0) &&
                   localZ >= 0 &&
                   localZ < walkableMap.GetLength(1) &&
                   walkableMap[localX, localZ];
        }

        public IReadOnlyList<GridPosition> GetWalkablePositions()
        {
            var positions = new List<GridPosition>();
            for (var x = 0; x < walkableMap.GetLength(0); x++)
            {
                for (var z = 0; z < walkableMap.GetLength(1); z++)
                {
                    if (walkableMap[x, z])
                    {
                        positions.Add(new GridPosition(x, YMin, z));
                    }
                }
            }

            return positions;
        }
    }
}
