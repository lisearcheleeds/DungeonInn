using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Application.Actors.Spawn;
using DungeonInn.Domain.Actor;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.Actors.Spawn
{
    public sealed class SpawnMonsterUseCase
    {
        readonly IActorFactory actorFactory;
        readonly IMasterRepository masterRepository;
        readonly CompleteActorSpawnUseCase completeActorSpawnUseCase;

        [Inject]
        public SpawnMonsterUseCase(
            IActorFactory actorFactory,
            IMasterRepository masterRepository,
            CompleteActorSpawnUseCase completeActorSpawnUseCase)
        {
            this.actorFactory = actorFactory ?? throw new ArgumentNullException(nameof(actorFactory));
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
            this.completeActorSpawnUseCase = completeActorSpawnUseCase ?? throw new ArgumentNullException(nameof(completeActorSpawnUseCase));
        }

        public UniTask<Actor> ExecuteAsync(ActorFactoryRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.RequiredBehaviorType != ActorBehaviorType.Monster)
            {
                throw new InvalidOperationException("Spawn monster requires monster actor request.");
            }

            var archetypeMaster = masterRepository.GetActorArchetypeMaster(request.ArchetypeId);
            var actor = actorFactory.Create(request);
            completeActorSpawnUseCase.Complete(actor, archetypeMaster, string.Empty);
            return UniTask.FromResult(actor);
        }
    }
}
