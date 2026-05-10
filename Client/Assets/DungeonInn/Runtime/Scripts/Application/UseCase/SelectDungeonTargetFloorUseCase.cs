using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Actor;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class SelectDungeonTargetFloorUseCase
    {
        readonly IMasterRepository masterRepository;
        readonly ActorCombatPowerCalculator combatPowerCalculator;

        [Inject]
        public SelectDungeonTargetFloorUseCase(
            IMasterRepository masterRepository,
            ActorCombatPowerCalculator combatPowerCalculator)
        {
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
            this.combatPowerCalculator = combatPowerCalculator
                ?? throw new ArgumentNullException(nameof(combatPowerCalculator));
        }

        public UniTask<int> ExecuteAsync(Actor actor)
        {
            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            var actorCombatPower = combatPowerCalculator.Calculate(actor);
            var floors = masterRepository.DungeonFloorExplorationMasters.Values
                .Select(master => new FloorDifficulty(master, CalculateFloorDifficulty(master)))
                .OrderByDescending(score => score.Master.FloorIndex)
                .ToArray();

            var selected = floors
                .Where(score => score.Difficulty <= actorCombatPower)
                .OrderByDescending(score => score.Master.FloorIndex)
                .FirstOrDefault();
            if (selected.Master != null)
            {
                return UniTask.FromResult(selected.Master.FloorIndex);
            }

            selected = floors
                .OrderBy(score => score.Master.FloorIndex)
                .First();
            return UniTask.FromResult(selected.Master.FloorIndex);
        }

        float CalculateFloorDifficulty(DungeonFloorExplorationMaster floorMaster)
        {
            var spawnTable = masterRepository.GetSpawnTableMaster(floorMaster.MonsterSpawnTableId);
            if (spawnTable.TargetType != SpawnTableTargetType.ActorArchetype)
            {
                throw new InvalidOperationException("Dungeon floor exploration requires actor archetype spawn table.");
            }

            var totalWeight = spawnTable.Entries.Sum(entry => entry.Weight);
            var averageCombatPower = spawnTable.Entries.Sum(entry =>
            {
                var archetypeMaster = masterRepository.GetActorArchetypeMaster(entry.TargetMasterId);
                return combatPowerCalculator.Calculate(archetypeMaster) * entry.Weight;
            }) / (float)totalWeight;
            return averageCombatPower * floorMaster.DifficultyCoefficient;
        }

        readonly struct FloorDifficulty
        {
            public DungeonFloorExplorationMaster Master { get; }
            public float Difficulty { get; }

            public FloorDifficulty(DungeonFloorExplorationMaster master, float difficulty)
            {
                Master = master;
                Difficulty = difficulty;
            }
        }
    }
}
