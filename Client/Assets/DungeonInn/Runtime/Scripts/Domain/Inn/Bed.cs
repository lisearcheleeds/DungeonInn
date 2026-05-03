using System;
using DungeonInn.Domain.World;

namespace DungeonInn.Domain.Inn
{
    public class Bed
    {
        public Guid Id { get; }
        public GridPosition Position { get; }
        public bool IsOccupied { get; private set; }

        public Bed(Guid id, GridPosition position)
        {
            Id = id;
            Position = position;
        }

        public void CheckIn()
        {
            IsOccupied = true;
        }

        public void CheckOut()
        {
            IsOccupied = false;
        }
    }
}
