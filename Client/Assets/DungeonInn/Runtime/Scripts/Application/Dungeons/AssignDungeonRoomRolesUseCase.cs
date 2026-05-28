using System;
using System.Collections.Generic;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Master;
using VContainer;

namespace DungeonInn.Application.Dungeons
{
    public sealed class AssignDungeonRoomRolesUseCase
    {
        readonly IMasterRepository masterRepository;

        [Inject]
        public AssignDungeonRoomRolesUseCase(IMasterRepository masterRepository)
        {
            this.masterRepository = masterRepository ?? throw new ArgumentNullException(nameof(masterRepository));
        }

        public IReadOnlyList<DungeonRoom> Execute(int floorIndex, IReadOnlyList<DungeonRoom> rooms)
        {
            if (rooms == null)
            {
                throw new ArgumentNullException(nameof(rooms));
            }

            if (rooms.Count == 0)
            {
                return rooms;
            }

            var role = ToRoomRole(masterRepository.GetDungeonDepthBandMasterForFloor(floorIndex).SpecialRoomType);
            if (role == DungeonRoomRole.Normal)
            {
                return rooms;
            }

            var selectedRoomId = SelectHighestRouteDepthRoomId(rooms);
            var result = new DungeonRoom[rooms.Count];
            for (var i = 0; i < rooms.Count; i++)
            {
                result[i] = rooms[i].Id == selectedRoomId
                    ? rooms[i].WithRole(role)
                    : rooms[i];
            }

            return result;
        }

        static int SelectHighestRouteDepthRoomId(IReadOnlyList<DungeonRoom> rooms)
        {
            var selectedRoom = rooms[0];
            for (var i = 1; i < rooms.Count; i++)
            {
                if (selectedRoom.RouteDepth < rooms[i].RouteDepth)
                {
                    selectedRoom = rooms[i];
                }
            }

            return selectedRoom.Id;
        }

        static DungeonRoomRole ToRoomRole(DungeonSpecialRoomType specialRoomType)
        {
            switch (specialRoomType)
            {
                case DungeonSpecialRoomType.None:
                    return DungeonRoomRole.Normal;
                case DungeonSpecialRoomType.BossRoom:
                    return DungeonRoomRole.Boss;
                case DungeonSpecialRoomType.TreasureRoom:
                    return DungeonRoomRole.Treasure;
                case DungeonSpecialRoomType.RestRoom:
                    return DungeonRoomRole.Rest;
                default:
                    throw new ArgumentOutOfRangeException(nameof(specialRoomType));
            }
        }
    }
}
