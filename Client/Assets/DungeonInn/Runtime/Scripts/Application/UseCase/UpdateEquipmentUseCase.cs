using System;
using System.Collections.Generic;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Item;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class UpdateEquipmentUseCase
    {
        readonly IItemMasterRepository masterRepository;
        readonly IEventPublisher eventBus;

        [Inject]
        public UpdateEquipmentUseCase(IItemMasterRepository masterRepository, IEventPublisher eventBus)
        {
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
            this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public void Execute(IGameWorldState worldState)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            foreach (var actor in worldState.Actors)
            {
                if (actor.Behavior is not AdventurerBehavior behavior)
                {
                    continue;
                }

                if (behavior.LifecycleState != AdventurerLifecycleState.Returning &&
                    behavior.LifecycleState != AdventurerLifecycleState.Recovering &&
                    behavior.LifecycleState != AdventurerLifecycleState.WaitingForInn)
                {
                    continue;
                }

                UpdateEquipment(actor);
            }
        }

        void UpdateEquipment(Actor actor)
        {
            TryUpgradeWeapon(actor);
            var weaponType = actor.Equipment.Weapon?.WeaponType ?? actor.NaturalWeaponType;
            var statWeights = WeaponCalculatorFactory.GetStatWeights(weaponType);
            TryUpgradeSlot(actor, EquipmentSlot.Armor, statWeights);
            TryUpgradeSlot(actor, EquipmentSlot.Accessory, statWeights);
        }

        void TryUpgradeWeapon(Actor actor)
        {
            actor.Equipment.EquippedMasters.TryGetValue(EquipmentSlot.Weapon, out var currentEquipment);
            var currentScore = currentEquipment != null ? CalculateWeaponScore(currentEquipment, actor.Equipment.Weapon, actor) : -1;

            EquipmentMaster bestCandidate = null;
            WeaponMaster bestWeaponMaster = null;
            var bestScore = currentScore;

            foreach (var inventorySlot in actor.Inventory.Slots)
            {
                if (!masterRepository.EquipmentMasters.TryGetValue(inventorySlot.ItemId, out var candidate) ||
                    candidate.Slot != EquipmentSlot.Weapon)
                {
                    continue;
                }

                var weaponMaster = masterRepository.GetWeaponMaster(candidate.ItemId);
                var score = CalculateWeaponScore(candidate, weaponMaster, actor);
                if (bestScore < score)
                {
                    bestScore = score;
                    bestCandidate = candidate;
                    bestWeaponMaster = weaponMaster;
                }
            }

            if (bestCandidate == null)
            {
                return;
            }

            EquipCandidate(actor, EquipmentSlot.Weapon, currentEquipment, bestCandidate, bestWeaponMaster);
        }

        void TryUpgradeSlot(Actor actor, EquipmentSlot slot, IReadOnlyList<(StatType stat, int weight)> statWeights)
        {
            actor.Equipment.EquippedMasters.TryGetValue(slot, out var currentEquipment);
            var currentScore = currentEquipment != null ? CalculateEquipmentScore(currentEquipment, statWeights) : -1;

            EquipmentMaster bestCandidate = null;
            var bestScore = currentScore;

            foreach (var inventorySlot in actor.Inventory.Slots)
            {
                if (!masterRepository.EquipmentMasters.TryGetValue(inventorySlot.ItemId, out var candidate))
                {
                    continue;
                }

                if (candidate.Slot != slot)
                {
                    continue;
                }

                var score = CalculateEquipmentScore(candidate, statWeights);
                if (bestScore < score)
                {
                    bestScore = score;
                    bestCandidate = candidate;
                }
            }

            if (bestCandidate == null)
            {
                return;
            }

            EquipCandidate(actor, slot, currentEquipment, bestCandidate, null);
        }

        void EquipCandidate(
            Actor actor,
            EquipmentSlot slot,
            EquipmentMaster currentEquipment,
            EquipmentMaster bestCandidate,
            WeaponMaster weaponMaster)
        {
            if (currentEquipment != null && !actor.Inventory.CanAdd(new ItemStack(currentEquipment.ItemId, 1)))
            {
                return;
            }

            var previousItemId = currentEquipment?.ItemId;

            if (currentEquipment != null)
            {
                actor.Unequip(slot);
                actor.Inventory.Add(new ItemStack(currentEquipment.ItemId, 1));
            }

            actor.Inventory.Remove(new ItemStack(bestCandidate.ItemId, 1));

            if (slot == EquipmentSlot.Weapon)
            {
                actor.Equip(bestCandidate, weaponMaster);
            }
            else
            {
                actor.Equip(bestCandidate);
            }

            eventBus.Publish(new EquipmentChanged(actor.Id, slot, previousItemId, bestCandidate.ItemId));
        }

        static int CalculateWeaponScore(EquipmentMaster equipment, WeaponMaster weaponMaster, Actor actor)
        {
            var statWeights = WeaponCalculatorFactory.GetStatWeights(weaponMaster.WeaponType);
            return weaponMaster.Attack * 10
                + CalculateWeightedStatBonus(equipment, statWeights)
                + CalculateWeaponTypeAffinity(weaponMaster, actor) * 2;
        }

        static int CalculateWeaponTypeAffinity(WeaponMaster weaponMaster, Actor actor)
        {
            var score = 0;
            foreach (var (stat, weight) in WeaponCalculatorFactory.GetStatWeights(weaponMaster.WeaponType))
            {
                score += GetActorStat(actor, stat) * weight;
            }

            return score;
        }

        static int CalculateEquipmentScore(EquipmentMaster equipment, IReadOnlyList<(StatType stat, int weight)> statWeights)
        {
            return equipment.Defense * 10 + CalculateWeightedStatBonus(equipment, statWeights);
        }

        static int CalculateWeightedStatBonus(EquipmentMaster equipment, IReadOnlyList<(StatType stat, int weight)> statWeights)
        {
            var score = 0;
            foreach (var (stat, weight) in statWeights)
            {
                foreach (var bonus in equipment.StatBonuses)
                {
                    if (bonus.StatType == stat)
                    {
                        score += bonus.Amount * weight;
                    }
                }
            }

            return score;
        }

        static int GetActorStat(Actor actor, StatType statType)
        {
            switch (statType)
            {
                case StatType.Strength:
                    return actor.Stats.Strength;
                case StatType.Dexterity:
                    return actor.Stats.Dexterity;
                case StatType.Constitution:
                    return actor.Stats.Constitution;
                case StatType.Intelligence:
                    return actor.Stats.Intelligence;
                case StatType.Wisdom:
                    return actor.Stats.Wisdom;
                case StatType.Charisma:
                    return actor.Stats.Charisma;
                default:
                    throw new ArgumentOutOfRangeException(nameof(statType));
            }
        }
    }
}
