using System;
using DungeonInn.Domain.Actor;
using DungeonInn.Master;
using ActorEntity = DungeonInn.Domain.Actor.Actor;

namespace DungeonInn.Domain.Combat
{
    public sealed class DirectWeaponCombatCalculator : IWeaponCombatCalculator
    {
        readonly float rangeMeters;
        readonly float attackIntervalSeconds;

        public DirectWeaponCombatCalculator(float rangeMeters, float attackIntervalSeconds)
        {
            this.rangeMeters = Math.Max(0, rangeMeters);
            this.attackIntervalSeconds = Math.Max(0, attackIntervalSeconds);
        }

        public WeaponCombatParams Calculate(ActorEntity actor, WeaponMaster weaponMaster)
        {
            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            var attackPower = actor.WeaponAttack;
            var attackSpec = CreateDirectAttackSpec(attackPower);
            var resolvedRangeMeters = weaponMaster == null ? rangeMeters : weaponMaster.RangeMeters;
            var resolvedAttackIntervalSeconds = weaponMaster == null ? attackIntervalSeconds : weaponMaster.AttackIntervalSeconds;
            return new WeaponCombatParams(attackPower, resolvedRangeMeters, resolvedAttackIntervalSeconds, attackSpec);
        }

        static WeaponAttackSpec CreateDirectAttackSpec(int attackPower)
        {
            var directDamageNode = new CombatEffectNodeSpec(
                1,
                CombatEffectNodeType.DirectDamage,
                new DamageSpec(attackPower),
                null,
                null,
                Array.Empty<CombatEffectLinkSpec>());

            return new WeaponAttackSpec(
                1,
                new[] { directDamageNode.Id },
                new[] { directDamageNode },
                1);
        }
    }
}
