using System;
using System.Collections.Generic;
using DungeonInn.Application.Actors.Lifecycle;
using DungeonInn.Application.World;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Item;
using VContainer;

namespace DungeonInn.Application.Items
{
    public sealed class PickUpItemUseCase
    {
        readonly IEventPublisher eventBus;
        readonly ItemSpatialIndexService itemSpatialIndexService;
        readonly ActorProcessingCandidateService candidateService;
        readonly ActorSimulationSettings actorSimulationSettings;
        readonly CombatBalanceSettings combatBalanceSettings;
        readonly List<Guid> actorIdBuffer = new();
        readonly List<ItemInstance> itemBuffer = new();

        [Inject]
        public PickUpItemUseCase(
            IEventPublisher eventBus,
            ItemSpatialIndexService itemSpatialIndexService,
            ActorProcessingCandidateService candidateService,
            ActorSimulationSettings actorSimulationSettings,
            CombatBalanceSettings combatBalanceSettings)
        {
            this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            this.itemSpatialIndexService = itemSpatialIndexService
                ?? throw new ArgumentNullException(nameof(itemSpatialIndexService));
            this.candidateService = candidateService ?? throw new ArgumentNullException(nameof(candidateService));
            this.actorSimulationSettings = actorSimulationSettings
                ?? throw new ArgumentNullException(nameof(actorSimulationSettings));
            this.combatBalanceSettings = combatBalanceSettings ?? throw new ArgumentNullException(nameof(combatBalanceSettings));
        }

        public void Execute(IGameWorldState worldState)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            candidateService.CollectItemPickupCandidates(actorIdBuffer);
            foreach (var actorId in actorIdBuffer)
            {
                var actor = worldState.FindActor(actorId);
                if (actor == null)
                {
                    candidateService.RemoveActor(actorId);
                    continue;
                }

                if (actor.Hp <= 0 || actor.Behavior is not AdventurerBehavior behavior)
                {
                    candidateService.RemoveActor(actor.Id);
                    continue;
                }

                if (behavior.LifecycleState != AdventurerLifecycleState.Exploring)
                {
                    candidateService.RemoveActor(actor.Id);
                    continue;
                }

                PickUpNearbyItems(actor, worldState);
            }
        }

        void PickUpNearbyItems(Actor actor, IGameWorldState worldState)
        {
            var pickupRadius = actorSimulationSettings.ItemPickupRadiusMeters;
            var pickupRadiusSq = pickupRadius * pickupRadius;
            var neighborCellRadius = Math.Max(
                0,
                (int)Math.Ceiling(pickupRadius / combatBalanceSettings.SpatialIndexCellSizeMeters));

            itemBuffer.Clear();
            itemSpatialIndexService.CollectNearbyItems(actor.Position, neighborCellRadius, itemBuffer);
            for (var i = itemBuffer.Count - 1; 0 <= i; i--)
            {
                var item = itemBuffer[i];
                if (!actor.Position.LayerId.Equals(item.Position.LayerId))
                {
                    continue;
                }

                if (pickupRadiusSq < actor.Position.DistanceSquaredTo(item.Position))
                {
                    continue;
                }

                if (!actor.Inventory.CanAdd(item.Stack))
                {
                    continue;
                }

                actor.GainItem(item.Stack);
                worldState.RemoveItem(item.InstanceId);
                eventBus.Publish(new ItemPickedUp(actor.Id, item));
            }
        }
    }
}
