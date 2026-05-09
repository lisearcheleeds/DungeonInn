using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Event;
using DungeonInn.Application.Event.Events;
using DungeonInn.Application.Factory;
using DungeonInn.Application.Profiles;
using DungeonInn.Domain.Actor;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class SpawnMonsterUseCase
    {
        readonly IMonsterFactory monsterFactory;
        readonly IMasterRepository masterRepository;
        readonly IActorProfileRegistry profileRegistry;
        readonly IGameEventBus eventBus;

        [Inject]
        public SpawnMonsterUseCase(
            IMonsterFactory monsterFactory,
            IMasterRepository masterRepository,
            IActorProfileRegistry profileRegistry,
            IGameEventBus eventBus)
        {
            this.monsterFactory = monsterFactory ?? throw new ArgumentNullException(nameof(monsterFactory));
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
            this.profileRegistry = profileRegistry ?? throw new ArgumentNullException(nameof(profileRegistry));
            this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public UniTask<Actor> ExecuteAsync(MonsterCreateRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var speciesMaster = masterRepository.GetMonsterSpeciesMaster(request.SpeciesId);
            var actor = monsterFactory.Create(request);
            profileRegistry.RegisterMonster(actor.Id, speciesMaster.Name, speciesMaster.Id);
            eventBus.Publish(new ActorSpawned(actor.Id));
            return UniTask.FromResult(actor);
        }
    }
}
