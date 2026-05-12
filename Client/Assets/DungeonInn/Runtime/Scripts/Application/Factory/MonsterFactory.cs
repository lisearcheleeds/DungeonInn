using System;
using DungeonInn.Domain.Actor;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.Factory
{
    public sealed class MonsterFactory : IMonsterFactory
    {
        readonly IActorFactory actorFactory;

        [Inject]
        public MonsterFactory(IActorFactory actorFactory)
        {
            this.actorFactory = actorFactory ?? throw new ArgumentNullException(nameof(actorFactory));
        }

        public MonsterFactory(IMasterRepository masterRepository)
            : this(new ActorFactory(masterRepository))
        {
        }

        public Actor Create(MonsterCreateRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return actorFactory.Create(new ActorFactoryRequest(
                request.ArchetypeId,
                request.ActorId,
                request.Position,
                request.Faction,
                request.PreferenceSeed,
                ActorBehaviorType.Monster));
        }
    }
}
