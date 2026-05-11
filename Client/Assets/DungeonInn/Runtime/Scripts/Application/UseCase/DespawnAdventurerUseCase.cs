using System;
using DungeonInn.Application.Event;
using DungeonInn.Application.GameLoop;
using DungeonInn.Domain.Actor;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class DespawnAdventurerUseCase
    {
        readonly DespawnAdventurerService despawnAdventurerService;

        [Inject]
        public DespawnAdventurerUseCase(DespawnAdventurerService despawnAdventurerService)
        {
            this.despawnAdventurerService = despawnAdventurerService ?? throw new ArgumentNullException(nameof(despawnAdventurerService));
        }

        public DespawnAdventurerUseCase(IEventPublisher eventPublisher)
            : this(new DespawnAdventurerService(eventPublisher))
        {
        }

        public void Execute(IGameWorldState worldState, Actor actor, int waitedDays)
        {
            despawnAdventurerService.Execute(worldState, actor, waitedDays);
        }
    }
}
