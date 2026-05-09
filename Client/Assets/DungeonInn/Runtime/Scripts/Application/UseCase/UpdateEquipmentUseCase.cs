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
        readonly IGameEventBus eventBus;

        static readonly EquipmentSlot[] EquippableSlots =
        {
            EquipmentSlot.Weapon,
            EquipmentSlot.Armor,
            EquipmentSlot.Accessory
        };

        [Inject]
        public UpdateEquipmentUseCase(IItemMasterRepository masterRepository, IGameEventBus eventBus)
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
                    behavior.LifecycleState != AdventurerLifecycleState.Recovering)
                {
                    continue;
                }

                UpdateEquipment(actor);
            }
        }

        void UpdateEquipment(Actor actor)
        {
            var weaponType = actor.Equipment.Weapon?.WeaponType ?? actor.NaturalWeaponType;
            var statWeights = WeaponCalculatorFactory.GetStatWeights(weaponType);

            foreach (var slot in EquippableSlots)
            {
                TryUpgradeSlot(actor, slot, statWeights);
            }
        }

        void TryUpgradeSlot(Actor actor, EquipmentSlot slot, IReadOnlyList<(StatType stat, int weight)> statWeights)
        {
            actor.Equipment.EquippedMasters.TryGetValue(slot, out var currentEquipment);
            var currentScore = currentEquipment != null ? CalculateScore(currentEquipment, statWeights) : -1;

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

                var score = CalculateScore(candidate, statWeights);
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
                var weaponMaster = masterRepository.GetWeaponMaster(bestCandidate.ItemId);
                actor.Equip(bestCandidate, weaponMaster);
            }
            else
            {
                actor.Equip(bestCandidate);
            }

            eventBus.Publish(new EquipmentChanged(actor.Id, slot, previousItemId, bestCandidate.ItemId));
        }

        static int CalculateScore(EquipmentMaster equipment, IReadOnlyList<(StatType stat, int weight)> statWeights)
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
    }
}
