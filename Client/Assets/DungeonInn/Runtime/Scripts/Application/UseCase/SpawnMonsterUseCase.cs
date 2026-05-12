using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Factory;
using DungeonInn.Domain.Actor;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.UseCase
{
    public sealed class SpawnMonsterUseCase
    {
        readonly IMonsterFactory monsterFactory;
        readonly IMasterRepository masterRepository;
        readonly ActorSpawnCompletionService spawnCompletionService;

        [Inject]
        public SpawnMonsterUseCase(
            IMonsterFactory monsterFactory,
            IMasterRepository masterRepository,
            ActorSpawnCompletionService spawnCompletionService)
        {
            this.monsterFactory = monsterFactory ?? throw new ArgumentNullException(nameof(monsterFactory));
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
            this.spawnCompletionService = spawnCompletionService ?? throw new ArgumentNullException(nameof(spawnCompletionService));
        }

        public SpawnMonsterUseCase(
            IMonsterFactory monsterFactory,
            IMasterRepository masterRepository,
            DungeonInn.Application.Profiles.IActorProfileRegistry profileRegistry,
            DungeonInn.Application.Event.IEventPublisher eventBus)
            : this(
                monsterFactory,
                masterRepository,
                new ActorSpawnCompletionService(profileRegistry, eventBus))
        {
        }

        public UniTask<Actor> ExecuteAsync(MonsterCreateRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var archetypeMaster = masterRepository.GetActorArchetypeMaster(request.ArchetypeId);
            var actor = monsterFactory.Create(request);
            spawnCompletionService.Complete(actor, archetypeMaster, string.Empty);
            return UniTask.FromResult(actor);
        }
    }
}
