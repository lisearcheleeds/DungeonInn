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
            DungeonDepthBandMaster selectedBand = null;
            DungeonDepthBandMaster lowestBand = null;
            foreach (var depthBandMaster in masterRepository.DungeonDepthBandMasters.Values)
            {
                if (lowestBand == null || depthBandMaster.MinFloorIndex < lowestBand.MinFloorIndex)
                {
                    lowestBand = depthBandMaster;
                }

                var difficulty = CalculateFloorDifficulty(depthBandMaster);
                if (difficulty <= actorCombatPower
                    && (selectedBand == null || selectedBand.MinFloorIndex < depthBandMaster.MinFloorIndex))
                {
                    selectedBand = depthBandMaster;
                }
            }

            if (selectedBand != null)
            {
                eventBus.Publish(new ActorAiDecisionRecorded(
                    actor.Id,
                    AiDecisionType.SelectDungeonFloor,
                    AiDecisionReasonType.CombatPowerMatchesFloor,
                    default,
                    default,
                    0,
                    0,
                    selectedBand.MinFloorIndex,
                    0));
                return UniTask.FromResult(selectedBand.MinFloorIndex);
            }

            if (lowestBand == null)
            {
                throw new InvalidOperationException("Dungeon depth band master does not exist.");
            }

            eventBus.Publish(new ActorAiDecisionRecorded(
                actor.Id,
                AiDecisionType.SelectDungeonFloor,
                AiDecisionReasonType.FallbackToLowestFloor,
                default,
                default,
                0,
                0,
                lowestBand.MinFloorIndex,
                0));
            return UniTask.FromResult(lowestBand.MinFloorIndex);
        }

        float CalculateFloorDifficulty(DungeonDepthBandMaster depthBandMaster)
        {
            var spawnTable = masterRepository.GetSpawnTableMaster(depthBandMaster.MonsterSpawnTableId);
            if (spawnTable.TargetType != SpawnTableTargetType.ActorArchetype)
            {
                throw new InvalidOperationException("Dungeon depth band requires actor archetype spawn table.");
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
            return averageCombatPower * depthBandMaster.DifficultyCoefficient;
        }
    }
}
