using System;
using DungeonInn.Domain.Actor;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.Factory
{
    public sealed class MonsterFactory : IMonsterFactory
    {
        readonly IMasterRepository masterRepository;

        [Inject]
        public MonsterFactory(IMasterRepository masterRepository)
        {
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
        }

        public Actor Create(MonsterCreateRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var speciesMaster = masterRepository.GetMonsterSpeciesMaster(request.SpeciesId);
            var archetypeMaster = masterRepository.GetActorArchetypeMaster(speciesMaster.ActorArchetypeId);
            if (archetypeMaster.BehaviorType != ActorBehaviorType.Monster)
            {
                throw new InvalidOperationException("Monster factory requires monster actor archetype.");
            }

            var levelTable = masterRepository.GetLevelTable(archetypeMaster.LevelTableId);
            var actor = ActorFactoryCore.CreateActor(
                request.ActorId,
                archetypeMaster,
                levelTable,
                request.Position,
                request.Faction,
                request.PreferenceSeed,
                new MonsterBehavior(speciesMaster.Id, speciesMaster.CanScavenge, speciesMaster.SpeciesDrops),
                masterRepository);
            actor.ChangeNaturalWeaponType(masterRepository.GetWeaponTypeCombatMaster(speciesMaster.DefaultWeaponType));
            actor.Inventory.AddRange(archetypeMaster.InitialInventoryItemIds);
            return actor;
        }
    }
}
