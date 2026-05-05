using System;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Item;
using DungeonInn.Master;

namespace DungeonInn.Application.Factory
{
    public sealed class MasterActorFactory : IActorFactory
    {
        readonly IMasterRepository masterRepository;

        public MasterActorFactory(IMasterRepository masterRepository)
        {
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
        }

        public Actor CreateActor(ActorCreateRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var archetypeMaster = masterRepository.GetActorArchetypeMaster(request.ArchetypeId);
            if (archetypeMaster.BehaviorType == ActorBehaviorType.Monster)
            {
                throw new InvalidOperationException("Monster actor requires monster species master.");
            }

            var actor = CreateActorCore(
                request.ActorId,
                archetypeMaster,
                request.Position,
                request.Faction,
                request.PreferenceSeed,
                CreateBehavior(archetypeMaster.BehaviorType));
            if (request.ApplyInitialItems)
            {
                ApplyInitialItems(actor, archetypeMaster);
            }

            RecoverFully(actor);
            return actor;
        }

        public Actor CreateMonster(MonsterCreateRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var speciesMaster = masterRepository.GetMonsterSpeciesMaster(request.SpeciesId);
            var archetypeMaster = masterRepository.GetActorArchetypeMaster(speciesMaster.ActorArchetypeId);
            if (archetypeMaster.BehaviorType != ActorBehaviorType.Monster)
            {
                throw new InvalidOperationException("Monster species requires monster actor archetype.");
            }

            var actor = CreateActorCore(
                request.ActorId,
                archetypeMaster,
                request.Position,
                request.Faction,
                request.PreferenceSeed,
                new MonsterBehavior(speciesMaster.Id, speciesMaster.CanScavenge, speciesMaster.SpeciesDrops));
            actor.ChangeNaturalWeaponType(speciesMaster.DefaultWeaponType);
            ApplyInitialItems(actor, archetypeMaster);
            RecoverFully(actor);
            return actor;
        }

        static Actor CreateActorCore(
            Guid actorId,
            ActorArchetypeMaster archetypeMaster,
            DungeonInn.Domain.Map.LayerPosition position,
            ActorFaction faction,
            int preferenceSeed,
            IActorBehavior behavior)
        {
            return new Actor(
                actorId,
                archetypeMaster.Name,
                archetypeMaster.BaseStats,
                new Inventory(),
                archetypeMaster.InitialLevel,
                0,
                1,
                0,
                0,
                0,
                preferenceSeed,
                position,
                faction,
                behavior);
        }

        void ApplyInitialItems(Actor actor, ActorArchetypeMaster archetypeMaster)
        {
            foreach (var itemId in archetypeMaster.InitialInventoryItemIds)
            {
                masterRepository.GetItemMaster(itemId);
                actor.Inventory.Add(new ItemStack(itemId, 1));
            }

            foreach (var itemId in archetypeMaster.InitialEquipmentItemIds)
            {
                masterRepository.GetItemMaster(itemId);
                var equipmentMaster = masterRepository.GetEquipmentMaster(itemId);
                actor.Inventory.Add(new ItemStack(itemId, 1));
                Equip(actor, equipmentMaster);
            }
        }

        void Equip(Actor actor, EquipmentMaster equipmentMaster)
        {
            if (equipmentMaster.Slot == EquipmentSlot.Weapon)
            {
                actor.Equip(equipmentMaster, masterRepository.GetWeaponMaster(equipmentMaster.ItemId));
                return;
            }

            actor.Equip(equipmentMaster);
        }

        static IActorBehavior CreateBehavior(ActorBehaviorType behaviorType)
        {
            switch (behaviorType)
            {
                case ActorBehaviorType.Adventurer:
                    return new AdventurerBehavior(0, AdventurerLifecycleState.Arrived);
                case ActorBehaviorType.GuildStaff:
                    return new GuildStaffBehavior(Array.Empty<ItemStack>());
                case ActorBehaviorType.Pet:
                    throw new InvalidOperationException("Pet actor requires owner actor id.");
                case ActorBehaviorType.Monster:
                    throw new InvalidOperationException("Monster actor requires monster species master.");
                default:
                    throw new ArgumentOutOfRangeException(nameof(behaviorType));
            }
        }

        static void RecoverFully(Actor actor)
        {
            actor.Recover(actor.Params.MaxHp, actor.Params.MaxMp, 0, 0, 0);
        }
    }
}
