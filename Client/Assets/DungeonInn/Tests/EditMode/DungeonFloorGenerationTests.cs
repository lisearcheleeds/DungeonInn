using System.Collections.Generic;
using DungeonInn.Application.Actors.Ai;
using DungeonInn.Application.Actors.Equipment;
using DungeonInn.Application.Actors.Lifecycle;
using DungeonInn.Application.Actors.Movement;
using DungeonInn.Application.Actors.Profiles;
using DungeonInn.Application.Actors.Spawn;
using DungeonInn.Application.Combat;
using DungeonInn.Application.Dungeons;
using DungeonInn.Application.Economy;
using DungeonInn.Application.Facilities;
using DungeonInn.Application.Items;
using DungeonInn.Application.World;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Map;
using DungeonInn.Master;
using NUnit.Framework;

namespace DungeonInn.Tests.EditMode
{
    public sealed class DungeonFloorGenerationTests
    {
        /// <summary>
        /// �����_�� seed �̃t���A��?E?E??E?��?E���A��w�K�i���牺�w�K�i�܂�?E?E??E?�ړ��\�ȒʘH�Őڑ�����Ă�E?E??E?���Ƃ���؂���AE        /// </summary>
        [Test]
        public void GeneratedFloorsConnectUpStairToDownStair()
        {
            for (var seed = 0; seed < 100; seed++)
            {
                var dungeon = new Dungeon(seed);
                var useCase = new GenerateDungeonFloorUseCase(new FixedWorldGameSettingsRepository(), new HardcodedMasterRepository());
                var floor = useCase.ExecuteAsync(
                        dungeon,
                        1)
                    .GetAwaiter()
                    .GetResult();

                Assert.That(
                    IsConnected(floor, floor.UpStair.Position, floor.DownStair.Position),
                    Is.True,
                    $"Generated dungeon floor is disconnected. seed:{seed}");
                Assert.That(
                    AreAllWalkableCellsConnected(floor, floor.UpStair.Position),
                    Is.True,
                    $"Generated dungeon floor has isolated walkable cells. seed:{seed}");
                Assert.That(
                    floor.Rooms,
                    Is.Not.Empty,
                    $"Generated dungeon floor has no rooms. seed:{seed}");

                foreach (var room in floor.Rooms)
                {
                    Assert.That(
                        floor.IsWalkable(room.Center),
                        Is.True,
                        $"Generated room center is not walkable. seed:{seed} room:{room.Id}");
                    Assert.That(
                        room.Cells,
                        Is.Not.Empty,
                        $"Generated room has no cells. seed:{seed} room:{room.Id}");
                }
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

        static bool AreAllWalkableCellsConnected(DungeonFloor floor, GridPosition start)
        {
            var visited = CreateVisitedMap(floor, start);
            for (var z = 0; z < floor.Layer.Depth; z++)
            {
                for (var x = 0; x < floor.Layer.Width; x++)
                {
                    var position = new GridPosition(x, z);
                    if (floor.IsWalkable(position) && !visited[x, z])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        static bool[,] CreateVisitedMap(DungeonFloor floor, GridPosition start)
        {
            var visited = new bool[floor.Layer.Width, floor.Layer.Depth];
            var queue = new Queue<GridPosition>();

            if (!floor.IsWalkable(start))
            {
                return visited;
            }

            visited[start.X, start.Z] = true;
            queue.Enqueue(start);

            while (0 < queue.Count)
            {
                var current = queue.Dequeue();
                EnqueueIfWalkable(floor, visited, queue, new GridPosition(current.X + 1, current.Z));
                EnqueueIfWalkable(floor, visited, queue, new GridPosition(current.X - 1, current.Z));
                EnqueueIfWalkable(floor, visited, queue, new GridPosition(current.X, current.Z + 1));
                EnqueueIfWalkable(floor, visited, queue, new GridPosition(current.X, current.Z - 1));
            }

            return visited;
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
