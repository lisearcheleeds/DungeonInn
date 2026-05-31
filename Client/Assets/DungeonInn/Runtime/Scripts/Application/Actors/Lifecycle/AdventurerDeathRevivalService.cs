using System;
using System.Collections.Generic;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.World;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Item;
using VContainer;

namespace DungeonInn.Application.Actors.Lifecycle
{
    public sealed class AdventurerDeathRevivalService
    {
        static readonly EquipmentSlot[] RevivalLostEquipmentSlots =
        {
            EquipmentSlot.Weapon,
            EquipmentSlot.Armor,
            EquipmentSlot.Accessory
        };

        readonly IGameClock gameClock;
        readonly ActorProcessingCandidateService candidateService;
        readonly List<ItemStack> lostInventoryBuffer = new();
        readonly List<int> lostEquipmentItemIdBuffer = new();

        [Inject]
        public AdventurerDeathRevivalService(
            IGameClock gameClock,
            ActorProcessingCandidateService candidateService)
        {
            this.gameClock = gameClock ?? throw new ArgumentNullException(nameof(gameClock));
            this.candidateService = candidateService ?? throw new ArgumentNullException(nameof(candidateService));
        }

        public bool CanReviveAtInn(IGameWorldState worldState, Actor actor)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            return actor.Behavior is AdventurerBehavior && TryFindInn(worldState, out _);
        }

        public void ReviveAtInn(IGameWorldState worldState, Actor adventurer, IEventPublisher eventPublisher)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            if (adventurer == null)
            {
                throw new ArgumentNullException(nameof(adventurer));
            }

            if (eventPublisher == null)
            {
                throw new ArgumentNullException(nameof(eventPublisher));
            }

            if (adventurer.Behavior is not AdventurerBehavior behavior)
            {
                throw new InvalidOperationException("Actor is not adventurer.");
            }

            if (!TryFindInn(worldState, out var inn))
            {
                throw new InvalidOperationException("Inn does not exist.");
            }

            var deathPosition = adventurer.Position;
            DropInventory(worldState, adventurer, deathPosition, eventPublisher);
            DropEquipment(worldState, adventurer, deathPosition, eventPublisher);

            var revivePosition = worldState.GroundMap.Layer.GetCellCenter(worldState.GroundMap.DungeonEntrancePosition);
            adventurer.MoveTo(revivePosition);
            adventurer.ChangeGoal(ActorGoal.None());
            adventurer.ChangePlan(ActorPlan.None());
            adventurer.ChangeAction(ActorAction.None());
            behavior.ClearWaitingForInn();
            behavior.ChangeLifecycleState(AdventurerLifecycleState.Recovering);

            worldState.RegisterActor(adventurer);
            var reservation = worldState.Guild.ReserveInnForRevival(
                Guid.NewGuid(),
                adventurer,
                inn.Id,
                gameClock.CurrentScheduleTick);
            eventPublisher.Publish(new ActorReservedInn(adventurer.Id, reservation.InnFacilityId));
            candidateService.MarkRecoveryCandidate(adventurer.Id);
        }

        void DropInventory(
            IGameWorldState worldState,
            Actor adventurer,
            Domain.Map.LayerPosition deathPosition,
            IEventPublisher eventPublisher)
        {
            lostInventoryBuffer.Clear();
            foreach (var kvp in adventurer.Inventory.ItemCounts)
            {
                lostInventoryBuffer.Add(new ItemStack(kvp.Key, kvp.Value));
            }

            foreach (var stack in lostInventoryBuffer)
            {
                adventurer.RemoveItem(stack);
                DropItem(worldState, adventurer, stack, deathPosition, eventPublisher);
            }
        }

        void DropEquipment(
            IGameWorldState worldState,
            Actor adventurer,
            Domain.Map.LayerPosition deathPosition,
            IEventPublisher eventPublisher)
        {
            lostEquipmentItemIdBuffer.Clear();
            foreach (var slot in RevivalLostEquipmentSlots)
            {
                var itemId = adventurer.Equipment.GetEquippedItemId(slot);
                if (!itemId.HasValue)
                {
                    continue;
                }

                lostEquipmentItemIdBuffer.Add(itemId.Value);
                adventurer.Unequip(slot);
            }

            foreach (var itemId in lostEquipmentItemIdBuffer)
            {
                DropItem(worldState, adventurer, new ItemStack(itemId, 1), deathPosition, eventPublisher);
            }
        }

        static void DropItem(
            IGameWorldState worldState,
            Actor adventurer,
            ItemStack stack,
            Domain.Map.LayerPosition position,
            IEventPublisher eventPublisher)
        {
            var instance = new ItemInstance(Guid.NewGuid(), stack, position);
            worldState.AddItem(instance);
            eventPublisher.Publish(new ItemDropped(adventurer.Id, instance));
        }

        static bool TryFindInn(IGameWorldState worldState, out Facility inn)
        {
            foreach (var facility in worldState.Guild.Facilities)
            {
                if (facility.Type != FacilityType.Inn)
                {
                    continue;
                }

                inn = facility;
                return true;
            }

            inn = null;
            return false;
        }
    }
}
