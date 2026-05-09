using System;
using System.Collections.Generic;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Common;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class PickUpItemUseCase
    {
        readonly IGameEventBus eventBus;

        [Inject]
        public PickUpItemUseCase(IGameEventBus eventBus)
        {
            this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public void Execute(IGameWorldState worldState)
        {
            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            var actors = new List<Actor>(worldState.Actors);
            foreach (var actor in actors)
            {
                if (actor.Hp <= 0 || actor.Behavior is not AdventurerBehavior behavior)
                {
                    continue;
                }

                if (behavior.LifecycleState != AdventurerLifecycleState.Exploring)
                {
                    continue;
                }

                PickUpNearbyItems(actor, worldState);
            }
        }

        void PickUpNearbyItems(Actor actor, IGameWorldState worldState)
        {
            var pickupRadius = GameConstants.AdventurerItemPickupRadiusMeters;
            var pickupRadiusSq = pickupRadius * pickupRadius;
            var items = new List<Domain.Item.ItemInstance>(worldState.Items);

            foreach (var item in items)
            {
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

                actor.Inventory.Add(item.Stack);
                worldState.RemoveItem(item.InstanceId);
                eventBus.Publish(new ItemPickedUp(actor.Id, item));
            }
        }
    }
}
