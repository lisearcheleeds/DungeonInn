using System;

namespace DungeonInn.Domain.World
{
    public class WorldGrid
    {
        private readonly bool[,,] occupiedCells;

        public int SizeX { get; }
        public int SizeY { get; }
        public int SizeZ { get; }

        public WorldGrid(int sizeX, int sizeY, int sizeZ)
        {
            if (sizeX <= 0) throw new ArgumentOutOfRangeException(nameof(sizeX));
            if (sizeY <= 0) throw new ArgumentOutOfRangeException(nameof(sizeY));
            if (sizeZ <= 0) throw new ArgumentOutOfRangeException(nameof(sizeZ));

            SizeX = sizeX;
            SizeY = sizeY;
            SizeZ = sizeZ;
            occupiedCells = new bool[sizeX, sizeY, sizeZ];
        }

        public bool IsInBounds(GridPosition pos)
        {
            return pos.X >= 0 && pos.X < SizeX &&
                   pos.Y >= 0 && pos.Y < SizeY &&
                   pos.Z >= 0 && pos.Z < SizeZ;
        }

        public bool IsOccupied(GridPosition pos)
        {
            return IsInBounds(pos) && occupiedCells[pos.X, pos.Y, pos.Z];
        }

        public void SetOccupied(GridPosition pos, bool occupied)
        {
            if (!IsInBounds(pos))
            {
                throw new ArgumentOutOfRangeException(nameof(pos));
            }

            occupiedCells[pos.X, pos.Y, pos.Z] = occupied;
        }
    }
}
