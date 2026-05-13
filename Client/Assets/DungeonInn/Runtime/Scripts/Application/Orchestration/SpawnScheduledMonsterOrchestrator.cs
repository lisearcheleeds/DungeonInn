using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Factory;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.UseCase;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.Orchestration
{
    public sealed class SpawnScheduledMonsterOrchestrator
    {
        readonly SpawnMonsterUseCase spawnMonsterUseCase;
        readonly IMasterRepository masterRepository;
        readonly IGameRandom gameRandom;

        [Inject]
        public SpawnScheduledMonsterOrchestrator(
            SpawnMonsterUseCase spawnMonsterUseCase,
            IMasterRepository masterRepository,
            IGameRandom gameRandom)
        {
            this.spawnMonsterUseCase = spawnMonsterUseCase ?? throw new ArgumentNullException(nameof(spawnMonsterUseCase));
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
            this.gameRandom = gameRandom ?? throw new ArgumentNullException(nameof(gameRandom));
        }

        public async UniTask<Actor> ExecuteAsync(IGameWorldState worldState, int currentScheduleTick)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            // TODO: 間隔をSpawnTableMasterから取得する
            if (currentScheduleTick - worldState.SpawnSchedule.LastMonsterSpawnTick < GameConstants.MonsterSpawnIntervalTicks)
            {
                return null;
            }

            worldState.SpawnSchedule.LastMonsterSpawnTick = currentScheduleTick;

            // TODO: 上限をSpawnTableMasterから取得する
            var monsterCount = 0;
            foreach (var worldActor in worldState.Actors)
            {
                if (worldActor.Behavior is MonsterBehavior)
                {
                    monsterCount++;
                }
            }

            if (monsterCount >= GameConstants.InitialMaxMonsterCount)
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

            // TODO: FactionをFactionMasterから取得する
            var faction = new ActorFaction(2, "Monster");

            var request = new MonsterCreateRequest(
                entry.TargetMasterId,
                Guid.NewGuid(),
                position,
                faction,
                gameRandom.Next());

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
