using System;
using System.Linq;
using DungeonInn.Domain.Actor;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class ActorCombatPowerCalculator
    {
        readonly IMasterRepository masterRepository;

        [Inject]
        public ActorCombatPowerCalculator(IMasterRepository masterRepository)
        {
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
        }

        public int Calculate(Actor actor)
        {
            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            return CalculateStats(actor.Stats)
                + actor.Equipment.AllStatBonuses.Sum(bonus => bonus.Amount)
                + (actor.Equipment.Weapon?.Attack ?? 0)
                + actor.Equipment.All.Sum(equipment => equipment.Defense);
        }

        public int Calculate(MonsterSpeciesMaster speciesMaster)
        {
            if (speciesMaster == null)
            {
                throw new ArgumentNullException(nameof(speciesMaster));
            }

            var archetypeMaster = masterRepository.GetActorArchetypeMaster(speciesMaster.ActorArchetypeId);
            return CalculateStats(archetypeMaster.BaseStats);
        }

        static int CalculateStats(ActorStats stats)
        {
            return stats.Strength
                + stats.Dexterity
                + stats.Constitution
                + stats.Intelligence
                + stats.Wisdom
                + stats.Charisma;
        }
    }
}
