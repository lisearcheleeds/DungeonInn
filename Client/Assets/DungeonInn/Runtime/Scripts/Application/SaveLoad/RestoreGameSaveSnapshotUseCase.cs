using System;
using System.Collections.Generic;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.World;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Item;
using DungeonInn.Domain.Map;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.SaveLoad
{
    public sealed class RestoreGameSaveSnapshotUseCase
    {
        readonly IGameClockRestorer gameClockRestorer;
        readonly IGameWorldStateWriter gameWorldState;
        readonly IGameWorldStateReader gameWorldStateReader;
        readonly IItemStackLimitResolver stackLimitResolver;
        readonly IMasterRepository masterRepository;
        readonly TutorialProgressService tutorialProgressService;

        [Inject]
        public RestoreGameSaveSnapshotUseCase(
            IGameClockRestorer gameClockRestorer,
            IGameWorldStateWriter gameWorldState,
            IGameWorldStateReader gameWorldStateReader,
            IItemStackLimitResolver stackLimitResolver,
            IMasterRepository masterRepository,
            TutorialProgressService tutorialProgressService)
        {
            this.gameClockRestorer = gameClockRestorer
                ?? throw new ArgumentNullException(nameof(gameClockRestorer));
            this.gameWorldState = gameWorldState ?? throw new ArgumentNullException(nameof(gameWorldState));
            this.gameWorldStateReader = gameWorldStateReader
                ?? throw new ArgumentNullException(nameof(gameWorldStateReader));
            this.stackLimitResolver = stackLimitResolver
                ?? throw new ArgumentNullException(nameof(stackLimitResolver));
            this.masterRepository = masterRepository
                ?? throw new ArgumentNullException(nameof(masterRepository));
            this.tutorialProgressService = tutorialProgressService
                ?? throw new ArgumentNullException(nameof(tutorialProgressService));
        }

        public void Execute(GameSaveData saveData)
        {
            if (saveData == null)
            {
                throw new ArgumentNullException(nameof(saveData));
            }

            RestoreClock(saveData.clock);
            gameWorldState.RestoreGuild(
                CreateGuild(saveData.guild),
                new InnEconomyState(saveData.guild.reputation));
            RestoreActors(saveData.actors);
            tutorialProgressService.Restore(saveData.tutorial);
        }

        void RestoreClock(GameClockSaveData saveData)
        {
            if (saveData == null)
            {
                return;
            }

            gameClockRestorer.Restore(
                saveData.totalScheduleTick,
                saveData.elapsedRealTimeSeconds,
                saveData.elapsedGameTimeSeconds,
                saveData.timeScale,
                saveData.isPaused);
        }

        AdventurerGuild CreateGuild(GuildSaveData saveData)
        {
            if (saveData == null)
            {
                throw new ArgumentNullException(nameof(saveData));
            }

            var facilities = new List<Facility>();
            foreach (var facilitySaveData in saveData.facilities ?? Array.Empty<FacilitySaveData>())
            {
                facilities.Add(CreateFacility(facilitySaveData));
            }

            return new AdventurerGuild(
                Guid.Parse(saveData.id),
                CreateInventory(saveData.inventory),
                facilities);
        }

        Facility CreateFacility(FacilitySaveData saveData)
        {
            var facility = new Facility(
                Guid.Parse(saveData.id),
                (FacilityType)saveData.type,
                saveData.name,
                saveData.basePrice,
                saveData.capacity,
                CreateInventory(saveData.inventory));

            if (1 < saveData.level)
            {
                facility.UpgradeTo(saveData.level, saveData.quality, saveData.capacity);
            }

            facility.ApplyStaffPoint(saveData.staffPoint);
            return facility;
        }

        Inventory CreateInventory(InventorySaveData saveData)
        {
            var inventory = new Inventory(
                Math.Max(1, saveData.maxSlotCount),
                stackLimitResolver);
            foreach (var slot in saveData.slots ?? Array.Empty<InventorySlotSaveData>())
            {
                inventory.Add(new ItemStack(slot.itemId, slot.count));
            }

            return inventory;
        }

        void RestoreActors(ActorSaveData[] saveData)
        {
            foreach (var actorSaveData in saveData ?? Array.Empty<ActorSaveData>())
            {
                gameWorldState.RegisterActor(CreateActor(actorSaveData));
            }
        }

        Actor CreateActor(ActorSaveData saveData)
        {
            var naturalWeaponType = (WeaponType)saveData.naturalWeaponType;
            var actor = new Actor(
                Guid.Parse(saveData.id),
                saveData.archetypeId,
                CreateActorStats(saveData.stats),
                CreateInventory(saveData.inventory),
                saveData.level,
                saveData.experience,
                saveData.hp,
                saveData.mp,
                saveData.fatigue,
                saveData.injurySeverity,
                saveData.preferenceSeed,
                GetGroundStartPosition(),
                CreateFaction((ActorBehaviorType)saveData.behaviorType),
                CreateBehavior(saveData),
                masterRepository.GetWeaponTypeCombatMaster(naturalWeaponType));

            RestoreEquipment(actor, saveData.equippedItemIds);
            return actor;
        }

        LayerPosition GetGroundStartPosition()
        {
            return gameWorldStateReader.GroundMap.Layer.GetCellCenter(
                gameWorldStateReader.GroundMap.DungeonEntrancePosition);
        }

        static ActorStats CreateActorStats(ActorStatsSaveData saveData)
        {
            return new ActorStats(
                saveData.strength,
                saveData.dexterity,
                saveData.constitution,
                saveData.intelligence,
                saveData.wisdom,
                saveData.charisma);
        }

        static ActorFaction CreateFaction(ActorBehaviorType behaviorType)
        {
            switch (behaviorType)
            {
                case ActorBehaviorType.Adventurer:
                    return new ActorFaction(1, "Adventurer Guild");
                case ActorBehaviorType.GuildStaff:
                    return new ActorFaction(1, "Adventurer Guild");
                default:
                    throw new ArgumentOutOfRangeException(nameof(behaviorType));
            }
        }

        IActorBehavior CreateBehavior(ActorSaveData saveData)
        {
            var behaviorType = (ActorBehaviorType)saveData.behaviorType;
            switch (behaviorType)
            {
                case ActorBehaviorType.Adventurer:
                    var adventurer = saveData.adventurer;
                    return new AdventurerBehavior(
                        adventurer?.stress ?? 0,
                        AdventurerLifecycleState.Arrived);
                case ActorBehaviorType.GuildStaff:
                    return new GuildStaffBehavior(CreateSalary(saveData.guildStaff));
                default:
                    throw new ArgumentOutOfRangeException(nameof(saveData));
            }
        }

        IReadOnlyList<ItemStack> CreateSalary(GuildStaffBehaviorSaveData saveData)
        {
            var salary = new List<ItemStack>();
            foreach (var slot in saveData?.salary ?? Array.Empty<InventorySlotSaveData>())
            {
                salary.Add(new ItemStack(slot.itemId, slot.count));
            }

            return salary;
        }

        void RestoreEquipment(Actor actor, int[] equippedItemIds)
        {
            foreach (var itemId in equippedItemIds ?? Array.Empty<int>())
            {
                var equipmentMaster = masterRepository.GetEquipmentMaster(itemId);
                if (equipmentMaster.Slot == EquipmentSlot.Weapon)
                {
                    actor.Equip(equipmentMaster, masterRepository.GetWeaponMaster(itemId));
                    continue;
                }

                actor.Equip(equipmentMaster);
            }
        }
    }
}
