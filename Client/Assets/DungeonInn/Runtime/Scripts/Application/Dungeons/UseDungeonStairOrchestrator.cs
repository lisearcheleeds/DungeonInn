using System;
using DungeonInn.Application.Actors.Ai;
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
using DungeonInn.Application.GameLoop;

using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Map;
using VContainer;

namespace DungeonInn.Application.Dungeons
{
    /// <summary>
    /// 繝繝ｳ繧ｸ繝ｧ繝ｳ髫取ｮｵ繧貞茜逕ｨ縺励※縲∝慍荳翫∪縺滂ｿｽE蛻･繝輔Ο繧｢縺ｸ遘ｻ蜍輔☆繧九Θ繝ｼ繧ｹ繧ｱ繝ｼ繧ｹ縲・    /// </summary>
    public sealed class UseDungeonStairOrchestrator
    {
        readonly EnsureDungeonFloorGeneratedOrchestrator ensureDungeonFloorGeneratedUseCase;

        [Inject]
        public UseDungeonStairOrchestrator(EnsureDungeonFloorGeneratedOrchestrator ensureDungeonFloorGeneratedUseCase)
        {
            this.ensureDungeonFloorGeneratedUseCase = ensureDungeonFloorGeneratedUseCase ?? throw new ArgumentNullException(nameof(ensureDungeonFloorGeneratedUseCase));
        }

        /// <summary>
        /// 迴ｾ蝨ｨ菴咲ｽｮ縺碁嚴谿ｵ荳翫〒縺ゅｋ縺薙→繧堤｢ｺ隱阪＠縲∫ｧｻ蜍包ｿｽE繝ｬ繧､繝､繝ｼ縺ｮ蟇ｾ蠢憺嚴谿ｵ莉倩ｿ代∈繝ｯ繝ｼ繝励☆繧九・        /// </summary>
        public async UniTask<LayerPosition> ExecuteAsync(
            Dungeon dungeon,
            GroundMap groundMap,
            LayerPosition currentPosition,
            DungeonStairType stairType)
        {
            if (currentPosition.LayerId.Equals(MapLayerId.Ground))
            {
                if (stairType != DungeonStairType.Down)
                {
                    throw new InvalidOperationException("Ground dungeon entrance can only move down.");
                }

                return await EnterDungeonFromGroundAsync(dungeon, groundMap, currentPosition);
            }

            var currentFloor = dungeon.GetFloor(currentPosition.LayerId.Value);
            var currentGrid = currentFloor.Layer.ToGridPosition(currentPosition);
            if (!currentFloor.IsStairPosition(currentGrid, stairType))
            {
                throw new InvalidOperationException("Current position is not target stair.");
            }

            if (stairType == DungeonStairType.Up && currentFloor.FloorIndex == 1)
            {
                return groundMap.Layer.GetCellCenter(groundMap.DungeonEntrancePosition);
            }

            var targetFloorIndex = stairType == DungeonStairType.Down
                ? currentFloor.FloorIndex + 1
                : currentFloor.FloorIndex - 1;
            var arrivalStairType = stairType == DungeonStairType.Down
                ? DungeonStairType.Up
                : DungeonStairType.Down;
            var targetFloor = await ensureDungeonFloorGeneratedUseCase.ExecuteAsync(dungeon, targetFloorIndex);

            return targetFloor.GetArrivalPosition(arrivalStairType);
        }

        async UniTask<LayerPosition> EnterDungeonFromGroundAsync(
            Dungeon dungeon,
            GroundMap groundMap,
            LayerPosition currentPosition)
        {
            var currentGrid = groundMap.Layer.ToGridPosition(currentPosition);
            if (!currentGrid.Equals(groundMap.DungeonEntrancePosition))
            {
                throw new InvalidOperationException("Current position is not dungeon entrance.");
            }

            var firstFloor = await ensureDungeonFloorGeneratedUseCase.ExecuteAsync(dungeon, 1);
            return firstFloor.GetArrivalPosition(DungeonStairType.Up);
        }
    }
}
