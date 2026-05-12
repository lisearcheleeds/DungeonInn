using System;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Item;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.Factory
{
    public sealed class ActorFactory : IActorFactory
    {
        readonly IMasterRepository masterRepository;

        [Inject]
        public ActorFactory(IMasterRepository masterRepository)
        {
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
        }

        public Actor Create(ActorFactoryRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var archetypeMaster = masterRepository.GetActorArchetypeMaster(request.ArchetypeId);
            ValidateBehaviorType(archetypeMaster, request.RequiredBehaviorType);

            var levelTable = masterRepository.GetLevelTable(archetypeMaster.LevelTableId);
            var actor = ActorFactoryCore.CreateActor(
                request.ActorId,
                archetypeMaster,
                levelTable,
                request.Position,
                request.Faction,
                request.PreferenceSeed,
                CreateBehavior(archetypeMaster),
                masterRepository);

            actor.ChangeNaturalWeaponType(masterRepository.GetWeaponTypeCombatMaster(archetypeMaster.DefaultWeaponType));
            actor.Inventory.AddRange(archetypeMaster.InitialInventoryItemIds);
            return actor;
        }

        static void ValidateBehaviorType(
            ActorArchetypeMaster archetypeMaster,
            ActorBehaviorType requiredBehaviorType)
        {
            if (requiredBehaviorType == ActorBehaviorType.None)
            {
                return;
            }

            if (archetypeMaster.BehaviorType != requiredBehaviorType)
            {
                throw new InvalidOperationException("Actor factory request behavior type does not match actor archetype.");
            }
        }

        IActorBehavior CreateBehavior(ActorArchetypeMaster archetypeMaster)
        {
            switch (archetypeMaster.BehaviorType)
            {
                case ActorBehaviorType.Adventurer:
                    return new AdventurerBehavior(0, AdventurerLifecycleState.Arrived);
                case ActorBehaviorType.Monster:
                    var speciesMaster = masterRepository.GetSpeciesMaster(archetypeMaster.SpeciesId);
                    return new MonsterBehavior(speciesMaster.Id, speciesMaster.SpeciesDrops);
                default:
                    throw new InvalidOperationException("Actor factory does not support this actor behavior type.");
            }
        }
    }
}
