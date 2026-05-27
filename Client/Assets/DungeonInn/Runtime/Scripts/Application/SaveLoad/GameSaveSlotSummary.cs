namespace DungeonInn.Application.SaveLoad
{
    public sealed class GameSaveSlotSummary
    {
        public int SlotId { get; }
        public bool IsEmpty { get; }
        public bool IsLatest { get; }
        public string SavedAtUtc { get; }
        public int DisplayDay { get; }
        public int Seed { get; }

        public GameSaveSlotSummary(
            int slotId,
            bool isEmpty,
            bool isLatest,
            string savedAtUtc,
            int displayDay,
            int seed)
        {
            SlotId = slotId;
            IsEmpty = isEmpty;
            IsLatest = isLatest;
            SavedAtUtc = savedAtUtc;
            DisplayDay = displayDay;
            Seed = seed;
        }

        public static GameSaveSlotSummary Empty(int slotId)
        {
            return new GameSaveSlotSummary(slotId, true, false, string.Empty, 0, 0);
        }
    }
}
