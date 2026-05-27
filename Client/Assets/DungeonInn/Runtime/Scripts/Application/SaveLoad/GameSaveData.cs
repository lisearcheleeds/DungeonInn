using System;

namespace DungeonInn.Application.SaveLoad
{
    [Serializable]
    public sealed class GameSaveData
    {
        public int version;
        public GameSaveMetadata metadata;
        public GameClockSaveData clock;
        public GuildSaveData guild;
        public ActorSaveData[] actors;
        public TutorialSaveData tutorial;
        public int newGameSeed;
    }

    [Serializable]
    public sealed class GameSaveMetadata
    {
        public int slotId;
        public string savedAtUtc;
        public int displayDay;
        public int seed;
        public bool isLatest;
    }

    [Serializable]
    public sealed class GameClockSaveData
    {
        public int totalScheduleTick;
        public float elapsedRealTimeSeconds;
        public float elapsedGameTimeSeconds;
        public float timeScale;
        public bool isPaused;
    }

    [Serializable]
    public sealed class GuildSaveData
    {
        public string id;
        public InventorySaveData inventory;
        public FacilitySaveData[] facilities;
        public int reputation;
    }

    [Serializable]
    public sealed class FacilitySaveData
    {
        public string id;
        public int type;
        public string name;
        public int basePrice;
        public int staffPoint;
        public int level;
        public int quality;
        public int capacity;
        public InventorySaveData inventory;
    }

    [Serializable]
    public sealed class InventorySaveData
    {
        public int maxSlotCount;
        public InventorySlotSaveData[] slots;
    }

    [Serializable]
    public sealed class InventorySlotSaveData
    {
        public int itemId;
        public int count;
    }

    [Serializable]
    public sealed class TutorialSaveData
    {
        public string[] seenStepIds;
    }

    [Serializable]
    public sealed class ActorSaveData
    {
        public string id;
        public int archetypeId;
        public ActorStatsSaveData stats;
        public InventorySaveData inventory;
        public int level;
        public int experience;
        public int hp;
        public int mp;
        public int fatigue;
        public int injurySeverity;
        public int preferenceSeed;
        public int behaviorType;
        public AdventurerBehaviorSaveData adventurer;
        public GuildStaffBehaviorSaveData guildStaff;
        public int naturalWeaponType;
        public int[] equippedItemIds;
    }

    [Serializable]
    public sealed class ActorStatsSaveData
    {
        public int strength;
        public int dexterity;
        public int constitution;
        public int intelligence;
        public int wisdom;
        public int charisma;
    }

    [Serializable]
    public sealed class AdventurerBehaviorSaveData
    {
        public int stress;
        public int lifecycleState;
        public int targetFloorDepth;
    }

    [Serializable]
    public sealed class GuildStaffBehaviorSaveData
    {
        public InventorySlotSaveData[] salary;
    }
}
