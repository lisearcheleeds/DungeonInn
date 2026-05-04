using Cysharp.Threading.Tasks;
using DungeonInn.Domain.Dungeon;
using DungeonInn.Domain.Map;

namespace DungeonInn.Application.UseCase
{
    /// <summary>
    /// 地上またはダンジョンフロア上の指定座標へ移動できるか判定するユースケース。
    /// </summary>
    public sealed class CanMoveOnMapLayerUseCase
    {
        /// <summary>
        /// レイヤー ID から地上またはダンジョンフロアを選び、セルしきい値で移動可否を判定する。
        /// </summary>
        public UniTask<bool> ExecuteAsync(
            GroundMap groundMap,
            Dungeon dungeon,
            LayerPosition position,
            float agentRadius)
        {
            if (position.LayerId.Equals(MapLayerId.Ground))
            {
                return UniTask.FromResult(groundMap.IsWalkable(position, agentRadius));
            }

            var floor = dungeon.GetFloor(position.LayerId.Value);
            return UniTask.FromResult(floor.IsWalkable(position, agentRadius));
        }
    }
}
