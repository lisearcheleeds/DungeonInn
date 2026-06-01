using System;
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Actor;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.Actors.Spawn
{
    public sealed class SpawnAdventurerUseCase
    {
        readonly IActorFactory actorFactory;
        readonly IMasterRepository masterRepository;
        readonly CompleteActorSpawnUseCase completeActorSpawnUseCase;

        [Inject]
        public SpawnAdventurerUseCase(
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

            if (request.RequiredBehaviorType != ActorBehaviorType.Adventurer)
            {
                throw new InvalidOperationException("Spawn adventurer requires adventurer actor request.");
            }

            var archetypeMaster = masterRepository.GetActorArchetypeMaster(request.ArchetypeId);
            if (archetypeMaster.BehaviorType != ActorBehaviorType.Adventurer)
            {
                throw new InvalidOperationException("Spawn adventurer requires adventurer actor archetype.");
            }

            var actor = actorFactory.Create(request);

            if (actor.Level == 1)
            {
                actor.RequireBehavior<AdventurerBehavior>().ChangeLifecycleState(AdventurerLifecycleState.Arrived);
            }
            else if (actor.Level < 5)
            {
                throw new InvalidOperationException("Only level 1 or level 5 and higher adventurers can spawn.");
            }
            else
            {
                actor.RequireBehavior<AdventurerBehavior>().ChangeLifecycleState(AdventurerLifecycleState.Arrived);
            }
            completeActorSpawnUseCase.Complete(
                actor,
                archetypeMaster,
                request.DisplayName,
                request.AdventurerSpawnMasterId);
            return UniTask.FromResult(actor);
        }
    }
}
