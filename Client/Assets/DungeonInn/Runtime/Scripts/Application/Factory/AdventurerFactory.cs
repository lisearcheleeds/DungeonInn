using System;
using DungeonInn.Domain.Actor;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.Factory
{
    public sealed class AdventurerFactory : IAdventurerFactory
    {
        readonly IMasterRepository masterRepository;

        [Inject]
        public AdventurerFactory(IMasterRepository masterRepository)
        {
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
        }

        public Actor Create(AdventurerCreateRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var archetypeMaster = masterRepository.GetActorArchetypeMaster(request.ArchetypeId);
            if (archetypeMaster.BehaviorType != ActorBehaviorType.Adventurer)
            {
                throw new InvalidOperationException("Adventurer factory requires adventurer actor archetype.");
            }

            var levelTable = masterRepository.GetLevelTable(archetypeMaster.LevelTableId);
            var actor = ActorFactoryCore.CreateActor(
                request.ActorId,
                archetypeMaster,
                levelTable,
                request.Position,
                request.Faction,
                request.PreferenceSeed,
                new AdventurerBehavior(0, AdventurerLifecycleState.Arrived),
                masterRepository);
            actor.Inventory.AddRange(archetypeMaster.InitialInventoryItemIds);
            return actor;
        }
    }
}
