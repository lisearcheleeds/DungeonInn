using System;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Item;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class DropItemUseCase
    {
        readonly IGameRandom gameRandom;
        readonly IEventPublisher eventBus;

        [Inject]
        public DropItemUseCase(IGameRandom gameRandom, IEventPublisher eventBus)
        {
            this.gameRandom = gameRandom ?? throw new ArgumentNullException(nameof(gameRandom));
            this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public void Execute(Actor defeatedActor, IGameWorldState worldState)
        {
            if (defeatedActor == null)
            {
                throw new ArgumentNullException(nameof(defeatedActor));
            }

            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            if (defeatedActor.Behavior is not IActorDropSource dropSource)
            {
                return;
            }

            foreach (var entry in dropSource.DropTable)
            {
                var roll = gameRandom.Next(0, 10000) / 10000f;
                if (roll >= entry.Probability)
                {
                    continue;
                }

                var count = entry.MinCount == entry.MaxCount
                    ? entry.MinCount
                    : gameRandom.Next(entry.MinCount, entry.MaxCount + 1);

                var instance = new ItemInstance(Guid.NewGuid(), new ItemStack(entry.ItemId, count), defeatedActor.Position);
                worldState.AddItem(instance);
                eventBus.Publish(new ItemDropped(defeatedActor.Id, instance));
            }
        }
    }
}
