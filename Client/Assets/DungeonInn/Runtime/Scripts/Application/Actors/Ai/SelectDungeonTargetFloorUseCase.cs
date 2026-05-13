using DungeonInn.Application.Combat;
using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Domain.Actor;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.Actors.Ai
{
    public sealed class SelectDungeonTargetFloorUseCase
    {
        readonly IMasterRepository masterRepository;
        readonly ActorCombatPowerCalculator combatPowerCalculator;
        readonly IEventPublisher eventBus;

        [Inject]
        public SelectDungeonTargetFloorUseCase(
            IMasterRepository masterRepository,
            ActorCombatPowerCalculator combatPowerCalculator,
            IEventPublisher eventBus)
        {
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
            this.combatPowerCalculator = combatPowerCalculator
                ?? throw new ArgumentNullException(nameof(combatPowerCalculator));
            this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public UniTask<int> ExecuteAsync(Actor actor)
        {
            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            var actorCombatPower = combatPowerCalculator.Calculate(actor);
            DungeonFloorExplorationMaster selectedFloor = null;
            DungeonFloorExplorationMaster lowestFloor = null;
            foreach (var floorMaster in masterRepository.DungeonFloorExplorationMasters.Values)
            {
                if (lowestFloor == null || floorMaster.FloorIndex < lowestFloor.FloorIndex)
                {
                    lowestFloor = floorMaster;
                }

                var difficulty = CalculateFloorDifficulty(floorMaster);
                if (difficulty <= actorCombatPower
                    && (selectedFloor == null || selectedFloor.FloorIndex < floorMaster.FloorIndex))
                {
                    selectedFloor = floorMaster;
                }
            }

            if (selectedFloor != null)
            {
                eventBus.Publish(new ActorAiDecisionRecorded(
                    actor.Id,
                    AiDecisionType.SelectDungeonFloor,
                    AiDecisionReasonType.CombatPowerMatchesFloor,
                    default,
                    default,
                    0,
                    0,
                    selectedFloor.FloorIndex,
                    0));
                return UniTask.FromResult(selectedFloor.FloorIndex);
            }

            if (lowestFloor == null)
            {
                throw new InvalidOperationException("Dungeon floor exploration master does not exist.");
            }

            eventBus.Publish(new ActorAiDecisionRecorded(
                actor.Id,
                AiDecisionType.SelectDungeonFloor,
                AiDecisionReasonType.FallbackToLowestFloor,
                default,
                default,
                0,
                0,
                lowestFloor.FloorIndex,
                0));
            return UniTask.FromResult(lowestFloor.FloorIndex);
        }

        float CalculateFloorDifficulty(DungeonFloorExplorationMaster floorMaster)
        {
            var spawnTable = masterRepository.GetSpawnTableMaster(floorMaster.MonsterSpawnTableId);
            if (spawnTable.TargetType != SpawnTableTargetType.ActorArchetype)
            {
                throw new InvalidOperationException("Dungeon floor exploration requires actor archetype spawn table.");
            }

            var totalWeight = 0;
            var weightedCombatPower = 0f;
            foreach (var entry in spawnTable.Entries)
            {
                totalWeight += entry.Weight;
                var archetypeMaster = masterRepository.GetActorArchetypeMaster(entry.TargetMasterId);
                weightedCombatPower += combatPowerCalculator.Calculate(archetypeMaster) * entry.Weight;
            }

            var averageCombatPower = weightedCombatPower / totalWeight;
            return averageCombatPower * floorMaster.DifficultyCoefficient;
        }
    }
}
