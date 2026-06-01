using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Actors.Spawn;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.Actors.Ai;
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
using DungeonInn.Domain.Map;
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
        readonly SpawnTableResolver spawnTableResolver;

        [Inject]
        public SpawnScheduledMonsterOrchestrator(
            SpawnMonsterUseCase spawnMonsterUseCase,
            IMasterRepository masterRepository,
            IGameRandom gameRandom,
            IWorldGameSettingsRepository worldGameSettingsRepository,
            SpawnTableResolver spawnTableResolver)
        {
            this.spawnMonsterUseCase = spawnMonsterUseCase ?? throw new ArgumentNullException(nameof(spawnMonsterUseCase));
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
            this.gameRandom = gameRandom ?? throw new ArgumentNullException(nameof(gameRandom));
            this.worldGameSettingsRepository =
                worldGameSettingsRepository ?? throw new ArgumentNullException(nameof(worldGameSettingsRepository));
            this.spawnTableResolver = spawnTableResolver ?? throw new ArgumentNullException(nameof(spawnTableResolver));
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

            var floor = SelectSpawnableFloor(worldState, spawnBalanceSettings.MaxMonsterCount);
            if (floor == null)
            {
                return null;
            }

            var room = SelectSpawnableRoom(floor, currentScheduleTick, out var spawnTable);
            if (room == null)
            {
                return null;
            }

            var position = floor.Layer.GetCellCenter(room.Center);
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
                string.Empty,
                0);

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

        DungeonFloor SelectSpawnableFloor(IGameWorldState worldState, int maxMonsterCountPerFloor)
        {
            if (maxMonsterCountPerFloor <= 0)
            {
                return null;
            }

            DungeonFloor selectedFloor = null;
            var selectedMonsterCount = int.MaxValue;
            var tieCount = 0;
            foreach (var floor in worldState.Dungeon.Floors.Values)
            {
                if (floor.Rooms.Count == 0)
                {
                    continue;
                }

                var monsterCount = CountMonstersOnLayer(worldState, floor.Layer.Id);
                if (maxMonsterCountPerFloor <= monsterCount)
                {
                    continue;
                }

                if (monsterCount < selectedMonsterCount)
                {
                    selectedFloor = floor;
                    selectedMonsterCount = monsterCount;
                    tieCount = 1;
                    continue;
                }

                if (monsterCount == selectedMonsterCount)
                {
                    tieCount++;
                    if (gameRandom.Next(tieCount) == 0)
                    {
                        selectedFloor = floor;
                    }
                }
            }

            return selectedFloor;
        }

        DungeonRoom SelectSpawnableRoom(
            DungeonFloor floor,
            int currentScheduleTick,
            out SpawnTableMaster spawnTable)
        {
            var startIndex = currentScheduleTick % floor.Rooms.Count;
            for (var i = 0; i < floor.Rooms.Count; i++)
            {
                var room = floor.Rooms[(startIndex + i) % floor.Rooms.Count];
                var context = spawnTableResolver.CreateDungeonRoomContext(floor.FloorIndex, room);
                spawnTable = spawnTableResolver.ResolveMonsterSpawnTable(context);
                if (spawnTable != null)
                {
                    return room;
                }
            }

            spawnTable = null;
            return null;
        }

        static int CountMonstersOnLayer(IGameWorldState worldState, MapLayerId layerId)
        {
            var count = 0;
            foreach (var worldActor in worldState.Actors)
            {
                if (worldActor.Behavior is MonsterBehavior &&
                    worldActor.Position.LayerId.Equals(layerId))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
