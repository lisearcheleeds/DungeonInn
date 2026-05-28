using DungeonInn.Domain.Dungeon;

namespace DungeonInn.Application.Actors.Spawn
{
    public readonly struct SpawnContext
    {
        public int FloorIndex { get; }
        public int DungeonDepthBandId { get; }
        public DungeonRoomRole RoomRole { get; }

        public SpawnContext(int floorIndex, int dungeonDepthBandId, DungeonRoomRole roomRole)
        {
            FloorIndex = floorIndex;
            DungeonDepthBandId = dungeonDepthBandId;
            RoomRole = roomRole;
        }
    }
}
