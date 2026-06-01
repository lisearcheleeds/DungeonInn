using System;
using DungeonInn.Application.Actors.Ai;
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
        readonly IWorldGameSettingsRepository worldGameSettingsRepository;
        readonly SpawnTableResolver spawnTableResolver;
        readonly IGameClock gameClock;
        readonly IActorProfileRegistry actorProfileRegistry;

        [Inject]
        public SpawnScheduledAdventurerOrchestrator(
            SpawnAdventurerUseCase spawnAdventurerUseCase,
            IMasterRepository masterRepository,
            IGameRandom gameRandom,
            IWorldGameSettingsRepository worldGameSettingsRepository,
            SpawnTableResolver spawnTableResolver,
            IGameClock gameClock,
            IActorProfileRegistry actorProfileRegistry)
        {
            this.spawnAdventurerUseCase = spawnAdventurerUseCase ?? throw new ArgumentNullException(nameof(spawnAdventurerUseCase));
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
            this.gameRandom = gameRandom ?? throw new ArgumentNullException(nameof(gameRandom));
            this.worldGameSettingsRepository =
                worldGameSettingsRepository ?? throw new ArgumentNullException(nameof(worldGameSettingsRepository));
            this.spawnTableResolver = spawnTableResolver ?? throw new ArgumentNullException(nameof(spawnTableResolver));
            this.gameClock = gameClock ?? throw new ArgumentNullException(nameof(gameClock));
            this.actorProfileRegistry = actorProfileRegistry ?? throw new ArgumentNullException(nameof(actorProfileRegistry));
        }

        public async UniTask<Actor> ExecuteAsync(IGameWorldState worldState, int currentScheduleTick)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            var spawnBalanceSettings = worldGameSettingsRepository.GetSpawnBalanceSettings();
            if (currentScheduleTick - worldState.SpawnSchedule.LastAdventurerSpawnTick < spawnBalanceSettings.AdventurerSpawnIntervalTicks)
            {
                return null;
            }

            worldState.SpawnSchedule.LastAdventurerSpawnTick = currentScheduleTick;

            // TODO: Spawn limit should come from guild level.
            var adventurerCount = 0;
            foreach (var worldActor in worldState.Actors)
            {
                if (worldActor.Behavior is AdventurerBehavior)
                {
                    adventurerCount++;
                }
            }

            if (adventurerCount >= spawnBalanceSettings.MaxAdventurerCount)
            {
                return null;
            }

            var spawnTable = spawnTableResolver.ResolveAdventurerSpawnTable(gameClock);
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
                adventurerSpawnMaster.DisplayName,
                adventurerSpawnMaster.Id);

            var actor = await spawnAdventurerUseCase.ExecuteAsync(request);
            worldState.RegisterActor(actor);
            if (adventurerSpawnMaster.SpawnOnce)
            {
                worldState.SpawnSchedule.MarkAdventurerSpawned(adventurerSpawnMaster.Id);
            }

            return actor;
        }

        SpawnTableEntryMaster SelectAdventurerSpawnEntry(IGameWorldState worldState, SpawnTableMaster spawnTable)
        {
            var totalWeight = 0;
            foreach (var entry in spawnTable.Entries)
            {
                var adventurerSpawnMaster = masterRepository.GetAdventurerSpawnMaster(entry.TargetMasterId);
                if (adventurerSpawnMaster.SpawnOnce &&
                    worldState.SpawnSchedule.HasSpawnedAdventurerSpawn(adventurerSpawnMaster.Id))
                {
                    continue;
                }

                if (HasActiveAdventurerSpawnMaster(worldState, adventurerSpawnMaster.Id))
                {
                    continue;
                }

                totalWeight += entry.Weight;
            }

            if (totalWeight <= 0)
            {
                return null;
            }

            var roll = gameRandom.Next(totalWeight);
            var currentWeight = 0;
            SpawnTableEntryMaster fallbackEntry = null;
            foreach (var entry in spawnTable.Entries)
            {
                var adventurerSpawnMaster = masterRepository.GetAdventurerSpawnMaster(entry.TargetMasterId);
                if (adventurerSpawnMaster.SpawnOnce &&
                    worldState.SpawnSchedule.HasSpawnedAdventurerSpawn(adventurerSpawnMaster.Id))
                {
                    continue;
                }

                if (HasActiveAdventurerSpawnMaster(worldState, adventurerSpawnMaster.Id))
                {
                    continue;
                }

                fallbackEntry = entry;
                currentWeight += entry.Weight;
                if (roll < currentWeight)
                {
                    return entry;
                }
            }

            return fallbackEntry;
        }

        bool HasActiveAdventurerSpawnMaster(IGameWorldState worldState, int adventurerSpawnMasterId)
        {
            foreach (var actor in worldState.Actors)
            {
                if (actor.Behavior is not AdventurerBehavior)
                {
                    continue;
                }

                if (!actorProfileRegistry.TryGetProfile(actor.Id, out var profile))
                {
                    continue;
                }

                if (profile.AdventurerSpawnMasterId == adventurerSpawnMasterId)
                {
                    return true;
                }
            }

            return false;
        }

        GridPosition PickRandomEdgePosition()
        {
            var groundMapGenerationSettings = worldGameSettingsRepository.GetGroundMapGenerationSettings();
            return gameRandom.Next(4) switch
            {
                0 => new GridPosition(gameRandom.Next(groundMapGenerationSettings.Width), 0),
                1 => new GridPosition(gameRandom.Next(groundMapGenerationSettings.Width), groundMapGenerationSettings.Depth - 1),
                2 => new GridPosition(0, gameRandom.Next(groundMapGenerationSettings.Depth)),
                _ => new GridPosition(groundMapGenerationSettings.Width - 1, gameRandom.Next(groundMapGenerationSettings.Depth)),
            };
        }
    }
}
