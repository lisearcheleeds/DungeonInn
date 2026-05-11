using System;
using DungeonInn.Application.Event;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class DropItemUseCase
    {
        readonly DropItemService dropItemService;

        [Inject]
        public DropItemUseCase(DropItemService dropItemService)
        {
            this.dropItemService = dropItemService ?? throw new ArgumentNullException(nameof(dropItemService));
        }

        public DropItemUseCase(IGameRandom gameRandom, IEventPublisher eventPublisher)
            : this(new DropItemService(gameRandom, eventPublisher))
        {
        }

        public void Execute(Actor defeatedActor, IGameWorldState worldState)
        {
            dropItemService.Execute(defeatedActor, worldState);
        }
    }
}
