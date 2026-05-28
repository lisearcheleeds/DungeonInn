using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Domain.Item;

namespace DungeonInn.Master
{
    public static class WeaponTypeCombatMasterCatalog
    {
        static readonly IReadOnlyDictionary<WeaponType, WeaponTypeCombatMaster> Masters = CreateMasters();

        public static IReadOnlyDictionary<WeaponType, WeaponTypeCombatMaster> CreateAll()
        {
            return Masters.ToDictionary(x => x.Key, x => x.Value);
        }

        public static WeaponTypeCombatMaster Get(WeaponType weaponType)
        {
            if (weaponType == WeaponType.None)
            {
                weaponType = WeaponType.Fist;
            }

            if (Masters.TryGetValue(weaponType, out var master))
            {
                return master;
            }

            throw new ArgumentOutOfRangeException(nameof(weaponType));
        }

        static IReadOnlyDictionary<WeaponType, WeaponTypeCombatMaster> CreateMasters()
        {
            return new[]
            {
                new WeaponTypeCombatMaster(WeaponType.Sword, 2.0f, 1.2f, "", "", 12f, 3f, 3),
                new WeaponTypeCombatMaster(WeaponType.Bow, 20.0f, 1.5f, "World/Projectile/Arrow", "", 12f, 3f, 3),
                new WeaponTypeCombatMaster(WeaponType.Dagger, 1.8f, 0.8f, "", "", 12f, 3f, 3),
                new WeaponTypeCombatMaster(WeaponType.Axe, 2.0f, 1.8f, "", "", 12f, 3f, 3),
                new WeaponTypeCombatMaster(WeaponType.Scythe, 3.0f, 1.6f, "", "World/AreaEffect/Scythe", 12f, 3f, 3),
                new WeaponTypeCombatMaster(WeaponType.Staff, 8.0f, 1.6f, "", "", 12f, 3f, 3),
                new WeaponTypeCombatMaster(WeaponType.Fist, 1.5f, 1.0f, "", "", 12f, 3f, 3),
                new WeaponTypeCombatMaster(WeaponType.Claws, 1.5f, 0.9f, "", "", 12f, 3f, 3),
                new WeaponTypeCombatMaster(WeaponType.Fangs, 1.5f, 1.0f, "", "", 12f, 3f, 3)
            }.ToDictionary(x => x.WeaponType);
        }
    }
}
