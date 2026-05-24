using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Actors.Spawn;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.Actors.Ai;
using DungeonInn.Application.Actors.Equipment;
using DungeonInn.Application.Actors.Lifecycle;
using DungeonInn.Application.Actors.Movement;
using DungeonInn.Application.Actors.Profiles;

using DungeonInn.Application.Combat;
using DungeonInn.Application.Dungeons;
using DungeonInn.Application.Economy;
using DungeonInn.Application.Facilities;
using DungeonInn.Application.Items;
using DungeonInn.Application.World;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.Actors.Spawn
{
    public sealed class SpawnScheduledMonsterOrchestrator
    {
        readonly SpawnMonsterUseCase spawnMonsterUseCase;
        readonly IMasterRepository masterRepository;
        readonly IGameRandom gameRandom;
        readonly IWorldGameSettingsRepository worldGameSettingsRepository;

        [Inject]
        public SpawnScheduledMonsterOrchestrator(
            SpawnMonsterUseCase spawnMonsterUseCase,
            IMasterRepository masterRepository,
            IGameRandom gameRandom,
            IWorldGameSettingsRepository worldGameSettingsRepository)
        {
            this.spawnMonsterUseCase = spawnMonsterUseCase ?? throw new ArgumentNullException(nameof(spawnMonsterUseCase));
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
            this.gameRandom = gameRandom ?? throw new ArgumentNullException(nameof(gameRandom));
            this.worldGameSettingsRepository =
                worldGameSettingsRepository ?? throw new ArgumentNullException(nameof(worldGameSettingsRepository));
        }

        public async UniTask<Actor> ExecuteAsync(IGameWorldState worldState, int currentScheduleTick)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            // TODO: Spawn timing should come from SpawnTableMaster.
            var spawnBalanceSettings = worldGameSettingsRepository.GetSpawnBalanceSettings();
            if (currentScheduleTick - worldState.SpawnSchedule.LastMonsterSpawnTick < spawnBalanceSettings.MonsterSpawnIntervalTicks)
            {
                return null;
            }

            worldState.SpawnSchedule.LastMonsterSpawnTick = currentScheduleTick;

            // TODO: Spawn limit should come from SpawnTableMaster.
            var monsterCount = 0;
            foreach (var worldActor in worldState.Actors)
            {
                if (worldActor.Behavior is MonsterBehavior)
                {
                    monsterCount++;
                }
            }

            if (monsterCount >= spawnBalanceSettings.MaxMonsterCount)
            {
                return null;
            }

            var selectedFloorIndex = gameRandom.Next(worldState.Dungeon.Floors.Count);
            var floor = SelectFloor(worldState, selectedFloorIndex);
            var room = floor.Rooms[currentScheduleTick % floor.Rooms.Count];
            var position = floor.Layer.GetCellCenter(room.Center);

            var floorExplorationMaster = masterRepository.GetDungeonFloorExplorationMaster(floor.FloorIndex);
            var spawnTable = masterRepository.GetSpawnTableMaster(floorExplorationMaster.MonsterSpawnTableId);
            if (spawnTable.TargetType != SpawnTableTargetType.ActorArchetype)
            {
                throw new InvalidOperationException("Monster schedule requires actor archetype spawn table.");
            }

            var entry = SelectMonsterSpawnEntry(spawnTable);

            // TODO: Faction should come from FactionMaster.
            var faction = new ActorFaction(2, "Monster");

            var request = new ActorFactoryRequest(
                entry.TargetMasterId,
                Guid.NewGuid(),
                position,
                faction,
                gameRandom.Next(),
                ActorBehaviorType.Monster,
                string.Empty);

            var actor = await spawnMonsterUseCase.ExecuteAsync(request);
            worldState.RegisterActor(actor);
            return actor;
        }

        SpawnTableEntryMaster SelectMonsterSpawnEntry(SpawnTableMaster spawnTable)
        {
            var totalWeight = 0;
            foreach (var entry in spawnTable.Entries)
            {
                totalWeight += entry.Weight;
            }

            var roll = gameRandom.Next(totalWeight);
            var currentWeight = 0;
            foreach (var entry in spawnTable.Entries)
            {
                currentWeight += entry.Weight;
                if (roll < currentWeight)
                {
                    return entry;
                }
            }

            return spawnTable.Entries[spawnTable.Entries.Count - 1];
        }

        static DungeonFloor SelectFloor(IGameWorldState worldState, int selectionIndex)
        {
            var currentIndex = 0;
            foreach (var floor in worldState.Dungeon.Floors.Values)
            {
                if (currentIndex == selectionIndex)
                {
                    return floor;
                }

                currentIndex++;
            }

            throw new InvalidOperationException("Generated dungeon floor does not exist.");
        }
    }
}
