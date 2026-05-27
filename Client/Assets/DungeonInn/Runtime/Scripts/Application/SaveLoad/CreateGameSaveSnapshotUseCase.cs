using System;
using System.Collections.Generic;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.World;
using DungeonInn.Domain.Actor;
using DungeonInn.Domain.Facility;
using DungeonInn.Domain.Guild;
using DungeonInn.Domain.Item;
using DungeonInn.GameSession;
using VContainer;

namespace DungeonInn.Application.SaveLoad
{
    public sealed class CreateGameSaveSnapshotUseCase
    {
        readonly IGameClock gameClock;
        readonly IGameWorldStateReader gameWorldState;
        readonly GameSessionStartRequestStore startRequestStore;
        readonly TutorialProgressService tutorialProgressService;

        [Inject]
        public CreateGameSaveSnapshotUseCase(
            IGameClock gameClock,
            IGameWorldStateReader gameWorldState,
            GameSessionStartRequestStore startRequestStore,
            TutorialProgressService tutorialProgressService)
        {
            this.gameClock = gameClock ?? throw new ArgumentNullException(nameof(gameClock));
            this.gameWorldState = gameWorldState ?? throw new ArgumentNullException(nameof(gameWorldState));
            this.startRequestStore = startRequestStore
                ?? throw new ArgumentNullException(nameof(startRequestStore));
            this.tutorialProgressService = tutorialProgressService
                ?? throw new ArgumentNullException(nameof(tutorialProgressService));
        }

        public GameSaveData Execute(int slotId)
        {
            var seed = startRequestStore.Current.DungeonSeed;
            return new GameSaveData
            {
                version = 1,
                metadata = new GameSaveMetadata
                {
                    slotId = slotId,
                    savedAtUtc = DateTime.UtcNow.ToString("O"),
                    displayDay = gameClock.CurrentDay + 1,
                    seed = seed,
                    isLatest = true
                },
                clock = CreateClockSaveData(),
                guild = CreateGuildSaveData(gameWorldState.Guild, gameWorldState.InnEconomy),
                actors = CreateActorSaveData(),
                tutorial = tutorialProgressService.CreateSaveData(),
                newGameSeed = seed
            };
        }

        ActorSaveData[] CreateActorSaveData()
        {
            var actors = new List<ActorSaveData>();
            foreach (var actor in gameWorldState.Actors)
            {
                if (actor.Behavior is MonsterBehavior || actor.Behavior is PetBehavior)
                {
                    continue;
                }

                actors.Add(CreateActorSaveData(actor));
            }

            return actors.ToArray();
        }

        static ActorSaveData CreateActorSaveData(Actor actor)
        {
            return new ActorSaveData
            {
                id = actor.Id.ToString(),
                archetypeId = actor.ArchetypeId,
                stats = CreateActorStatsSaveData(actor.Stats),
                inventory = CreateInventorySaveData(actor.Inventory),
                level = actor.Level,
                experience = actor.Experience,
                hp = actor.Hp,
                mp = actor.Mp,
                fatigue = actor.Fatigue,
                injurySeverity = actor.InjurySeverity,
                preferenceSeed = actor.PreferenceSeed,
                behaviorType = (int)ResolveBehaviorType(actor),
                adventurer = CreateAdventurerSaveData(actor.Behavior as AdventurerBehavior),
                guildStaff = CreateGuildStaffSaveData(actor.Behavior as GuildStaffBehavior),
                naturalWeaponType = (int)actor.NaturalWeaponType,
                equippedItemIds = CreateEquippedItemIds(actor)
            };
        }

        static ActorStatsSaveData CreateActorStatsSaveData(ActorStats stats)
        {
            return new ActorStatsSaveData
            {
                strength = stats.Strength,
                dexterity = stats.Dexterity,
                constitution = stats.Constitution,
                intelligence = stats.Intelligence,
                wisdom = stats.Wisdom,
                charisma = stats.Charisma
            };
        }

        static AdventurerBehaviorSaveData CreateAdventurerSaveData(AdventurerBehavior behavior)
        {
            if (behavior == null)
            {
                return null;
            }

            return new AdventurerBehaviorSaveData
            {
                stress = behavior.Stress,
                lifecycleState = (int)AdventurerLifecycleState.Arrived,
                targetFloorDepth = 1
            };
        }

        static GuildStaffBehaviorSaveData CreateGuildStaffSaveData(GuildStaffBehavior behavior)
        {
            if (behavior == null)
            {
                return null;
            }

            var salary = new List<InventorySlotSaveData>();
            foreach (var itemStack in behavior.Salary)
            {
                salary.Add(new InventorySlotSaveData
                {
                    itemId = itemStack.ItemId,
                    count = itemStack.Count
                });
            }

            return new GuildStaffBehaviorSaveData
            {
                salary = salary.ToArray()
            };
        }

        static int[] CreateEquippedItemIds(Actor actor)
        {
            var itemIds = new List<int>();
            AddEquippedItemId(actor, EquipmentSlot.Weapon, itemIds);
            AddEquippedItemId(actor, EquipmentSlot.Armor, itemIds);
            AddEquippedItemId(actor, EquipmentSlot.Accessory, itemIds);
            return itemIds.ToArray();
        }

        static void AddEquippedItemId(Actor actor, EquipmentSlot slot, List<int> itemIds)
        {
            var itemId = actor.Equipment.GetEquippedItemId(slot);
            if (itemId.HasValue)
            {
                itemIds.Add(itemId.Value);
            }
        }

        static ActorBehaviorType ResolveBehaviorType(Actor actor)
        {
            if (actor.Behavior is AdventurerBehavior)
            {
                return ActorBehaviorType.Adventurer;
            }

            if (actor.Behavior is GuildStaffBehavior)
            {
                return ActorBehaviorType.GuildStaff;
            }

            return ActorBehaviorType.None;
        }

        GameClockSaveData CreateClockSaveData()
        {
            return new GameClockSaveData
            {
                totalScheduleTick = gameClock.TotalScheduleTick,
                elapsedRealTimeSeconds = gameClock.ElapsedRealTimeSeconds,
                elapsedGameTimeSeconds = gameClock.ElapsedGameTimeSeconds,
                timeScale = gameClock.TimeScale,
                isPaused = gameClock.IsPaused
            };
        }

        static GuildSaveData CreateGuildSaveData(AdventurerGuild guild, InnEconomyState innEconomy)
        {
            var facilities = new List<FacilitySaveData>();
            foreach (var facility in guild.Facilities)
            {
                facilities.Add(CreateFacilitySaveData(facility));
            }

            return new GuildSaveData
            {
                id = guild.Id.ToString(),
                inventory = CreateInventorySaveData(guild.Inventory),
                facilities = facilities.ToArray(),
                reputation = innEconomy.Reputation
            };
        }

        static FacilitySaveData CreateFacilitySaveData(Facility facility)
        {
            return new FacilitySaveData
            {
                id = facility.Id.ToString(),
                type = (int)facility.Type,
                name = facility.Name,
                basePrice = facility.BasePrice,
                staffPoint = facility.StaffPoint,
                level = facility.Level,
                quality = facility.Quality,
                capacity = facility.Capacity,
                inventory = CreateInventorySaveData(facility.Inventory)
            };
        }

        static InventorySaveData CreateInventorySaveData(IReadOnlyInventory inventory)
        {
            var slots = new List<InventorySlotSaveData>();
            foreach (var slot in inventory.Slots)
            {
                slots.Add(new InventorySlotSaveData
                {
                    itemId = slot.ItemId,
                    count = slot.Count
                });
            }

            return new InventorySaveData
            {
                maxSlotCount = inventory.MaxSlotCount,
                slots = slots.ToArray()
            };
        }
    }
}
