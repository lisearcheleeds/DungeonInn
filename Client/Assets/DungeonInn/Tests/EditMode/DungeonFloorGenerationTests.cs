using System.Collections.Generic;
using DungeonInn.Application.UseCase;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Map;
using NUnit.Framework;

namespace DungeonInn.Tests.EditMode
{
    public sealed class DungeonFloorGenerationTests
    {
        /// <summary>
        /// ランダム seed のフロアを複数生成し、上層階段から下層階段まで必ず移動可能な通路で接続されていることを検証する。
        /// </summary>
        [Test]
        public void GeneratedFloorsConnectUpStairToDownStair()
        {
            for (var seed = 0; seed < 100; seed++)
            {
                var dungeon = new Dungeon(seed);
                var useCase = new GenerateDungeonFloorUseCase();
                var floor = useCase.ExecuteAsync(
                        dungeon,
                        1,
                        new List<DungeonDepthBandConfig>())
                    .GetAwaiter()
                    .GetResult();

                Assert.That(
                    IsConnected(floor, floor.UpStair.Position, floor.DownStair.Position),
                    Is.True,
                    $"Generated dungeon floor is disconnected. seed:{seed}");
            }
        }

        static bool IsConnected(DungeonFloor floor, GridPosition start, GridPosition goal)
        {
            var visited = new bool[floor.Layer.Width, floor.Layer.Depth];
            var queue = new Queue<GridPosition>();

            if (!floor.IsWalkable(start) || !floor.IsWalkable(goal))
            {
                return false;
            }

            visited[start.X, start.Z] = true;
            queue.Enqueue(start);

            while (0 < queue.Count)
            {
                var current = queue.Dequeue();
                if (current.Equals(goal))
                {
                    return true;
                }

                EnqueueIfWalkable(floor, visited, queue, new GridPosition(current.X + 1, current.Z));
                EnqueueIfWalkable(floor, visited, queue, new GridPosition(current.X - 1, current.Z));
                EnqueueIfWalkable(floor, visited, queue, new GridPosition(current.X, current.Z + 1));
                EnqueueIfWalkable(floor, visited, queue, new GridPosition(current.X, current.Z - 1));
            }

            return false;
        }

        static void EnqueueIfWalkable(
            DungeonFloor floor,
            bool[,] visited,
            Queue<GridPosition> queue,
            GridPosition position)
        {
            if (!floor.Layer.Contains(position))
            {
                return;
            }

            if (visited[position.X, position.Z])
            {
                return;
            }

            if (!floor.IsWalkable(position))
            {
                return;
            }

            visited[position.X, position.Z] = true;
            queue.Enqueue(position);
        }
    }
}
