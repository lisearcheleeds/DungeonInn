using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Application.World;
using DungeonInn.Domain.Actor;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.Dungeons
{
    public sealed class GetDungeonLayerInfoUseCase
    {
        readonly IGameWorldStateReader worldState;
        readonly IMasterRepository masterRepository;

        [Inject]
        public GetDungeonLayerInfoUseCase(
            IGameWorldStateReader worldState,
            IMasterRepository masterRepository)
        {
            this.worldState = worldState ?? throw new ArgumentNullException(nameof(worldState));
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
        }

        public bool CanExecute => worldState.IsInitialized;

        public IReadOnlyList<DungeonLayerInfoSummary> Execute()
        {
            if (!CanExecute)
            {
                return Array.Empty<DungeonLayerInfoSummary>();
            }

            var summaries = new List<DungeonLayerInfoSummary>();
            summaries.Add(new DungeonLayerInfoSummary(
                0,
                true,
                CountActors(0, ActorBehaviorType.Adventurer),
                CountActors(0, ActorBehaviorType.Monster),
                0f,
                Array.Empty<DungeonLayerMonsterSpawnSummary>(),
                Array.Empty<DungeonLayerItemDropSummary>()));

            foreach (var depthBandMaster in masterRepository.DungeonDepthBandMasters.Values
                .OrderBy(x => x.MinFloorIndex)
                .ThenByDescending(x => x.SelectionPriority))
            {
                var floorIndex = depthBandMaster.MinFloorIndex;
                var monsterSpawnTable = masterRepository.GetSpawnTableMaster(depthBandMaster.MonsterSpawnTableId);
                var monsterSpawns = CreateMonsterSpawnSummaries(monsterSpawnTable);
                summaries.Add(new DungeonLayerInfoSummary(
                    floorIndex,
                    worldState.Dungeon.HasFloor(floorIndex),
                    CountActors(floorIndex, ActorBehaviorType.Adventurer),
                    CountActors(floorIndex, ActorBehaviorType.Monster),
                    depthBandMaster.DifficultyCoefficient,
                    monsterSpawns,
                    CreateItemDropSummaries(monsterSpawns)));
            }

            return summaries;
        }

        IReadOnlyList<DungeonLayerMonsterSpawnSummary> CreateMonsterSpawnSummaries(SpawnTableMaster spawnTable)
        {
            var summaries = new List<DungeonLayerMonsterSpawnSummary>();
            foreach (var entry in spawnTable.Entries.OrderByDescending(x => x.Weight).ThenBy(x => x.TargetMasterId))
            {
                var archetype = masterRepository.GetActorArchetypeMaster(entry.TargetMasterId);
                summaries.Add(new DungeonLayerMonsterSpawnSummary(
                    archetype.Id,
                    archetype.Name,
                    entry.Weight,
                    entry.MinLevel,
                    entry.MaxLevel));
            }

            return summaries;
        }

        IReadOnlyList<DungeonLayerItemDropSummary> CreateItemDropSummaries(
            IReadOnlyList<DungeonLayerMonsterSpawnSummary> monsterSpawns)
        {
            var summaries = new List<DungeonLayerItemDropSummary>();
            foreach (var monsterSpawn in monsterSpawns)
            {
                var archetype = masterRepository.GetActorArchetypeMaster(monsterSpawn.ActorArchetypeId);
                var species = masterRepository.GetSpeciesMaster(archetype.SpeciesId);
                foreach (var drop in species.SpeciesDrops)
                {
                    var item = masterRepository.GetItemMaster(drop.ItemId);
                    summaries.Add(new DungeonLayerItemDropSummary(
                        item.Id,
                        item.Name,
                        monsterSpawn.MonsterName,
                        drop.Probability,
                        drop.MinCount,
                        drop.MaxCount));
                }
            }

            return summaries
                .OrderBy(x => x.SourceMonsterName)
                .ThenBy(x => x.ItemId)
                .ToArray();
        }

        int CountActors(int floorIndex, ActorBehaviorType behaviorType)
        {
            var count = 0;
            foreach (var actor in worldState.Actors)
            {
                if (actor.Position.LayerId.Value == floorIndex &&
                    masterRepository.GetActorArchetypeMaster(actor.ArchetypeId).BehaviorType == behaviorType)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
