using System;

namespace DungeonInn.Domain.Dungeon
{
    public sealed class DungeonExplorationGoal
    {
        public DungeonExplorationGoalType Type { get; }
        public int TargetItemId { get; }
        public int TargetItemCount { get; }
        public int TargetMonsterId { get; }
        public int TargetFloorId { get; }

        DungeonExplorationGoal(
            DungeonExplorationGoalType type,
            int targetItemId,
            int targetItemCount,
            int targetMonsterId,
            int targetFloorId)
        {
            Type = type;
            TargetItemId = Math.Max(0, targetItemId);
            TargetItemCount = Math.Max(0, targetItemCount);
            TargetMonsterId = Math.Max(0, targetMonsterId);
            TargetFloorId = Math.Max(0, targetFloorId);
        }

        public static DungeonExplorationGoal CreateLeveling()
        {
            return new DungeonExplorationGoal(
                DungeonExplorationGoalType.Leveling,
                0,
                0,
                0,
                0);
        }

        public static DungeonExplorationGoal CreateCollectItem(int targetItemId, int targetItemCount)
        {
            if (targetItemId < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(targetItemId));
            }

            if (targetItemCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(targetItemCount));
            }

            return new DungeonExplorationGoal(
                DungeonExplorationGoalType.CollectItem,
                targetItemId,
                targetItemCount,
                0,
                0);
        }

        public static DungeonExplorationGoal CreateDefeatMonster(int targetMonsterId)
        {
            if (targetMonsterId < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(targetMonsterId));
            }

            return new DungeonExplorationGoal(
                DungeonExplorationGoalType.DefeatMonster,
                0,
                0,
                targetMonsterId,
                0);
        }

        public static DungeonExplorationGoal CreateReachFloor(int targetFloorId)
        {
            if (targetFloorId < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(targetFloorId));
            }

            return new DungeonExplorationGoal(
                DungeonExplorationGoalType.ReachFloor,
                0,
                0,
                0,
                targetFloorId);
        }
    }
}
