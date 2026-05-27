using System.Collections.Generic;

namespace DungeonInn.Application.SaveLoad
{
    public interface IGameSaveRepository
    {
        int SlotCount { get; }
        void Save(int slotId, GameSaveData saveData);
        bool TryLoad(int slotId, out GameSaveData saveData);
        IReadOnlyList<GameSaveSlotSummary> GetSlotSummaries();
        bool TryGetLatestSlot(out GameSaveSlotSummary summary);
    }
}
