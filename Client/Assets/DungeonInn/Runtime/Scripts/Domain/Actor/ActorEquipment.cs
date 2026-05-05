using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Domain.Item;

namespace DungeonInn.Domain.Actor
{
    public sealed class ActorEquipment
    {
        readonly Dictionary<EquipmentSlot, EquipmentSpec> equippedSpecs = new();

        public IReadOnlyDictionary<EquipmentSlot, EquipmentSpec> EquippedSpecs => equippedSpecs;
        public EquipmentSpec Weapon => equippedSpecs.TryGetValue(EquipmentSlot.Weapon, out var weapon) ? weapon : null;
        public IReadOnlyList<EquipmentSpec> All => equippedSpecs.Values.ToArray();

        internal void Equip(EquipmentSpec equipmentSpec)
        {
            if (equipmentSpec == null)
            {
                throw new ArgumentNullException(nameof(equipmentSpec));
            }

            if (equipmentSpec.Slot == EquipmentSlot.None)
            {
                throw new InvalidOperationException("Equipment slot is required.");
            }

            equippedSpecs[equipmentSpec.Slot] = equipmentSpec;
        }

        internal void Unequip(EquipmentSlot slot)
        {
            if (slot == EquipmentSlot.None)
            {
                return;
            }

            equippedSpecs.Remove(slot);
        }
    }
}
