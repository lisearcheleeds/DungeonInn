using System;
using System.Collections.Generic;
using DungeonInn.Domain.Map;

namespace DungeonInn.Domain.Dungeon
{
    public sealed class Dungeon
    {
        readonly Dictionary<int, DungeonFloor> floors = new();

        public int Seed { get; }
        public IReadOnlyDictionary<int, DungeonFloor> Floors => floors;

        public Dungeon(int seed)
        {
            Seed = seed;
        }

        public bool HasFloor(int floorIndex)
        {
            return floors.ContainsKey(floorIndex);
        }

        public DungeonFloor GetFloor(int floorIndex)
        {
            if (!floors.TryGetValue(floorIndex, out var floor))
            {
                throw new InvalidOperationException("Dungeon floor does not exist.");
            }

            return floor;
        }

        public bool TryGetFloor(int floorIndex, out DungeonFloor floor)
        {
            return floors.TryGetValue(floorIndex, out floor);
        }

        public void AddFloor(DungeonFloor floor)
        {
            if (floor == null)
            {
                throw new ArgumentNullException(nameof(floor));
            }

            if (floors.ContainsKey(floor.FloorIndex))
            {
                throw new InvalidOperationException("Dungeon floor already exists.");
            }

            floors.Add(floor.FloorIndex, floor);
        }

        public LayerPosition GetArrivalPosition(int floorIndex, DungeonStairType arrivalStairType)
        {
            return GetFloor(floorIndex).GetArrivalPosition(arrivalStairType);
        }
    }
}
