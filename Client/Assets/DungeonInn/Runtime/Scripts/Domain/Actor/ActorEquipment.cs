using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Domain.Item;

namespace DungeonInn.Domain.Actor
{
    public sealed class ActorEquipment
    {
        readonly Dictionary<EquipmentSlot, EquipmentMaster> equippedMasters = new();

        public IReadOnlyDictionary<EquipmentSlot, EquipmentMaster> EquippedMasters => equippedMasters;
        public EquipmentMaster Weapon => equippedMasters.TryGetValue(EquipmentSlot.Weapon, out var weapon) ? weapon : null;
        public IReadOnlyList<EquipmentMaster> All => equippedMasters.Values.ToArray();

        internal void Equip(EquipmentMaster equipmentMaster)
        {
            if (equipmentMaster == null)
            {
                throw new ArgumentNullException(nameof(equipmentMaster));
            }

            if (equipmentMaster.Slot == EquipmentSlot.None)
            {
                throw new InvalidOperationException("Equipment slot is required.");
            }

            equippedMasters[equipmentMaster.Slot] = equipmentMaster;
        }

        internal void Unequip(EquipmentSlot slot)
        {
            if (slot == EquipmentSlot.None)
            {
                return;
            }

            equippedMasters.Remove(slot);
        }
    }
}
