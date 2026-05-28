using System;
using DungeonInn.Domain.Dungeon;

namespace DungeonInn.Master
{
    public sealed class DungeonDepthBandMaster
    {
        public int Id { get; }
        public string Name { get; }
        public int MinFloorIndex { get; }
        public int MaxFloorIndex { get; }
        public int FloorInterval { get; }
        public int SelectionPriority { get; }
        public int MonsterSpawnTableId { get; }
        public int MonsterLevel { get; }
        public float DifficultyCoefficient { get; }
        public DungeonSpecialRoomType SpecialRoomType { get; }
        public DungeonFloorGenerationSettings GenerationSettings { get; }

        public DungeonDepthBandMaster(
            int id,
            string name,
            int minFloorIndex,
            int maxFloorIndex,
            int floorInterval,
            int selectionPriority,
            int monsterSpawnTableId,
            int monsterLevel,
            float difficultyCoefficient,
            DungeonSpecialRoomType specialRoomType,
            DungeonFloorGenerationSettings generationSettings)
        {
            if (id < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Dungeon depth band name is required.", nameof(name));
            }

            if (minFloorIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(minFloorIndex));
            }

            if (maxFloorIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxFloorIndex));
            }

            if (0 < maxFloorIndex && maxFloorIndex < minFloorIndex)
            {
                throw new ArgumentOutOfRangeException(nameof(maxFloorIndex));
            }

            if (floorInterval < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(floorInterval));
            }

            if (selectionPriority < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(selectionPriority));
            }

            if (monsterSpawnTableId < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(monsterSpawnTableId));
            }

            if (monsterLevel < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(monsterLevel));
            }

            if (difficultyCoefficient < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(difficultyCoefficient));
            }

            Id = id;
            Name = name;
            MinFloorIndex = minFloorIndex;
            MaxFloorIndex = maxFloorIndex;
            FloorInterval = floorInterval;
            SelectionPriority = selectionPriority;
            MonsterSpawnTableId = monsterSpawnTableId;
            MonsterLevel = monsterLevel;
            DifficultyCoefficient = difficultyCoefficient;
            SpecialRoomType = specialRoomType;
            GenerationSettings = generationSettings ?? throw new ArgumentNullException(nameof(generationSettings));
        }

        public bool Contains(int floorIndex)
        {
            if (floorIndex < MinFloorIndex)
            {
                return false;
            }

            if (0 < FloorInterval)
            {
                return floorIndex % FloorInterval == 0;
            }

            return MaxFloorIndex == 0 || floorIndex <= MaxFloorIndex;
        }
    }
}
