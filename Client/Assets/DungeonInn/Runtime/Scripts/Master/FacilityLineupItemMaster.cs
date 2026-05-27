using System;

namespace DungeonInn.Master
{
    public sealed class FacilityLineupItemMaster
    {
        public FacilityLineupItemMaster(int id, int lineupId, int itemId, int displayPriority)
        {
            if (id < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            if (lineupId < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(lineupId));
            }

            if (itemId < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(itemId));
            }

            Id = id;
            LineupId = lineupId;
            ItemId = itemId;
            DisplayPriority = Math.Max(0, displayPriority);
        }

        public int Id { get; }
        public int LineupId { get; }
        public int ItemId { get; }
        public int DisplayPriority { get; }
    }
}
