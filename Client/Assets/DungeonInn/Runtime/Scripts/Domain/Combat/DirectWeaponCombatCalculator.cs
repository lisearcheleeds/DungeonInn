using System;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Item;
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

        public WeaponCombatParams Calculate(ActorEntity actor, EquipmentSpec weaponSpec)
        {
            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            var attackPower = actor.WeaponAttack;
            var attackDefinition = CreateDirectAttackDefinition(attackPower);
            return new WeaponCombatParams(attackPower, rangeMeters, attackIntervalSeconds, attackDefinition);
        }

        static WeaponAttackDefinition CreateDirectAttackDefinition(int attackPower)
        {
            var directDamageNode = new CombatEffectNode(
                1,
                CombatEffectNodeType.DirectDamage,
                new DamageSpec(attackPower),
                null,
                null,
                Array.Empty<CombatEffectLink>());

            return new WeaponAttackDefinition(
                1,
                new[] { directDamageNode.Id },
                new[] { directDamageNode },
                1);
        }
    }
}
