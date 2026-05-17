using System;
using System.Collections.Generic;
using DungeonInn.Domain.Item;
using DungeonInn.Master;

namespace DungeonInn.Domain.Actor
{
    public interface IReadOnlyActorEquipment
    {
        WeaponType? EquippedWeaponType { get; }
        int EquippedWeaponAttack { get; }
        int TotalDefense { get; }
        int TotalStatBonusAmount { get; }
        int? GetEquippedItemId(EquipmentSlot slot);
        bool IsEquipped(int itemId);
    }

    public sealed class ActorEquipment : IReadOnlyActorEquipment
    {
        readonly Dictionary<EquipmentSlot, EquipmentMaster> equippedMasters = new();
        WeaponMaster weaponMaster;

        public WeaponType? EquippedWeaponType => weaponMaster?.WeaponType;
        public int EquippedWeaponAttack => weaponMaster?.Attack ?? 0;

        public int TotalDefense
        {
            get
            {
                var total = 0;
                foreach (var equippedMaster in equippedMasters.Values)
                {
                    total += equippedMaster.Defense;
                }
                return total;
            }
        }

        public int TotalStatBonusAmount
        {
            get
            {
                var total = 0;
                foreach (var equippedMaster in equippedMasters.Values)
                {
                    foreach (var bonus in equippedMaster.StatBonuses)
                    {
                        total += bonus.Amount;
                    }
                }
                return total;
            }
        }

        public int? GetEquippedItemId(EquipmentSlot slot)
        {
            return equippedMasters.TryGetValue(slot, out var equippedMaster) ? (int?)equippedMaster.ItemId : null;
        }

        public bool IsEquipped(int itemId)
        {
            foreach (var equippedMaster in equippedMasters.Values)
            {
                if (equippedMaster.ItemId == itemId)
                {
                    return true;
                }
            }
            return false;
        }

        // Actor 内部計算用（インターフェース外）
        internal EquipmentMaster WeaponEquipment => equippedMasters.TryGetValue(EquipmentSlot.Weapon, out var weapon) ? weapon : null;
        internal WeaponMaster Weapon => weaponMaster;

        internal void CopyAllTo(List<EquipmentMaster> buffer)
        {
            buffer.Clear();
            foreach (var equippedMaster in equippedMasters.Values)
            {
                buffer.Add(equippedMaster);
            }
        }

        internal void CopyAllStatBonusesTo(List<StatBonus> buffer)
        {
            buffer.Clear();
            foreach (var equippedMaster in equippedMasters.Values)
            {
                foreach (var bonus in equippedMaster.StatBonuses)
                {
                    buffer.Add(bonus);
                }
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
