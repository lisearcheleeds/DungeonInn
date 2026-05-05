using System;
using System.Collections.Generic;

namespace DungeonInn.Domain.Combat
{
    public sealed class CombatEffectNodeSpec
    {
        public int Id { get; }
        public CombatEffectNodeType Type { get; }
        public DamageSpec DamageSpec { get; }
        public AttackAreaSpec AreaSpec { get; }
        public ProjectileSpec ProjectileSpec { get; }
        public IReadOnlyList<CombatEffectLinkSpec> Links { get; }

        public CombatEffectNodeSpec(
            int id,
            CombatEffectNodeType type,
            DamageSpec damageSpec,
            AttackAreaSpec areaSpec,
            ProjectileSpec projectileSpec,
            IReadOnlyList<CombatEffectLinkSpec> links)
        {
            if (id < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            Id = id;
            Type = type;
            DamageSpec = damageSpec;
            AreaSpec = areaSpec;
            ProjectileSpec = projectileSpec;
            Links = links ?? Array.Empty<CombatEffectLinkSpec>();
            ValidateSpec();
        }

        void ValidateSpec()
        {
            switch (Type)
            {
                case CombatEffectNodeType.DirectDamage:
                    if (DamageSpec == null)
                    {
                        throw new ArgumentException("Direct damage node requires damage spec.");
                    }

                    return;
                case CombatEffectNodeType.Area:
                    if (AreaSpec == null)
                    {
                        throw new ArgumentException("Area node requires area spec.");
                    }

                    return;
                case CombatEffectNodeType.Projectile:
                    if (ProjectileSpec == null)
                    {
                        throw new ArgumentException("Projectile node requires projectile spec.");
                    }

                    return;
                case CombatEffectNodeType.ApplyStatus:
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(Type));
            }
        }
    }
}
