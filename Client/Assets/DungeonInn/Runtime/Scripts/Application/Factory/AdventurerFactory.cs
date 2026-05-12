using System;
using DungeonInn.Domain.Actor;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.Factory
{
    public sealed class AdventurerFactory : IAdventurerFactory
    {
        readonly IActorFactory actorFactory;

        [Inject]
        public AdventurerFactory(IActorFactory actorFactory)
        {
            this.actorFactory = actorFactory ?? throw new ArgumentNullException(nameof(actorFactory));
        }

        public AdventurerFactory(IMasterRepository masterRepository)
            : this(new ActorFactory(masterRepository))
        {
        }

        public Actor Create(AdventurerCreateRequest request)
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
                ActorBehaviorType.Adventurer));
        }
    }
}
