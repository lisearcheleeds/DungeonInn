using System;
using DungeonInn.Domain.Actor;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.Actors.Spawn
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
            var naturalWeaponTypeCombatMaster = masterRepository.GetWeaponTypeCombatMaster(archetypeMaster.DefaultWeaponType);
            var actor = ActorFactoryCore.CreateActor(
                request.ActorId,
                archetypeMaster,
                levelTable,
                request.Position,
                request.Faction,
                request.PreferenceSeed,
                CreateBehavior(archetypeMaster),
                masterRepository,
                naturalWeaponTypeCombatMaster);
            ApplyLoadout(actor, archetypeMaster);
            RecoverFully(actor);
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
                    return new MonsterBehavior(speciesMaster.Id);
                default:
                    throw new InvalidOperationException("Actor factory does not support this actor behavior type.");
            }
        }

        void ApplyLoadout(Actor actor, ActorArchetypeMaster archetypeMaster)
        {
            if (archetypeMaster.LoadoutMasterId < 1)
            {
                return;
            }

            var loadoutMaster = masterRepository.GetActorLoadoutMaster(archetypeMaster.LoadoutMasterId);
            EquipWeapon(actor, loadoutMaster.WeaponItemId);
            Equip(actor, loadoutMaster.ArmorItemId);
            foreach (var accessoryItemId in loadoutMaster.AccessoryItemIds)
            {
                Equip(actor, accessoryItemId);
            }

            actor.GainItems(loadoutMaster.InitialInventory);
        }

        void EquipWeapon(Actor actor, int itemId)
        {
            if (itemId < 1)
            {
                return;
            }

            actor.Equip(
                masterRepository.GetEquipmentMaster(itemId),
                masterRepository.GetWeaponMaster(itemId));
        }

        void Equip(Actor actor, int itemId)
        {
            if (itemId < 1)
            {
                return;
            }

            actor.Equip(masterRepository.GetEquipmentMaster(itemId));
        }

        static void RecoverFully(Actor actor)
        {
            actor.Recover(actor.Params.MaxHp, actor.Params.MaxMp, 0, 0, 0);
        }
    }
}

