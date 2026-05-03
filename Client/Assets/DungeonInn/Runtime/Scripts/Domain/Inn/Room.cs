using System;
using System.Collections.Generic;
using DungeonInn.Domain.World;

namespace DungeonInn.Domain.Inn
{
    public class Room
    {
        private readonly List<Bed> beds = new();
        private readonly List<GridPosition> doorPositions = new();

        public Guid Id { get; }
        public GridPosition Origin { get; }
        public int Width { get; }
        public int Depth { get; }
        public IReadOnlyList<Bed> Beds => beds;
        public IReadOnlyList<GridPosition> DoorPositions => doorPositions;

        public Room(Guid id, GridPosition origin, int width, int depth)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (depth <= 0) throw new ArgumentOutOfRangeException(nameof(depth));

            Id = id;
            Origin = origin;
            Width = width;
            Depth = depth;
        }

        public void OpenDoor(GridPosition wallPos)
        {
            if (!IsWall(wallPos))
            {
                throw new ArgumentException("Door position must be on a wall.", nameof(wallPos));
            }

            if (!doorPositions.Contains(wallPos))
            {
                doorPositions.Add(wallPos);
            }
        }

        public bool CanPlaceBedAt(GridPosition worldPos)
        {
            return Contains(worldPos) &&
                   !IsWall(worldPos) &&
                   !doorPositions.Contains(worldPos) &&
                   beds.TrueForAll(b => b.Position != worldPos);
        }

        public void PlaceBed(Bed bed)
        {
            if (bed == null) throw new ArgumentNullException(nameof(bed));
            if (!CanPlaceBedAt(bed.Position))
            {
                throw new InvalidOperationException("Cannot place bed at the specified position.");
            }

            beds.Add(bed);
        }

        public void RemoveBed(Guid bedId)
        {
            beds.RemoveAll(b => b.Id == bedId);
        }

        public bool IsWall(GridPosition worldPos)
        {
            if (!Contains(worldPos))
            {
                return false;
            }

            return worldPos.X == Origin.X ||
                   worldPos.X == Origin.X + Width - 1 ||
                   worldPos.Z == Origin.Z ||
                   worldPos.Z == Origin.Z + Depth - 1;
        }

        public bool Contains(GridPosition worldPos)
        {
            return worldPos.Y == Origin.Y &&
                   worldPos.X >= Origin.X &&
                   worldPos.X < Origin.X + Width &&
                   worldPos.Z >= Origin.Z &&
                   worldPos.Z < Origin.Z + Depth;
        }

        public float Density => (float)beds.Count / ((Width - 2) * (Depth - 2));
    }
}
