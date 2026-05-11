using System;
using DungeonInn.Application.UseCase;
using DungeonInn.Application.GameLoop;
using DungeonInn.Application.Combat;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Map;
using VContainer;

namespace DungeonInn.Application.Orchestration
{
    /// <summary>
    /// ダンジョン階段を利用して、地上または別フロアへ移動するユースケース。
    /// </summary>
    public sealed class UseDungeonStairOrchestrator
    {
        readonly EnsureDungeonFloorGeneratedOrchestrator ensureDungeonFloorGeneratedUseCase;

        [Inject]
        public UseDungeonStairOrchestrator(EnsureDungeonFloorGeneratedOrchestrator ensureDungeonFloorGeneratedUseCase)
        {
            this.ensureDungeonFloorGeneratedUseCase = ensureDungeonFloorGeneratedUseCase ?? throw new ArgumentNullException(nameof(ensureDungeonFloorGeneratedUseCase));
        }

        /// <summary>
        /// 現在位置が階段上であることを確認し、移動先レイヤーの対応階段付近へワープする。
        /// </summary>
        public async UniTask<LayerPosition> ExecuteAsync(
            Dungeon dungeon,
            GroundMap groundMap,
            LayerPosition currentPosition,
            DungeonStairType stairType,
            IReadOnlyList<DungeonDepthBandConfig> depthBandConfigs)
        {
            if (currentPosition.LayerId.Equals(MapLayerId.Ground))
            {
                if (stairType != DungeonStairType.Down)
                {
                    throw new InvalidOperationException("Ground dungeon entrance can only move down.");
                }

                return await EnterDungeonFromGroundAsync(dungeon, groundMap, currentPosition, depthBandConfigs);
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
            var targetFloor = await ensureDungeonFloorGeneratedUseCase.ExecuteAsync(
                dungeon,
                targetFloorIndex,
                depthBandConfigs);

            return targetFloor.GetArrivalPosition(arrivalStairType);
        }

        async UniTask<LayerPosition> EnterDungeonFromGroundAsync(
            Dungeon dungeon,
            GroundMap groundMap,
            LayerPosition currentPosition,
            IReadOnlyList<DungeonDepthBandConfig> depthBandConfigs)
        {
            var currentGrid = groundMap.Layer.ToGridPosition(currentPosition);
            if (!currentGrid.Equals(groundMap.DungeonEntrancePosition))
            {
                throw new InvalidOperationException("Current position is not dungeon entrance.");
            }

            var firstFloor = await ensureDungeonFloorGeneratedUseCase.ExecuteAsync(
                dungeon,
                1,
                depthBandConfigs);
            return firstFloor.GetArrivalPosition(DungeonStairType.Up);
        }
    }
}
