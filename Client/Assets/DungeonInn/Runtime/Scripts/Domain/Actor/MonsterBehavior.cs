using System;

namespace DungeonInn.Domain.Actor
{
    public sealed class MonsterBehavior : IActorBehavior
    {
        public int SpeciesId { get; }

        public MonsterBehavior(int speciesId)
        {
            if (speciesId < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(speciesId));
            }

            SpeciesId = speciesId;
        }

        public void OnRecovered(
            int hpAmount,
            int mpAmount,
            int fatigueReduction,
            int stressReduction,
            int injuryReduction)
        {
        }
    }
}
