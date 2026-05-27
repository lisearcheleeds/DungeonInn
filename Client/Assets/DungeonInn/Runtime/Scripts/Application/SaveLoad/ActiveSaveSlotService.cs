namespace DungeonInn.Application.SaveLoad
{
    public sealed class ActiveSaveSlotService
    {
        public int? ActiveSlotId { get; private set; }

        public void SetActiveSlot(int slotId)
        {
            ActiveSlotId = slotId;
        }

        public void Clear()
        {
            ActiveSlotId = null;
        }
    }
}
