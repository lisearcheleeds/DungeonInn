using System;
using DungeonInn.Application.Actors.Ai;
using DungeonInn.Application.Actors.Equipment;
using DungeonInn.Application.Actors.Lifecycle;
using DungeonInn.Application.Actors.Movement;
using DungeonInn.Application.Actors.Profiles;
using DungeonInn.Application.Actors.Spawn;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Dungeons;
using DungeonInn.Application.Economy;
using DungeonInn.Application.Facilities;
using DungeonInn.Application.Items;
using DungeonInn.Application.World;

using System.Linq;
using Cysharp.Threading.Tasks;

using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Common;
using DungeonInn.Domain.Map;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.Actors.Spawn
{
    public sealed class SpawnScheduledAdventurerOrchestrator
    {
        readonly SpawnAdventurerUseCase spawnAdventurerUseCase;
        readonly IMasterRepository masterRepository;
        readonly IGameRandom gameRandom;

        [Inject]
        public SpawnScheduledAdventurerOrchestrator(
            SpawnAdventurerUseCase spawnAdventurerUseCase,
            IMasterRepository masterRepository,
            IGameRandom gameRandom)
        {
            this.spawnAdventurerUseCase = spawnAdventurerUseCase ?? throw new ArgumentNullException(nameof(spawnAdventurerUseCase));
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
            this.gameRandom = gameRandom ?? throw new ArgumentNullException(nameof(gameRandom));
        }

        public async UniTask<Actor> ExecuteAsync(IGameWorldState worldState, int currentScheduleTick)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            if (currentScheduleTick - worldState.SpawnSchedule.LastAdventurerSpawnTick < GameConstants.AdventurerSpawnIntervalTicks)
            {
                return null;
            }

            worldState.SpawnSchedule.LastAdventurerSpawnTick = currentScheduleTick;

            // TODO: Spawn limit should come from guild level.
            var adventurerCount = worldState.Actors.Count(x => x.Behavior is AdventurerBehavior);
            if (adventurerCount >= GameConstants.InitialMaxAdventurerCount)
            {
                return null;
            }

            var spawnTable = masterRepository.GetSpawnTableMaster(1);
            if (spawnTable.TargetType != SpawnTableTargetType.AdventurerSpawn)
            {
                throw new InvalidOperationException("Adventurer schedule requires adventurer spawn table.");
            }

            var entry = SelectAdventurerSpawnEntry(worldState, spawnTable);
            if (entry == null)
            {
                return null;
            }

            var adventurerSpawnMaster = masterRepository.GetAdventurerSpawnMaster(entry.TargetMasterId);

            // TODO: Spawn point should come from master data.
            var position = worldState.GroundMap.Layer.GetCellCenter(PickRandomEdgePosition());

            // TODO: Faction should come from FactionMaster.
            var faction = new ActorFaction(1, "Adventurer");

            var request = new ActorFactoryRequest(
                adventurerSpawnMaster.ActorArchetypeId,
                Guid.NewGuid(),
                position,
                faction,
                gameRandom.Next(),
                ActorBehaviorType.Adventurer,
                adventurerSpawnMaster.DisplayName);

            var actor = await spawnAdventurerUseCase.ExecuteAsync(worldState.Guild, request, currentScheduleTick);
            worldState.RegisterActor(actor);
            if (adventurerSpawnMaster.SpawnOnce)
            {
                worldState.SpawnSchedule.MarkAdventurerSpawned(adventurerSpawnMaster.Id);
            }

            return actor;
        }

        SpawnTableEntryMaster SelectAdventurerSpawnEntry(IGameWorldState worldState, SpawnTableMaster spawnTable)
        {
            var entries = spawnTable.Entries
                .Where(entry =>
                {
                    var adventurerSpawnMaster = masterRepository.GetAdventurerSpawnMaster(entry.TargetMasterId);
                    return !adventurerSpawnMaster.SpawnOnce ||
                        !worldState.SpawnSchedule.HasSpawnedAdventurerSpawn(adventurerSpawnMaster.Id);
                })
                .ToArray();
            if (entries.Length == 0)
            {
                return null;
            }

            var totalWeight = entries.Sum(entry => entry.Weight);
            var roll = gameRandom.Next(totalWeight);
            var currentWeight = 0;
            foreach (var entry in entries)
            {
                currentWeight += entry.Weight;
                if (roll < currentWeight)
                {
                    return entry;
                }
            }

            return entries[entries.Length - 1];
        }

        GridPosition PickRandomEdgePosition()
        {
            return gameRandom.Next(4) switch
            {
                0 => new GridPosition(gameRandom.Next(GameConstants.GroundMapWidth), 0),
                1 => new GridPosition(gameRandom.Next(GameConstants.GroundMapWidth), GameConstants.GroundMapDepth - 1),
                2 => new GridPosition(0, gameRandom.Next(GameConstants.GroundMapDepth)),
                _ => new GridPosition(GameConstants.GroundMapWidth - 1, gameRandom.Next(GameConstants.GroundMapDepth)),
            };
        }
    }
}
