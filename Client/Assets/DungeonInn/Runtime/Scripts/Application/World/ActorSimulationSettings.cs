using System;

namespace DungeonInn.Application.World
{
    public sealed class ActorSimulationSettings
    {
        public float MoveSpeedMetersPerSecond { get; }
        public float MoveArrivalDistanceMeters { get; }
        public float ItemPickupRadiusMeters { get; }
        public int ExplorationRoomArrivalTarget { get; }

        public ActorSimulationSettings(
            float moveSpeedMetersPerSecond,
            float moveArrivalDistanceMeters,
            float itemPickupRadiusMeters,
            int explorationRoomArrivalTarget)
        {
            MoveSpeedMetersPerSecond = Math.Max(0f, moveSpeedMetersPerSecond);
            MoveArrivalDistanceMeters = Math.Max(0f, moveArrivalDistanceMeters);
            ItemPickupRadiusMeters = Math.Max(0f, itemPickupRadiusMeters);
            ExplorationRoomArrivalTarget = Math.Max(1, explorationRoomArrivalTarget);
        }

        public static ActorSimulationSettings CreateDefault()
        {
            return new ActorSimulationSettings(5f, 2.5f, 1.5f, 8);
        }
    }
}
