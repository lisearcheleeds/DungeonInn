using System;
using System.Collections.Generic;
using System.Linq;
using DungeonInn.Domain.World;

namespace DungeonInn.Domain.Inn
{
    public class InnLand
    {
        private const int ParcelSize = 5;
        private readonly List<Room> rooms;

        public IReadOnlyList<Room> Rooms => rooms;
        public Money Funds { get; private set; }
        public int OwnedBedCount { get; private set; }
        public int PlacedBedCount => rooms.Sum(r => r.Beds.Count);
        public int UnplacedBedCount => OwnedBedCount - PlacedBedCount;

        public InnLand(IEnumerable<Room> initialRooms, Money initialFunds, int initialBedCount)
        {
            rooms = initialRooms?.ToList() ?? throw new ArgumentNullException(nameof(initialRooms));
            Funds = initialFunds;
            OwnedBedCount = Math.Max(0, initialBedCount);
        }

        public Room PurchaseParcel(GridPosition origin, Money cost)
        {
            if (!CanPurchaseAt(origin, cost))
            {
                throw new InvalidOperationException("Cannot purchase parcel at the specified position.");
            }

            TrySpendFunds(cost);
            var room = new Room(Guid.NewGuid(), origin, ParcelSize, ParcelSize);
            OpenAdjacentDoors(room);
            rooms.Add(room);
            return room;
        }

        public bool CanPurchaseAt(GridPosition origin, Money cost)
        {
            return Funds >= cost &&
                   !rooms.Any(r => Overlaps(origin, ParcelSize, ParcelSize, r)) &&
                   (rooms.Count == 0 || rooms.Any(r => IsAdjacent(origin, r)));
        }

        public Room GetRoomContaining(GridPosition worldPos)
        {
            return rooms.FirstOrDefault(r => r.Contains(worldPos));
        }

        public void AddFunds(Money amount)
        {
            Funds += amount;
        }

        public bool TrySpendFunds(Money amount)
        {
            if (!(Funds >= amount))
            {
                return false;
            }

            Funds -= amount;
            return true;
        }

        public void AddBeds(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            OwnedBedCount += count;
        }

        private void OpenAdjacentDoors(Room newRoom)
        {
            foreach (var existingRoom in rooms)
            {
                if (existingRoom.Origin.X == newRoom.Origin.X &&
                    existingRoom.Origin.Z == newRoom.Origin.Z + ParcelSize)
                {
                    newRoom.OpenDoor(new GridPosition(newRoom.Origin.X + 2, newRoom.Origin.Y, newRoom.Origin.Z + 4));
                    existingRoom.OpenDoor(new GridPosition(existingRoom.Origin.X + 2, existingRoom.Origin.Y, existingRoom.Origin.Z));
                }

                if (existingRoom.Origin.X == newRoom.Origin.X &&
                    existingRoom.Origin.Z + ParcelSize == newRoom.Origin.Z)
                {
                    newRoom.OpenDoor(new GridPosition(newRoom.Origin.X + 2, newRoom.Origin.Y, newRoom.Origin.Z));
                    existingRoom.OpenDoor(new GridPosition(existingRoom.Origin.X + 2, existingRoom.Origin.Y, existingRoom.Origin.Z + 4));
                }

                if (existingRoom.Origin.X == newRoom.Origin.X + ParcelSize &&
                    existingRoom.Origin.Z == newRoom.Origin.Z)
                {
                    newRoom.OpenDoor(new GridPosition(newRoom.Origin.X + 4, newRoom.Origin.Y, newRoom.Origin.Z + 2));
                    existingRoom.OpenDoor(new GridPosition(existingRoom.Origin.X, existingRoom.Origin.Y, existingRoom.Origin.Z + 2));
                }

                if (existingRoom.Origin.X + ParcelSize == newRoom.Origin.X &&
                    existingRoom.Origin.Z == newRoom.Origin.Z)
                {
                    newRoom.OpenDoor(new GridPosition(newRoom.Origin.X, newRoom.Origin.Y, newRoom.Origin.Z + 2));
                    existingRoom.OpenDoor(new GridPosition(existingRoom.Origin.X + 4, existingRoom.Origin.Y, existingRoom.Origin.Z + 2));
                }
            }
        }

        private static bool Overlaps(GridPosition origin, int width, int depth, Room room)
        {
            return origin.X < room.Origin.X + room.Width &&
                   origin.X + width > room.Origin.X &&
                   origin.Z < room.Origin.Z + room.Depth &&
                   origin.Z + depth > room.Origin.Z;
        }

        private static bool IsAdjacent(GridPosition origin, Room room)
        {
            return (origin.X == room.Origin.X && Math.Abs(origin.Z - room.Origin.Z) == ParcelSize) ||
                   (origin.Z == room.Origin.Z && Math.Abs(origin.X - room.Origin.X) == ParcelSize);
        }
    }
}
