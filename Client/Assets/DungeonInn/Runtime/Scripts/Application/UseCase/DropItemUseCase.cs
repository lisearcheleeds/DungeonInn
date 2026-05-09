using System;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Item;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class DropItemUseCase
    {
        readonly IMasterRepository masterRepository;
        readonly IGameRandom gameRandom;
        readonly IGameEventBus eventBus;

        [Inject]
        public DropItemUseCase(
            IMasterRepository masterRepository,
            IGameRandom gameRandom,
            IGameEventBus eventBus)
        {
            this.masterRepository = masterRepository
                ?? throw new ArgumentNullException(nameof(masterRepository));
            this.gameRandom = gameRandom
                ?? throw new ArgumentNullException(nameof(gameRandom));
            this.eventBus = eventBus
                ?? throw new ArgumentNullException(nameof(eventBus));
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

                for (var i = 0; i < count; i++)
                {
                    var itemMaster = masterRepository.GetItemMaster(entry.ItemId);
                    var instance = new ItemInstance(Guid.NewGuid(), entry.ItemId, defeatedActor.Position);
                    worldState.AddItem(instance);
                    eventBus.Publish(new ItemDropped(
                        defeatedActor.Id,
                        instance.InstanceId,
                        entry.ItemId,
                        itemMaster.Name,
                        defeatedActor.Position));
                }
            }
        }
    }
}
