using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Domain.Item;
using DungeonInn.Master;

namespace DungeonInn.Domain.Actor
{
    public interface IReadOnlyActorEquipment
    {
        IReadOnlyDictionary<EquipmentSlot, EquipmentMaster> EquippedMasters { get; }
        EquipmentMaster WeaponEquipment { get; }
        WeaponMaster Weapon { get; }
        IReadOnlyList<EquipmentMaster> All { get; }
        IReadOnlyList<StatBonus> AllStatBonuses { get; }
    }

    public sealed class ActorEquipment : IReadOnlyActorEquipment
    {
        readonly Dictionary<EquipmentSlot, EquipmentMaster> equippedMasters = new();
        WeaponMaster weaponMaster;

        public IReadOnlyDictionary<EquipmentSlot, EquipmentMaster> EquippedMasters => equippedMasters;
        public EquipmentMaster WeaponEquipment => equippedMasters.TryGetValue(EquipmentSlot.Weapon, out var weapon) ? weapon : null;
        public WeaponMaster Weapon => weaponMaster;
        public IReadOnlyList<EquipmentMaster> All => equippedMasters.Values.ToArray();

        public IReadOnlyList<StatBonus> AllStatBonuses
        {
            get
            {
                var result = new List<StatBonus>();
                foreach (var equippedMaster in equippedMasters.Values)
                {
                    result.AddRange(equippedMaster.StatBonuses);
                }

                return result;
            }
        }

        internal void Equip(EquipmentMaster equipmentMaster)
        {
            Equip(equipmentMaster, null);
        }

        internal void Equip(EquipmentMaster equipmentMaster, WeaponMaster weaponMaster)
        {
            if (equipmentMaster == null)
            {
                throw new ArgumentNullException(nameof(equipmentMaster));
            }

            if (equipmentMaster.Slot == EquipmentSlot.None)
            {
                throw new InvalidOperationException("Equipment slot is required.");
            }

            if (equipmentMaster.Slot == EquipmentSlot.Weapon && weaponMaster == null)
            {
                throw new InvalidOperationException("Weapon equipment requires weapon master.");
            }

            if (equipmentMaster.Slot != EquipmentSlot.Weapon && weaponMaster != null)
            {
                throw new InvalidOperationException("Weapon master can only be equipped to weapon slot.");
            }

            if (weaponMaster != null && weaponMaster.ItemId != equipmentMaster.ItemId)
            {
                throw new InvalidOperationException("Weapon master item id must match equipment master item id.");
            }

            equippedMasters[equipmentMaster.Slot] = equipmentMaster;
            if (equipmentMaster.Slot == EquipmentSlot.Weapon)
            {
                this.weaponMaster = weaponMaster;
            }
        }

        internal void Unequip(EquipmentSlot slot)
        {
            if (slot == EquipmentSlot.None)
            {
                return;
            }

            equippedMasters.Remove(slot);
            if (slot == EquipmentSlot.Weapon)
            {
                weaponMaster = null;
            }
        }
    }
}
