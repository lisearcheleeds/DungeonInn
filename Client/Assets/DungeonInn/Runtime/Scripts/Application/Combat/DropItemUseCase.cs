using DungeonInn.Application.World;
using System;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Item;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.Combat
{
    public sealed class DropItemUseCase
    {
        readonly IGameRandom gameRandom;
        readonly IEventPublisher eventPublisher;
        readonly IMasterRepository masterRepository;

        [Inject]
        public DropItemUseCase(
            IGameRandom gameRandom,
            IEventPublisher eventPublisher,
            IMasterRepository masterRepository)
        {
            this.gameRandom = gameRandom ?? throw new ArgumentNullException(nameof(gameRandom));
            this.eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
        }

        public void Execute(Actor defeatedActor, IGameWorldState worldState)
        {
            Execute(defeatedActor, worldState, eventPublisher);
        }

        public void Execute(Actor defeatedActor, IGameWorldState worldState, IEventPublisher eventPublisher)
        {
            if (defeatedActor == null)
            {
                throw new ArgumentNullException(nameof(defeatedActor));
            }

            if (worldState == null)
            {
                throw new ArgumentNullException(nameof(worldState));
            }

            if (eventPublisher == null)
            {
                throw new ArgumentNullException(nameof(eventPublisher));
            }

            if (defeatedActor.Behavior is not MonsterBehavior)
            {
                return;
            }

            var archetypeMaster = masterRepository.GetActorArchetypeMaster(defeatedActor.ArchetypeId);
            var speciesMaster = masterRepository.GetSpeciesMaster(archetypeMaster.SpeciesId);
            foreach (var entry in speciesMaster.SpeciesDrops)
            {
                var roll = gameRandom.Next(0, 10000) / 10000f;
                if (entry.Probability <= roll)
                {
                    continue;
                }

                var count = entry.MinCount == entry.MaxCount
                    ? entry.MinCount
                    : gameRandom.Next(entry.MinCount, entry.MaxCount + 1);

                var instance = new ItemInstance(Guid.NewGuid(), new ItemStack(entry.ItemId, count), defeatedActor.Position);
                worldState.AddItem(instance);
                eventPublisher.Publish(new ItemDropped(defeatedActor.Id, instance));
            }
        }
    }
}
